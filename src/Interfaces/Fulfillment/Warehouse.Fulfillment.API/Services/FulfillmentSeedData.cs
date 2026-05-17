namespace Warehouse.Fulfillment.API.Services;

/// <summary>
/// Immutable seed data record for a carrier and its service levels.
/// </summary>
internal sealed record CarrierSeed(
    string Code,
    string Name,
    string? ContactPhone,
    string? ContactEmail,
    string? WebsiteUrl,
    string? TrackingUrlTemplate,
    IReadOnlyList<ServiceLevelSeed> ServiceLevels);

/// <summary>
/// Immutable seed data record for a carrier service level.
/// </summary>
internal sealed record ServiceLevelSeed(
    string Code,
    string Name,
    int? EstimatedDeliveryDays,
    decimal? BaseRate,
    decimal? PerKgRate);

/// <summary>
/// Immutable seed data record for a current product price entry (no validity window).
/// </summary>
internal sealed record ProductPriceSeed(string CurrencyCode, decimal UnitPrice);

/// <summary>
/// Immutable seed data record for a historical product price window (closed validity range).
/// </summary>
internal sealed record HistoricalPriceSeed(
    string CurrencyCode,
    decimal UnitPrice,
    DateTime ValidFromUtc,
    DateTime ValidToUtc);

/// <summary>
/// Immutable seed data record for a shipping/billing address used by demo sales orders and shipments.
/// </summary>
internal sealed record AddressSeed(
    string StreetLine1,
    string City,
    string? StateProvince,
    string PostalCode,
    string CountryCode);

/// <summary>
/// Immutable specification for a demo sales order. Resolved against runtime cross-schema data
/// (customers, products, warehouses, carriers) at seed time.
/// </summary>
internal sealed record DemoOrderSpec(
    int OrderIndex,
    string OrderNumber,
    string Status,
    int DaysAgoCreated,
    int LineCount);

/// <summary>
/// Immutable specification for a demo customer return.
/// </summary>
internal sealed record DemoReturnSpec(
    int ReturnIndex,
    string ReturnNumber,
    string Status,
    int DaysAgoCreated,
    int LineCount,
    string Reason,
    int LinkedCompletedOrderIndex);

