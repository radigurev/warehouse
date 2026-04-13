namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Error codes for the Fulfillment domain (sales orders, picking, packing, shipping, returns).
/// </summary>
public static class FulfillmentErrorCodes
{
    /// <summary>Sales order not found.</summary>
    public const string SoNotFound = "SO_NOT_FOUND";

    /// <summary>Sales order line not found.</summary>
    public const string SoLineNotFound = "SO_LINE_NOT_FOUND";

    /// <summary>Sales order is not editable in current status.</summary>
    public const string SoNotEditable = "SO_NOT_EDITABLE";

    /// <summary>Invalid SO status transition.</summary>
    public const string InvalidSoStatusTransition = "INVALID_SO_STATUS_TRANSITION";

    /// <summary>Sales order must have at least one line.</summary>
    public const string SoMustHaveLines = "SO_MUST_HAVE_LINES";

    /// <summary>Duplicate SO line for same product.</summary>
    public const string DuplicateSoLine = "DUPLICATE_SO_LINE";

    /// <summary>SO has active pick lists.</summary>
    public const string SoHasPickLists = "SO_HAS_PICK_LISTS";

    /// <summary>SO is fully allocated.</summary>
    public const string SoFullyAllocated = "SO_FULLY_ALLOCATED";

    /// <summary>SO is already completed.</summary>
    public const string SoAlreadyCompleted = "SO_ALREADY_COMPLETED";

    /// <summary>SO is already shipped.</summary>
    public const string SoAlreadyShipped = "SO_ALREADY_SHIPPED";

    /// <summary>SO is not in pickable status.</summary>
    public const string SoNotPickable = "SO_NOT_PICKABLE";

    /// <summary>SO is not in packable status.</summary>
    public const string SoNotPackable = "SO_NOT_PACKABLE";

    /// <summary>SO is not in dispatchable status.</summary>
    public const string SoNotDispatchable = "SO_NOT_DISPATCHABLE";

    /// <summary>Pick list not found.</summary>
    public const string PickListNotFound = "PICK_LIST_NOT_FOUND";

    /// <summary>Pick list line not found.</summary>
    public const string PickListLineNotFound = "PICK_LIST_LINE_NOT_FOUND";

    /// <summary>Pick list already completed.</summary>
    public const string PickListAlreadyCompleted = "PICK_LIST_ALREADY_COMPLETED";

    /// <summary>Pick line already picked.</summary>
    public const string LineAlreadyPicked = "LINE_ALREADY_PICKED";

    /// <summary>Invalid pick line reference.</summary>
    public const string InvalidPickLine = "INVALID_PICK_LINE";

    /// <summary>Over-pick: picked quantity exceeds required.</summary>
    public const string OverPick = "OVER_PICK";

    /// <summary>Shipment not found.</summary>
    public const string ShipmentNotFound = "SHIPMENT_NOT_FOUND";

    /// <summary>Invalid shipment status transition.</summary>
    public const string InvalidShipmentStatusTransition = "INVALID_SHIPMENT_STATUS_TRANSITION";

    /// <summary>Parcel not found.</summary>
    public const string ParcelNotFound = "PARCEL_NOT_FOUND";

    /// <summary>Parcel is not editable in current status.</summary>
    public const string ParcelNotEditable = "PARCEL_NOT_EDITABLE";

    /// <summary>Parcel is empty — no items added.</summary>
    public const string EmptyParcel = "EMPTY_PARCEL";

    /// <summary>Over-pack: packed quantity exceeds picked.</summary>
    public const string OverPack = "OVER_PACK";

    /// <summary>Carrier not found.</summary>
    public const string CarrierNotFound = "CARRIER_NOT_FOUND";

    /// <summary>Duplicate carrier code.</summary>
    public const string DuplicateCarrierCode = "DUPLICATE_CARRIER_CODE";

    /// <summary>Carrier has active shipments.</summary>
    public const string CarrierHasActiveShipments = "CARRIER_HAS_ACTIVE_SHIPMENTS";

    /// <summary>Service level not found.</summary>
    public const string ServiceLevelNotFound = "SERVICE_LEVEL_NOT_FOUND";

    /// <summary>Duplicate service level code.</summary>
    public const string DuplicateServiceLevelCode = "DUPLICATE_SERVICE_LEVEL_CODE";

    /// <summary>Service level is in use by shipments.</summary>
    public const string ServiceLevelInUse = "SERVICE_LEVEL_IN_USE";

    /// <summary>Return not found.</summary>
    public const string ReturnNotFound = "RETURN_NOT_FOUND";

    /// <summary>Return already confirmed.</summary>
    public const string ReturnAlreadyConfirmed = "RETURN_ALREADY_CONFIRMED";

    /// <summary>Return already received.</summary>
    public const string ReturnAlreadyReceived = "RETURN_ALREADY_RECEIVED";

    /// <summary>Return not in closeable status.</summary>
    public const string ReturnNotCloseable = "RETURN_NOT_CLOSEABLE";

    /// <summary>Return not in receivable status.</summary>
    public const string ReturnNotReceivable = "RETURN_NOT_RECEIVABLE";

    /// <summary>Return must have at least one line.</summary>
    public const string ReturnMustHaveLines = "RETURN_MUST_HAVE_LINES";
}
