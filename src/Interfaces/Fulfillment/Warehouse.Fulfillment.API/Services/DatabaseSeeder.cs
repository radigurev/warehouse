using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Warehouse.Fulfillment.DBModel;
using Warehouse.Fulfillment.DBModel.Models;
using Warehouse.Infrastructure.Configuration;

namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Seeds Fulfillment reference data (carriers, service levels) and optionally a rich demo data set
/// (product prices with historical windows, ~60 sales orders covering every status, pick lists,
/// parcels, shipments with tracking entries, customer returns, and full lifecycle events) on
/// application startup. All operations are idempotent and gated by feature flags.
/// </summary>
public sealed class DatabaseSeeder
{
    private const int CompletedOrderCount = 8;
    private const int CompletedOrderStartIndex = 51;

    private readonly FulfillmentDbContext _context;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance with the specified dependencies.
    /// </summary>
    public DatabaseSeeder(
        FulfillmentDbContext context,
        IFeatureManager featureManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds reference and (optionally) demo data. Safe to call on every startup.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        bool referenceEnabled = await _featureManager
            .IsEnabledAsync(FeatureFlags.EnableSeedFulfillmentReferenceData)
            .ConfigureAwait(false);

        if (referenceEnabled)
        {
            await SeedReferenceDataAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Fulfillment reference data seeding disabled by flag {Flag}",
                FeatureFlags.EnableSeedFulfillmentReferenceData);
        }

        bool demoEnabled = await _featureManager
            .IsEnabledAsync(FeatureFlags.EnableSeedFulfillmentDemoData)
            .ConfigureAwait(false);