/// <summary>
/// Static container for Fulfillment domain seed data (reference + demo).
/// </summary>
internal static class FulfillmentSeedData
{
    /// <summary>
    /// Gets the canonical carrier catalog seeded into the fulfillment schema.
    /// </summary>
    public static IReadOnlyList<CarrierSeed> Carriers { get; } =
    [
        new CarrierSeed(
            Code: "UPS",
            Name: "United Parcel Service",
            ContactPhone: "+1-800-742-5877",
            ContactEmail: "tracking@ups.com",
            WebsiteUrl: "https://www.ups.com",
            TrackingUrlTemplate: "https://www.ups.com/track?tracknum={trackingNumber}",
            ServiceLevels:
            [
                new ServiceLevelSeed("GROUND", "UPS Ground", 5, 8.50m, 0.65m),
                new ServiceLevelSeed("2DAY", "UPS 2nd Day Air", 2, 18.00m, 1.20m),
                new ServiceLevelSeed("NEXTDAY", "UPS Next Day Air", 1, 32.00m, 2.10m)
            ]),
        new CarrierSeed(
            Code: "FEDEX",
            Name: "FedEx",
            ContactPhone: "+1-800-463-3339",
            ContactEmail: "customerservice@fedex.com",
            WebsiteUrl: "https://www.fedex.com",
            TrackingUrlTemplate: "https://www.fedex.com/fedextrack/?trknbr={trackingNumber}",
            ServiceLevels:
            [
                new ServiceLevelSeed("GROUND", "FedEx Ground", 5, 9.00m, 0.70m),
                new ServiceLevelSeed("EXPRESS", "FedEx Express Saver", 3, 16.50m, 1.00m),
                new ServiceLevelSeed("OVERNIGHT", "FedEx Priority Overnight", 1, 35.00m, 2.25m)
            ]),
        new CarrierSeed(
            Code: "DHL",
            Name: "DHL Express",
            ContactPhone: "+1-800-225-5345",
            ContactEmail: "customer.service@dhl.com",
            WebsiteUrl: "https://www.dhl.com",
            TrackingUrlTemplate: "https://www.dhl.com/track?AWB={trackingNumber}",
            ServiceLevels:
            [
                new ServiceLevelSeed("ECONOMY", "DHL Economy Select", 4, 12.00m, 0.95m),
                new ServiceLevelSeed("EXPRESS", "DHL Express Worldwide", 2, 28.00m, 1.80m)
            ]),
        new CarrierSeed(
            Code: "USPS",
            Name: "United States Postal Service",
            ContactPhone: "+1-800-275-8777",
            ContactEmail: null,
            WebsiteUrl: "https://www.usps.com",
            TrackingUrlTemplate: "https://tools.usps.com/go/TrackConfirmAction?tLabels={trackingNumber}",
            ServiceLevels:
            [
                new ServiceLevelSeed("GROUND", "USPS Ground Advantage", 5, 6.50m, 0.50m),
                new ServiceLevelSeed("PRIORITY", "USPS Priority Mail", 3, 10.00m, 0.75m),
                new ServiceLevelSeed("EXPRESS", "USPS Priority Mail Express", 1, 28.50m, 1.60m)
            ]),
        new CarrierSeed(
            Code: "ECONT",
            Name: "Econt Express",
            ContactPhone: "+359-700-18-040",
            ContactEmail: "support@econt.com",
            WebsiteUrl: "https://www.econt.com",
            TrackingUrlTemplate: "https://www.econt.com/services/track-shipment/{trackingNumber}",
            ServiceLevels:
            [
                new ServiceLevelSeed("STANDARD", "Econt Standard", 2, 5.00m, 0.40m),
                new ServiceLevelSeed("EXPRESS", "Econt Express", 1, 9.50m, 0.70m),
                new ServiceLevelSeed("SAME_DAY", "Econt Same Day", 0, 18.00m, 1.20m)
            ])
    ];

    /// <summary>
    /// Gets the current (open-ended) product price entries seeded per resolved demo product.
    /// </summary>
    public static IReadOnlyList<ProductPriceSeed> ProductPrices { get; } =
    [
        new ProductPriceSeed("USD", 19.9900m),
        new ProductPriceSeed("EUR", 18.5000m),
        new ProductPriceSeed("BGN", 36.1900m)
    ];

    /// <summary>
    /// Gets the historical price windows seeded per resolved demo product. Each entry is keyed by
    /// (ProductId, CurrencyCode, ValidFromUtc) and represents a closed, expired price window
    /// for time-window pricing demonstration.
    /// </summary>
    public static IReadOnlyList<HistoricalPriceSeed> HistoricalProductPrices { get; } =
    [
        new HistoricalPriceSeed("USD", 17.4900m,
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)),
        new HistoricalPriceSeed("EUR", 16.2500m,
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)),
        new HistoricalPriceSeed("BGN", 31.7800m,
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)),
        new HistoricalPriceSeed("USD", 18.9900m,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc)),
        new HistoricalPriceSeed("EUR", 17.7500m,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc))
    ];

    /// <summary>
    /// Gets the rotating address pool used for shipping/billing addresses on demo sales orders.
    /// </summary>
    public static IReadOnlyList<AddressSeed> Addresses { get; } =
    [
        new AddressSeed("ul. Vitosha 12", "Sofia", null, "1000", "BG"),
        new AddressSeed("bul. Tsar Boris III 45", "Sofia", null, "1612", "BG"),
        new AddressSeed("ul. Aleksandar Stamboliyski 67", "Plovdiv", null, "4000", "BG"),
        new AddressSeed("ul. Slivnitsa 23", "Varna", null, "9000", "BG"),
        new AddressSeed("ul. Aleksandrovska 89", "Burgas", null, "8000", "BG"),
        new AddressSeed("Friedrichstrasse 100", "Berlin", "Berlin", "10117", "DE"),
        new AddressSeed("Marienplatz 8", "Munich", "Bayern", "80331", "DE"),
        new AddressSeed("Stephansplatz 4", "Vienna", "Wien", "1010", "AT"),
        new AddressSeed("Calea Victoriei 25", "Bucharest", null, "010071", "RO"),
        new AddressSeed("Ermou 15", "Athens", null, "10563", "GR")
    ];

    /// <summary>
    /// Gets the rotating list of return reasons used for demo customer returns.
    /// </summary>
    public static IReadOnlyList<string> ReturnReasons { get; } =
    [
        "Damaged in transit",
        "Wrong product shipped",
        "Customer dissatisfaction",
        "Defective merchandise",
        "Item did not match description",
        "Late delivery",
        "Quality issue",
        "Excess quantity received",
        "Customer changed mind",
        "Warranty claim",
        "Color/size mismatch",
        "Order cancellation after dispatch"
    ];

    /// <summary>
    /// Gets the demo sales order specifications. The distribution covers all SO statuses so that the UI
    /// can exercise every pipeline stage without manually creating data.
    /// </summary>
    public static IReadOnlyList<DemoOrderSpec> DemoOrders { get; } = BuildOrderSpecs();

    /// <summary>
    /// Gets the demo customer return specifications. Returns are linked to a subset of Completed sales orders.
    /// </summary>
    public static IReadOnlyList<DemoReturnSpec> DemoReturns { get; } = BuildReturnSpecs();

    /// <summary>
    /// Builds the rotating sales-order specs (60 total) covering every status transition.
    /// </summary>
    private static List<DemoOrderSpec> BuildOrderSpecs()
    {
        IReadOnlyList<(string Status, int Count, int MinDaysAgo, int MaxDaysAgo)> distribution =
        [
            ("Draft", 8, 0, 7),
            ("Confirmed", 10, 1, 14),
            ("Picking", 12, 2, 14),
            ("Packed", 8, 3, 21),
            ("Shipped", 12, 7, 45),
            ("Completed", 8, 30, 90),
            ("Cancelled", 2, 5, 60)
        ];

        List<DemoOrderSpec> specs = new();
        int orderIndex = 0;
        foreach ((string status, int count, int minDays, int maxDays) in distribution)
        {
            for (int i = 0; i < count; i++)
            {
                orderIndex++;
                int daysAgo = count == 1
                    ? minDays
                    : minDays + (maxDays - minDays) * i / (count - 1);
                int lineCount = 1 + ((orderIndex - 1) % 5);
                specs.Add(new DemoOrderSpec(
                    OrderIndex: orderIndex,
                    OrderNumber: $"SO-DEMO-{orderIndex:D4}",
                    Status: status,
                    DaysAgoCreated: daysAgo,
                    LineCount: lineCount));
            }
        }

        return specs;
    }

    /// <summary>
    /// Builds the rotating customer-return specs (12 total) linked to Completed sales orders by index.
    /// </summary>
    private static List<DemoReturnSpec> BuildReturnSpecs()
    {
        IReadOnlyList<(string Status, int Count)> distribution =
        [
            ("Draft", 2),
            ("Confirmed", 3),
            ("Received", 4),
            ("Closed", 2),
            ("Cancelled", 1)
        ];

        List<DemoReturnSpec> specs = new();
        int returnIndex = 0;
        foreach ((string status, int count) in distribution)
        {
            for (int i = 0; i < count; i++)
            {
                returnIndex++;
                int daysAgo = 1 + ((returnIndex - 1) * 3);
                int lineCount = 1 + ((returnIndex - 1) % 3);
                string reason = ReturnReasons[(returnIndex - 1) % ReturnReasons.Count];
                int linkedIndex = (returnIndex - 1) % 8;
                specs.Add(new DemoReturnSpec(
                    ReturnIndex: returnIndex,
                    ReturnNumber: $"RMA-DEMO-{returnIndex:D4}",
                    Status: status,
                    DaysAgoCreated: daysAgo,
                    LineCount: lineCount,
                    Reason: reason,
                    LinkedCompletedOrderIndex: linkedIndex));
            }
        }

        return specs;
    }
}