        if (demoEnabled)
        {
            await SeedDemoDataAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Fulfillment demo data seeding disabled by flag {Flag}",
                FeatureFlags.EnableSeedFulfillmentDemoData);
        }
    }

    /// <summary>
    /// Seeds carriers and their service levels. Idempotent by Carrier.Code and (CarrierId, ServiceLevel.Code).
    /// </summary>
    private async Task SeedReferenceDataAsync(CancellationToken cancellationToken)
    {
        int? adminUserId = await ResolveAdminUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (adminUserId is null)
        {
            _logger.LogWarning("Skipping Fulfillment reference seeding — admin user not found in auth.Users");
            return;
        }

        foreach (CarrierSeed seed in FulfillmentSeedData.Carriers)
        {
            Carrier carrier = await EnsureCarrierAsync(seed, adminUserId.Value, cancellationToken)
                .ConfigureAwait(false);

            foreach (ServiceLevelSeed levelSeed in seed.ServiceLevels)
            {
                await EnsureServiceLevelAsync(carrier.Id, levelSeed, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Seeded {CarrierCount} Fulfillment carriers with service levels",
            FulfillmentSeedData.Carriers.Count);
    }

    /// <summary>
    /// Inserts a carrier when missing. Returns the tracked entity.
    /// </summary>
    private async Task<Carrier> EnsureCarrierAsync(
        CarrierSeed seed,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        Carrier? existing = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Code == seed.Code, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        Carrier carrier = new()
        {
            Code = seed.Code,
            Name = seed.Name,
            ContactPhone = seed.ContactPhone,
            ContactEmail = seed.ContactEmail,
            WebsiteUrl = seed.WebsiteUrl,
            TrackingUrlTemplate = seed.TrackingUrlTemplate,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Carriers.Add(carrier);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return carrier;
    }

    /// <summary>
    /// Inserts a service level under the given carrier when missing.
    /// </summary>
    private async Task EnsureServiceLevelAsync(
        int carrierId,
        ServiceLevelSeed seed,
        CancellationToken cancellationToken)
    {
        bool exists = await _context.CarrierServiceLevels
            .AnyAsync(l => l.CarrierId == carrierId && l.Code == seed.Code, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        CarrierServiceLevel level = new()
        {
            CarrierId = carrierId,
            Code = seed.Code,
            Name = seed.Name,
            EstimatedDeliveryDays = seed.EstimatedDeliveryDays,
            BaseRate = seed.BaseRate,
            PerKgRate = seed.PerKgRate,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.CarrierServiceLevels.Add(level);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves cross-schema dependencies (admin user, customers, accounts, warehouses, products, carriers)
    /// and seeds product prices, sales orders covering every status, and customer returns. Gracefully skips
    /// when required dependencies cannot be resolved.
    /// </summary>
    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        DemoContext? demoContext = await ResolveDemoContextAsync(cancellationToken).ConfigureAwait(false);
        if (demoContext is null)
        {
            return;
        }

        await SeedProductPricesAsync(demoContext, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CarrierServicePair> carrierPairs =
            await ResolveCarrierServicePairsAsync(cancellationToken).ConfigureAwait(false);
        if (carrierPairs.Count == 0)
        {
            _logger.LogWarning("Skipping demo sales order seeding — no carriers/service levels present");
            return;
        }

        DemoRuntime runtime = new(demoContext, carrierPairs);

        Dictionary<string, int> seededOrderIds = await SeedSalesOrdersAsync(runtime, cancellationToken)
            .ConfigureAwait(false);

        await SeedCustomerReturnsAsync(runtime, seededOrderIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Fulfillment demo seeding complete: {OrderCount} sales orders processed, {ReturnCount} returns processed",
            FulfillmentSeedData.DemoOrders.Count, FulfillmentSeedData.DemoReturns.Count);
    }

    /// <summary>
    /// Seeds current and historical product price entries for every resolved demo product.
    /// </summary>
    private async Task SeedProductPricesAsync(DemoContext context, CancellationToken cancellationToken)
    {
        int currentInserted = await InsertCurrentPricesAsync(context, cancellationToken)
            .ConfigureAwait(false);
        int historicalInserted = await InsertHistoricalPricesAsync(context, cancellationToken)
            .ConfigureAwait(false);

        if (currentInserted + historicalInserted > 0)
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Seeded {Current} current and {Historical} historical product price rows for {Count} products",
            currentInserted, historicalInserted, context.ProductIds.Count);
    }

    /// <summary>
    /// Inserts current open-ended price rows. Idempotent by (ProductId, CurrencyCode, ValidFrom IS NULL).
    /// </summary>
    private async Task<int> InsertCurrentPricesAsync(DemoContext context, CancellationToken cancellationToken)
    {
        int inserted = 0;
        foreach (int productId in context.ProductIds)
        {
            foreach (ProductPriceSeed seed in FulfillmentSeedData.ProductPrices)
            {
                bool exists = await _context.ProductPrices.AnyAsync(p =>
                        p.ProductId == productId
                        && p.CurrencyCode == seed.CurrencyCode
                        && p.ValidFrom == null,
                    cancellationToken).ConfigureAwait(false);

                if (exists)
                {
                    continue;
                }

                _context.ProductPrices.Add(new ProductPrice
                {
                    ProductId = productId,
                    CurrencyCode = seed.CurrencyCode,
                    UnitPrice = seed.UnitPrice,
                    ValidFrom = null,
                    ValidTo = null,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = context.AdminUserId
                });
                inserted++;
            }
        }

        return inserted;
    }

    /// <summary>
    /// Inserts historical price windows. Idempotent by (ProductId, CurrencyCode, ValidFromUtc).
    /// </summary>
    private async Task<int> InsertHistoricalPricesAsync(DemoContext context, CancellationToken cancellationToken)
    {
        int inserted = 0;
        foreach (int productId in context.ProductIds)
        {
            foreach (HistoricalPriceSeed seed in FulfillmentSeedData.HistoricalProductPrices)
            {
                bool exists = await _context.ProductPrices.AnyAsync(p =>
                        p.ProductId == productId
                        && p.CurrencyCode == seed.CurrencyCode
                        && p.ValidFrom == seed.ValidFromUtc,
                    cancellationToken).ConfigureAwait(false);

                if (exists)
                {
                    continue;
                }

                _context.ProductPrices.Add(new ProductPrice
                {
                    ProductId = productId,
                    CurrencyCode = seed.CurrencyCode,
                    UnitPrice = seed.UnitPrice,
                    ValidFrom = seed.ValidFromUtc,
                    ValidTo = seed.ValidToUtc,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = context.AdminUserId
                });
                inserted++;
            }
        }

        return inserted;
    }

    /// <summary>
    /// Seeds every demo sales order in <see cref="FulfillmentSeedData.DemoOrders"/>. Idempotent by OrderNumber.
    /// Returns a map from OrderNumber to assigned SalesOrder.Id (including pre-existing rows) so that downstream
    /// seeders (customer returns) can resolve linked SOs.
    /// </summary>
    private async Task<Dictionary<string, int>> SeedSalesOrdersAsync(
        DemoRuntime runtime,
        CancellationToken cancellationToken)
    {
        Dictionary<string, int> seededOrderIds = new(StringComparer.Ordinal);
        List<FulfillmentEvent> pendingEvents = new();

        foreach (DemoOrderSpec spec in FulfillmentSeedData.DemoOrders)
        {
            int? existingId = await GetSalesOrderIdAsync(spec.OrderNumber, cancellationToken)
                .ConfigureAwait(false);
            if (existingId is not null)
            {
                seededOrderIds[spec.OrderNumber] = existingId.Value;
                continue;
            }

            SalesOrder salesOrder = BuildSalesOrderGraph(spec, runtime);
            Shipment? shipment = BuildShipmentIfApplicable(salesOrder, spec, runtime);

            _context.SalesOrders.Add(salesOrder);
            if (shipment is not null)
            {
                _context.Shipments.Add(shipment);
            }
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            seededOrderIds[spec.OrderNumber] = salesOrder.Id;
            QueueLifecycleEvents(salesOrder, spec, runtime.Context.AdminUserId, pendingEvents);
        }

        if (pendingEvents.Count > 0)
        {
            _context.FulfillmentEvents.AddRange(pendingEvents);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return seededOrderIds;
    }

    /// <summary>
    /// Builds the full SO entity graph (header + lines + downstream pick list/parcel/shipment per status).
    /// </summary>
    private static SalesOrder BuildSalesOrderGraph(DemoOrderSpec spec, DemoRuntime runtime)
    {
        SalesOrder salesOrder = BuildSalesOrderHeader(spec, runtime);
        AddSalesOrderLines(salesOrder, spec, runtime);
        salesOrder.TotalAmount = salesOrder.Lines.Sum(l => l.LineTotal);

        ApplyStatusSpecificFields(salesOrder, spec, runtime.Context.AdminUserId);
        AttachDownstreamGraph(salesOrder, spec, runtime);

        return salesOrder;
    }

    /// <summary>
    /// Builds the sales-order header fields, rotating customer/warehouse/carrier/address pools by spec index.
    /// </summary>
    private static SalesOrder BuildSalesOrderHeader(DemoOrderSpec spec, DemoRuntime runtime)
    {
        int seqIndex = spec.OrderIndex - 1;
        DemoContext ctx = runtime.Context;
        CustomerProfile customer = ctx.Customers[seqIndex % ctx.Customers.Count];
        int warehouseId = ctx.WarehouseIds[seqIndex % ctx.WarehouseIds.Count];
        AddressSeed shipping = FulfillmentSeedData.Addresses[seqIndex % FulfillmentSeedData.Addresses.Count];
        AddressSeed billing = FulfillmentSeedData.Addresses[(seqIndex + 3) % FulfillmentSeedData.Addresses.Count];
        CarrierServicePair carrierPair = runtime.CarrierPairs[seqIndex % runtime.CarrierPairs.Count];
        DateTime createdAt = DateTime.UtcNow.AddDays(-spec.DaysAgoCreated);

        return new SalesOrder
        {
            OrderNumber = spec.OrderNumber,
            CustomerId = customer.CustomerId,
            CustomerAccountId = customer.AccountId,
            CurrencyCode = customer.CurrencyCode,
            Status = spec.Status,
            WarehouseId = warehouseId,
            RequestedShipDate = DateOnly.FromDateTime(createdAt.AddDays(3)),
            ShippingStreetLine1 = shipping.StreetLine1,
            ShippingCity = shipping.City,
            ShippingStateProvince = shipping.StateProvince,
            ShippingPostalCode = shipping.PostalCode,
            ShippingCountryCode = shipping.CountryCode,
            BillingStreetLine1 = billing.StreetLine1,
            BillingCity = billing.City,
            BillingStateProvince = billing.StateProvince,
            BillingPostalCode = billing.PostalCode,
            BillingCountryCode = billing.CountryCode,
            CarrierId = carrierPair.CarrierId,
            CarrierServiceLevelId = carrierPair.ServiceLevelId,
            Notes = $"Demo SO #{spec.OrderIndex} ({spec.Status}). Safe to delete.",
            CreatedAtUtc = createdAt,
            CreatedByUserId = ctx.AdminUserId
        };
    }

    /// <summary>
    /// Adds 1–5 lines to the sales order, rotating products and varying quantities/prices deterministically.
    /// </summary>
    private static void AddSalesOrderLines(SalesOrder salesOrder, DemoOrderSpec spec, DemoRuntime runtime)
    {
        int seqIndex = spec.OrderIndex - 1;
        IReadOnlyList<int> productIds = runtime.Context.ProductIds;
        for (int lineIdx = 0; lineIdx < spec.LineCount; lineIdx++)
        {
            int productId = productIds[(seqIndex * 3 + lineIdx) % productIds.Count];
            decimal quantity = 1m + ((seqIndex * 2 + lineIdx) % 12);
            decimal unitPrice = Math.Round(9.99m + ((seqIndex * 7 + lineIdx * 11) % 90) * 1.10m, 2);
            salesOrder.Lines.Add(new SalesOrderLine
            {
                ProductId = productId,
                OrderedQuantity = quantity,
                UnitPrice = unitPrice,
                LineTotal = Math.Round(quantity * unitPrice, 4)
            });
        }
    }

    /// <summary>
    /// Sets ConfirmedAt/ShippedAt/CompletedAt timestamps and per-line picked/packed/shipped quantities
    /// according to the SO status.
    /// </summary>
    private static void ApplyStatusSpecificFields(SalesOrder salesOrder, DemoOrderSpec spec, int adminUserId)
    {
        DateTime baseTime = salesOrder.CreatedAtUtc;
        switch (spec.Status)
        {
            case "Confirmed":
                salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                salesOrder.ConfirmedByUserId = adminUserId;
                break;
            case "Picking":
                salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                salesOrder.ConfirmedByUserId = adminUserId;
                break;
            case "Packed":
                salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                salesOrder.ConfirmedByUserId = adminUserId;
                FillFulfilledQuantities(salesOrder.Lines, packed: true, shipped: false);
                break;
            case "Shipped":
                salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                salesOrder.ConfirmedByUserId = adminUserId;
                salesOrder.ShippedAtUtc = baseTime.AddDays(2);
                FillFulfilledQuantities(salesOrder.Lines, packed: true, shipped: true);
                break;
            case "Completed":
                salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                salesOrder.ConfirmedByUserId = adminUserId;
                salesOrder.ShippedAtUtc = baseTime.AddDays(2);
                salesOrder.CompletedAtUtc = baseTime.AddDays(7);
                salesOrder.CompletedByUserId = adminUserId;
                FillFulfilledQuantities(salesOrder.Lines, packed: true, shipped: true);
                break;
            case "Cancelled":
                if (spec.OrderIndex % 2 == 1)
                {
                    salesOrder.ConfirmedAtUtc = baseTime.AddHours(6);
                    salesOrder.ConfirmedByUserId = adminUserId;
                }
                salesOrder.ModifiedAtUtc = baseTime.AddHours(12);
                salesOrder.ModifiedByUserId = adminUserId;
                break;
        }
    }

    /// <summary>
    /// Fills picked/packed/shipped quantities to ordered for each line based on flags.
    /// </summary>
    private static void FillFulfilledQuantities(ICollection<SalesOrderLine> lines, bool packed, bool shipped)
    {
        foreach (SalesOrderLine line in lines)
        {
            line.PickedQuantity = line.OrderedQuantity;
            if (packed)
            {
                line.PackedQuantity = line.OrderedQuantity;
            }
            if (shipped)
            {
                line.ShippedQuantity = line.OrderedQuantity;
            }
        }
    }

    /// <summary>
    /// Attaches pick lists and parcels (nav children of SalesOrder) based on the SO status. Shipments are
    /// not nav children of SalesOrder and are constructed separately by <see cref="BuildShipmentIfApplicable"/>.
    /// </summary>
    private static void AttachDownstreamGraph(SalesOrder salesOrder, DemoOrderSpec spec, DemoRuntime runtime)
    {
        switch (spec.Status)
        {
            case "Picking":
                salesOrder.PickLists.Add(BuildPickList(salesOrder, spec, runtime, completed: false));
                break;
            case "Packed":
                {
                    PickList pickList = BuildPickList(salesOrder, spec, runtime, completed: true);
                    salesOrder.PickLists.Add(pickList);
                    salesOrder.Parcels.Add(BuildParcel(salesOrder, pickList, spec, runtime));
                    break;
                }
            case "Shipped":
            case "Completed":
                {
                    PickList pickList = BuildPickList(salesOrder, spec, runtime, completed: true);
                    salesOrder.PickLists.Add(pickList);
                    salesOrder.Parcels.Add(BuildParcel(salesOrder, pickList, spec, runtime));
                    break;
                }
        }
    }

    /// <summary>
    /// Builds a Shipment graph (header + lines + tracking entries) for SOs that have reached Shipped/Completed.
    /// Returns <c>null</c> for any other status. Uses navigation properties so EF resolves SalesOrderId/SalesOrderLineId
    /// during SaveChanges.
    /// </summary>
    private static Shipment? BuildShipmentIfApplicable(
        SalesOrder salesOrder,
        DemoOrderSpec spec,
        DemoRuntime runtime)
    {
        if (spec.Status != "Shipped" && spec.Status != "Completed")
        {
            return null;
        }

        Shipment shipment = BuildShipment(salesOrder, spec, runtime);
        AttachShipmentLines(shipment, salesOrder.Lines);
        AttachTrackingEntries(shipment, spec, runtime.Context.AdminUserId);
        return shipment;
    }

    /// <summary>
    /// Builds a pick list (with one line per SO line) at the given completion state.
    /// </summary>
    private static PickList BuildPickList(
        SalesOrder salesOrder,
        DemoOrderSpec spec,
        DemoRuntime runtime,
        bool completed)
    {
        DateTime created = salesOrder.CreatedAtUtc.AddHours(8);
        PickList pickList = new()
        {
            PickListNumber = $"PL-DEMO-{spec.OrderIndex:D4}",
            Status = completed ? "Completed" : "Pending",
            CreatedAtUtc = created,
            CreatedByUserId = runtime.Context.AdminUserId,
            CompletedAtUtc = completed ? created.AddHours(2) : null
        };

        foreach (SalesOrderLine soLine in salesOrder.Lines)
        {
            pickList.Lines.Add(new PickListLine
            {
                SalesOrderLine = soLine,
                ProductId = soLine.ProductId,
                WarehouseId = salesOrder.WarehouseId,
                RequestedQuantity = soLine.OrderedQuantity,
                ActualQuantity = completed ? soLine.OrderedQuantity : null,
                Status = completed ? "Picked" : "Pending",
                PickedAtUtc = completed ? created.AddHours(2) : null,
                PickedByUserId = completed ? runtime.Context.AdminUserId : null
            });
        }

        return pickList;
    }

    /// <summary>
    /// Builds a parcel containing one item per pick list line.
    /// </summary>
    private static Parcel BuildParcel(
        SalesOrder salesOrder,
        PickList pickList,
        DemoOrderSpec spec,
        DemoRuntime runtime)
    {
        decimal weight = 0.500m + (spec.OrderIndex % 7) * 0.250m;
        Parcel parcel = new()
        {
            ParcelNumber = $"PKG-DEMO-{spec.OrderIndex:D4}",
            Weight = weight,
            Length = 30.00m,
            Width = 20.00m,
            Height = 10.00m + (spec.OrderIndex % 4) * 2.00m,
            Notes = $"Demo parcel for {salesOrder.OrderNumber}",
            CreatedAtUtc = salesOrder.CreatedAtUtc.AddHours(11),
            CreatedByUserId = runtime.Context.AdminUserId
        };

        foreach (PickListLine pickLine in pickList.Lines)
        {
            parcel.Items.Add(new ParcelItem
            {
                PickListLine = pickLine,
                ProductId = pickLine.ProductId,
                Quantity = pickLine.ActualQuantity ?? pickLine.RequestedQuantity
            });
        }

        return parcel;
    }

    /// <summary>
    /// Builds the shipment header with status-aware values. Uses the SalesOrder navigation property so
    /// EF resolves SalesOrderId during SaveChanges (the SO has not been persisted yet at this point).
    /// </summary>
    private static Shipment BuildShipment(SalesOrder salesOrder, DemoOrderSpec spec, DemoRuntime runtime)
    {
        bool delivered = spec.Status == "Completed";
        DateTime dispatched = salesOrder.ShippedAtUtc ?? salesOrder.CreatedAtUtc.AddDays(2);
        string shipmentStatus = delivered ? "Delivered" : "InTransit";
        string trackingNumber = $"WHTRK{spec.OrderIndex:D7}";

        return new Shipment
        {
            ShipmentNumber = $"SH-DEMO-{spec.OrderIndex:D4}",
            SalesOrder = salesOrder,
            CarrierId = salesOrder.CarrierId,
            CarrierServiceLevelId = salesOrder.CarrierServiceLevelId,
            Status = shipmentStatus,
            ShippingStreetLine1 = salesOrder.ShippingStreetLine1,
            ShippingStreetLine2 = salesOrder.ShippingStreetLine2,
            ShippingCity = salesOrder.ShippingCity,
            ShippingStateProvince = salesOrder.ShippingStateProvince,
            ShippingPostalCode = salesOrder.ShippingPostalCode,
            ShippingCountryCode = salesOrder.ShippingCountryCode,
            TrackingNumber = trackingNumber,
            TrackingUrl = $"https://track.example.com/{trackingNumber}",
            DispatchedAtUtc = dispatched,
            DispatchedByUserId = runtime.Context.AdminUserId
        };
    }

    /// <summary>
    /// Attaches one shipment line per sales-order line.
    /// </summary>
    private static void AttachShipmentLines(Shipment shipment, ICollection<SalesOrderLine> salesOrderLines)
    {
        foreach (SalesOrderLine soLine in salesOrderLines)
        {
            shipment.Lines.Add(new ShipmentLine
            {
                SalesOrderLine = soLine,
                ProductId = soLine.ProductId,
                Quantity = soLine.ShippedQuantity
            });
        }
    }

    /// <summary>
    /// Builds Dispatched / InTransit / Delivered tracking entries depending on the SO status.
    /// </summary>
    private static void AttachTrackingEntries(Shipment shipment, DemoOrderSpec spec, int adminUserId)
    {
        DateTime dispatched = shipment.DispatchedAtUtc;
        shipment.TrackingEntries.Add(new ShipmentTracking
        {
            Status = "Dispatched",
            Notes = "Package handed over to carrier.",
            OccurredAtUtc = dispatched,
            RecordedByUserId = adminUserId
        });

        shipment.TrackingEntries.Add(new ShipmentTracking
        {
            Status = "InTransit",
            Notes = "Package scanned at carrier hub.",
            OccurredAtUtc = dispatched.AddHours(18),
            RecordedByUserId = adminUserId
        });

        if (spec.Status == "Completed")
        {
            shipment.TrackingEntries.Add(new ShipmentTracking
            {
                Status = "Delivered",
                Notes = "Package delivered to recipient.",
                OccurredAtUtc = dispatched.AddDays(3),
                RecordedByUserId = adminUserId
            });
        }
    }

    /// <summary>
    /// Queues lifecycle event rows reflecting the status the SO has reached. Events are bulk-inserted
    /// after all SOs have been persisted.
    /// </summary>
    private static void QueueLifecycleEvents(
        SalesOrder salesOrder,
        DemoOrderSpec spec,
        int adminUserId,
        List<FulfillmentEvent> eventBatch)
    {
        DateTime baseTime = salesOrder.CreatedAtUtc;
        AppendEvent(eventBatch, "SalesOrderCreated", "SalesOrder", salesOrder.Id, adminUserId, baseTime);

        switch (spec.Status)
        {
            case "Confirmed":
                AppendEvent(eventBatch, "SalesOrderConfirmed", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(6));
                break;
            case "Picking":
                AppendEvent(eventBatch, "SalesOrderConfirmed", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(6));
                AppendEvent(eventBatch, "PickListGenerated", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(8));
                break;
            case "Packed":
                AppendEvent(eventBatch, "SalesOrderConfirmed", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(6));
                AppendEvent(eventBatch, "PickListGenerated", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(8));
                AppendEvent(eventBatch, "PickListCompleted", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(10));
                AppendEvent(eventBatch, "ParcelCreated", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(11));
                break;
            case "Shipped":
                QueueShippedLifecycleEvents(eventBatch, salesOrder.Id, adminUserId, baseTime);
                break;
            case "Completed":
                QueueShippedLifecycleEvents(eventBatch, salesOrder.Id, adminUserId, baseTime);
                AppendEvent(eventBatch, "SalesOrderCompleted", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddDays(7));
                break;
            case "Cancelled":
                AppendEvent(eventBatch, "SalesOrderCancelled", "SalesOrder", salesOrder.Id, adminUserId,
                    baseTime.AddHours(12));
                break;
        }
    }

    /// <summary>
    /// Helper that appends the lifecycle event chain for any SO that has reached the Shipped status.
    /// </summary>
    private static void QueueShippedLifecycleEvents(
        List<FulfillmentEvent> eventBatch,
        int salesOrderId,
        int adminUserId,
        DateTime baseTime)
    {
        AppendEvent(eventBatch, "SalesOrderConfirmed", "SalesOrder", salesOrderId, adminUserId,
            baseTime.AddHours(6));
        AppendEvent(eventBatch, "PickListGenerated", "SalesOrder", salesOrderId, adminUserId,
            baseTime.AddHours(8));
        AppendEvent(eventBatch, "PickListCompleted", "SalesOrder", salesOrderId, adminUserId,
            baseTime.AddHours(10));
        AppendEvent(eventBatch, "ParcelCreated", "SalesOrder", salesOrderId, adminUserId,
            baseTime.AddHours(11));
        AppendEvent(eventBatch, "ShipmentDispatched", "SalesOrder", salesOrderId, adminUserId,
            baseTime.AddDays(2));
    }

    /// <summary>
    /// Appends a single event row to the batch.
    /// </summary>
    private static void AppendEvent(
        List<FulfillmentEvent> batch,
        string eventType,
        string entityType,
        int entityId,
        int userId,
        DateTime occurredAt)
    {
        batch.Add(new FulfillmentEvent
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            OccurredAtUtc = occurredAt
        });
    }

    /// <summary>
    /// Seeds every demo customer return. Idempotent by ReturnNumber.
    /// </summary>
    private async Task SeedCustomerReturnsAsync(
        DemoRuntime runtime,
        IReadOnlyDictionary<string, int> seededOrderIds,
        CancellationToken cancellationToken)
    {
        if (runtime.Context.WarehouseIds.Count == 0)
        {
            return;
        }

        List<FulfillmentEvent> pendingEvents = new();

        foreach (DemoReturnSpec spec in FulfillmentSeedData.DemoReturns)
        {
            int? existingId = await GetCustomerReturnIdAsync(spec.ReturnNumber, cancellationToken)
                .ConfigureAwait(false);
            if (existingId is not null)
            {
                continue;
            }

            int linkedOrderIndex = CompletedOrderStartIndex
                + (spec.LinkedCompletedOrderIndex % CompletedOrderCount);
            string linkedOrderNumber = $"SO-DEMO-{linkedOrderIndex:D4}";
            if (!seededOrderIds.TryGetValue(linkedOrderNumber, out int salesOrderId))
            {
                continue;
            }

            CustomerReturn ret = BuildCustomerReturnGraph(spec, salesOrderId, runtime);
            _context.CustomerReturns.Add(ret);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            QueueReturnLifecycleEvents(ret, spec, runtime.Context.AdminUserId, pendingEvents);
        }

        if (pendingEvents.Count > 0)
        {
            _context.FulfillmentEvents.AddRange(pendingEvents);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds a customer return graph (header + lines) using the linked SO and admin user.
    /// </summary>
    private static CustomerReturn BuildCustomerReturnGraph(
        DemoReturnSpec spec,
        int salesOrderId,
        DemoRuntime runtime)
    {
        DateTime created = DateTime.UtcNow.AddDays(-spec.DaysAgoCreated);
        CustomerProfile customer = runtime.Context.Customers[
            (spec.ReturnIndex - 1) % runtime.Context.Customers.Count];

        CustomerReturn ret = new()
        {
            ReturnNumber = spec.ReturnNumber,
            CustomerId = customer.CustomerId,
            SalesOrderId = salesOrderId,
            Status = spec.Status,
            Reason = spec.Reason,
            Notes = $"Demo RMA #{spec.ReturnIndex} ({spec.Status}). Safe to delete.",
            CreatedAtUtc = created,
            CreatedByUserId = runtime.Context.AdminUserId
        };

        ApplyReturnStatusTimestamps(ret, spec, runtime.Context.AdminUserId, created);
        AddReturnLines(ret, spec, runtime);
        return ret;
    }

    /// <summary>
    /// Sets ConfirmedAt/ReceivedAt/ClosedAt timestamps according to the return status.
    /// </summary>
    private static void ApplyReturnStatusTimestamps(
        CustomerReturn ret,
        DemoReturnSpec spec,
        int adminUserId,
        DateTime created)
    {
        switch (spec.Status)
        {
            case "Confirmed":
                ret.ConfirmedAtUtc = created.AddHours(4);
                ret.ConfirmedByUserId = adminUserId;
                break;
            case "Received":
                ret.ConfirmedAtUtc = created.AddHours(4);
                ret.ConfirmedByUserId = adminUserId;
                ret.ReceivedAtUtc = created.AddDays(2);
                ret.ReceivedByUserId = adminUserId;
                break;
            case "Closed":
                ret.ConfirmedAtUtc = created.AddHours(4);
                ret.ConfirmedByUserId = adminUserId;
                ret.ReceivedAtUtc = created.AddDays(2);
                ret.ReceivedByUserId = adminUserId;
                ret.ClosedAtUtc = created.AddDays(5);
                ret.ClosedByUserId = adminUserId;
                break;
            case "Cancelled":
                ret.ClosedAtUtc = created.AddHours(8);
                ret.ClosedByUserId = adminUserId;
                break;
        }
    }

    /// <summary>
    /// Adds 1–3 lines to the return, rotating products and warehouses.
    /// </summary>
    private static void AddReturnLines(CustomerReturn ret, DemoReturnSpec spec, DemoRuntime runtime)
    {
        IReadOnlyList<int> productIds = runtime.Context.ProductIds;
        IReadOnlyList<int> warehouseIds = runtime.Context.WarehouseIds;
        int seqIndex = spec.ReturnIndex - 1;
        for (int lineIdx = 0; lineIdx < spec.LineCount; lineIdx++)
        {
            int productId = productIds[(seqIndex * 2 + lineIdx) % productIds.Count];
            int warehouseId = warehouseIds[seqIndex % warehouseIds.Count];
            decimal quantity = 1m + ((seqIndex + lineIdx) % 4);
            ret.Lines.Add(new CustomerReturnLine
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = quantity,
                Notes = lineIdx == 0 ? spec.Reason : null
            });
        }
    }

    /// <summary>
    /// Queues lifecycle events reflecting the return's current status.
    /// </summary>
    private static void QueueReturnLifecycleEvents(
        CustomerReturn ret,
        DemoReturnSpec spec,
        int adminUserId,
        List<FulfillmentEvent> eventBatch)
    {
        DateTime baseTime = ret.CreatedAtUtc;
        AppendEvent(eventBatch, "CustomerReturnCreated", "CustomerReturn", ret.Id, adminUserId, baseTime);

        switch (spec.Status)
        {
            case "Confirmed":
                AppendEvent(eventBatch, "CustomerReturnConfirmed", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddHours(4));
                break;
            case "Received":
                AppendEvent(eventBatch, "CustomerReturnConfirmed", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddHours(4));
                AppendEvent(eventBatch, "CustomerReturnReceived", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddDays(2));
                break;
            case "Closed":
                AppendEvent(eventBatch, "CustomerReturnConfirmed", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddHours(4));
                AppendEvent(eventBatch, "CustomerReturnReceived", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddDays(2));
                AppendEvent(eventBatch, "CustomerReturnClosed", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddDays(5));
                break;
            case "Cancelled":
                AppendEvent(eventBatch, "CustomerReturnCancelled", "CustomerReturn", ret.Id, adminUserId,
                    baseTime.AddHours(8));
                break;
        }
    }

    /// <summary>
    /// Resolves the cross-schema dependencies required to seed demo data: admin user, top 5 customers
    /// (with their first account/currency), top 3 warehouses, top 10 products. Returns <c>null</c>
    /// when any required reference is missing.
    /// </summary>
    private async Task<DemoContext?> ResolveDemoContextAsync(CancellationToken cancellationToken)
    {
        int? adminUserId = await ResolveAdminUserIdAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CustomerProfile> customers =
            await ResolveCustomerProfilesAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<int> warehouseIds = await QueryListIntAsync(
                "SELECT TOP 3 Id AS [Value] FROM inventory.Warehouses ORDER BY Id", cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<int> productIds = await QueryListIntAsync(
                "SELECT TOP 10 Id AS [Value] FROM inventory.Products ORDER BY Id", cancellationToken)
            .ConfigureAwait(false);

        if (adminUserId is null || customers.Count == 0 || warehouseIds.Count == 0 || productIds.Count == 0)
        {
            _logger.LogWarning(
                "Skipping Fulfillment demo seeding — missing cross-schema references " +
                "(admin={Admin} customers={Customers} warehouses={Warehouses} products={Products})",
                adminUserId, customers.Count, warehouseIds.Count, productIds.Count);
            return null;
        }

        return new DemoContext(adminUserId.Value, customers, warehouseIds, productIds);
    }

    /// <summary>
    /// Resolves up to 5 customer profiles. For each customer we fetch the first account and its currency.
    /// Customers without any account are skipped silently.
    /// </summary>
    private async Task<IReadOnlyList<CustomerProfile>> ResolveCustomerProfilesAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<int> customerIds = await QueryListIntAsync(
                "SELECT TOP 5 Id AS [Value] FROM customers.Customers ORDER BY Id", cancellationToken)
            .ConfigureAwait(false);

        List<CustomerProfile> profiles = new();
        foreach (int customerId in customerIds)
        {
            int? accountId = await QueryScalarIntAsync(
                    $"SELECT TOP 1 Id AS [Value] FROM customers.CustomerAccounts WHERE CustomerId = {customerId} ORDER BY Id",
                    cancellationToken)
                .ConfigureAwait(false);
            if (accountId is null)
            {
                continue;
            }

            string? currency = await QueryScalarStringAsync(
                    $"SELECT TOP 1 CurrencyCode AS [Value] FROM customers.CustomerAccounts WHERE Id = {accountId}",
                    cancellationToken)
                .ConfigureAwait(false);
            if (currency is null)
            {
                continue;
            }

            profiles.Add(new CustomerProfile(customerId, accountId.Value, currency));
        }

        return profiles;
    }

    /// <summary>
    /// Resolves the seeded carrier service-level pairs in (CarrierId, ServiceLevelId) form.
    /// </summary>
    private async Task<IReadOnlyList<CarrierServicePair>> ResolveCarrierServicePairsAsync(
        CancellationToken cancellationToken)
    {
        List<CarrierServicePair> pairs = await _context.CarrierServiceLevels
            .OrderBy(l => l.CarrierId).ThenBy(l => l.Id)
            .Select(l => new CarrierServicePair(l.CarrierId, l.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return pairs;
    }

    /// <summary>
    /// Returns the SO ID for the given OrderNumber, or null when missing.
    /// </summary>
    private async Task<int?> GetSalesOrderIdAsync(string orderNumber, CancellationToken cancellationToken)
    {
        int id = await _context.SalesOrders
            .Where(so => so.OrderNumber == orderNumber)
            .Select(so => so.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return id == 0 ? null : id;
    }

    /// <summary>
    /// Returns the customer return ID for the given ReturnNumber, or null when missing.
    /// </summary>
    private async Task<int?> GetCustomerReturnIdAsync(string returnNumber, CancellationToken cancellationToken)
    {
        int id = await _context.CustomerReturns
            .Where(r => r.ReturnNumber == returnNumber)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return id == 0 ? null : id;
    }

    /// <summary>
    /// Resolves the admin user ID by Username. Returns <c>null</c> when not found.
    /// </summary>
    private async Task<int?> ResolveAdminUserIdAsync(CancellationToken cancellationToken)
    {
        return await QueryScalarIntAsync(
                "SELECT TOP 1 Id AS [Value] FROM auth.Users WHERE Username = 'admin' ORDER BY Id",
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a raw SQL query returning a single int column aliased as <c>[Value]</c>;
    /// returns <c>null</c> on error or empty.
    /// </summary>
    private async Task<int?> QueryScalarIntAsync(string sql, CancellationToken cancellationToken)
    {
        try
        {
            List<int> rows = await _context.Database
                .SqlQueryRaw<int>(sql)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows.Count == 0 ? null : rows[0];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Demo-seed lookup failed: {Sql}", sql);
            return null;
        }
    }

    /// <summary>
    /// Executes a raw SQL query returning a single string column aliased as <c>[Value]</c>;
    /// returns <c>null</c> on error or empty.
    /// </summary>
    private async Task<string?> QueryScalarStringAsync(string sql, CancellationToken cancellationToken)
    {
        try
        {
            List<string> rows = await _context.Database
                .SqlQueryRaw<string>(sql)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows.Count == 0 ? null : rows[0];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Demo-seed lookup failed: {Sql}", sql);
            return null;
        }
    }

    /// <summary>
    /// Executes a raw SQL query returning a list of int values from a single column aliased as <c>[Value]</c>.
    /// Returns an empty list on error.
    /// </summary>
    private async Task<IReadOnlyList<int>> QueryListIntAsync(string sql, CancellationToken cancellationToken)
    {
        try
        {
            List<int> rows = await _context.Database
                .SqlQueryRaw<int>(sql)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Demo-seed lookup failed: {Sql}", sql);
            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Resolved cross-schema dependencies required to seed demo data.
    /// </summary>
    private sealed record DemoContext(
        int AdminUserId,
        IReadOnlyList<CustomerProfile> Customers,
        IReadOnlyList<int> WarehouseIds,
        IReadOnlyList<int> ProductIds);

    /// <summary>
    /// Customer + first-account profile used to populate sales-order header fields.
    /// </summary>
    private sealed record CustomerProfile(int CustomerId, int AccountId, string CurrencyCode);

    /// <summary>
    /// (CarrierId, ServiceLevelId) tuple used to rotate carriers across demo orders.
    /// </summary>
    private sealed record CarrierServicePair(int CarrierId, int ServiceLevelId);

    /// <summary>
    /// Runtime aggregate combining the demo context with the resolved carrier pairs.
    /// </summary>
    private sealed record DemoRuntime(
        DemoContext Context,
        IReadOnlyList<CarrierServicePair> CarrierPairs);
}
