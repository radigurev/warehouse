namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Error codes for the Purchasing domain (suppliers, POs, goods receipts, returns).
/// </summary>
public static class PurchasingErrorCodes
{
    /// <summary>Supplier not found.</summary>
    public const string SupplierNotFound = "SUPPLIER_NOT_FOUND";

    /// <summary>Duplicate supplier code.</summary>
    public const string DuplicateSupplierCode = "DUPLICATE_SUPPLIER_CODE";

    /// <summary>Duplicate supplier email.</summary>
    public const string DuplicateSupplierEmail = "DUPLICATE_SUPPLIER_EMAIL";

    /// <summary>Supplier is inactive.</summary>
    public const string SupplierInactive = "SUPPLIER_INACTIVE";

    /// <summary>Supplier already active.</summary>
    public const string SupplierAlreadyActive = "SUPPLIER_ALREADY_ACTIVE";

    /// <summary>Supplier has open purchase orders.</summary>
    public const string SupplierHasOpenPos = "SUPPLIER_HAS_OPEN_POS";

    /// <summary>Duplicate supplier category name.</summary>
    public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";

    /// <summary>Purchase order not found.</summary>
    public const string PoNotFound = "PO_NOT_FOUND";

    /// <summary>Purchase order line not found.</summary>
    public const string PoLineNotFound = "PO_LINE_NOT_FOUND";

    /// <summary>Purchase order is not editable in current status.</summary>
    public const string PoNotEditable = "PO_NOT_EDITABLE";

    /// <summary>Invalid PO status transition.</summary>
    public const string InvalidPoStatusTransition = "INVALID_PO_STATUS_TRANSITION";

    /// <summary>Purchase order must have at least one line.</summary>
    public const string PoMustHaveLines = "PO_MUST_HAVE_LINES";

    /// <summary>Duplicate PO line for same product.</summary>
    public const string DuplicatePoLine = "DUPLICATE_PO_LINE";

    /// <summary>PO has receipts and cannot be deleted.</summary>
    public const string PoHasReceipts = "PO_HAS_RECEIPTS";

    /// <summary>PO is already closed.</summary>
    public const string PoAlreadyClosed = "PO_ALREADY_CLOSED";

    /// <summary>PO is not in receivable status.</summary>
    public const string PoNotReceivable = "PO_NOT_RECEIVABLE";

    /// <summary>Goods receipt not found.</summary>
    public const string ReceiptNotFound = "RECEIPT_NOT_FOUND";

    /// <summary>Receipt line not found.</summary>
    public const string ReceiptLineNotFound = "RECEIPT_LINE_NOT_FOUND";

    /// <summary>Goods receipt already completed.</summary>
    public const string ReceiptAlreadyCompleted = "RECEIPT_ALREADY_COMPLETED";

    /// <summary>Received quantity exceeds ordered quantity.</summary>
    public const string OverReceipt = "OVER_RECEIPT";

    /// <summary>PO line has been fully received.</summary>
    public const string LineFullyReceived = "LINE_FULLY_RECEIVED";

    /// <summary>Receipt line already inspected.</summary>
    public const string LineAlreadyInspected = "LINE_ALREADY_INSPECTED";

    /// <summary>Receipt line is not in quarantine status.</summary>
    public const string LineNotQuarantined = "LINE_NOT_QUARANTINED";

    /// <summary>Supplier return not found.</summary>
    public const string ReturnNotFound = "RETURN_NOT_FOUND";

    /// <summary>Return already confirmed.</summary>
    public const string ReturnAlreadyConfirmed = "RETURN_ALREADY_CONFIRMED";

    /// <summary>Return must have at least one line.</summary>
    public const string ReturnMustHaveLines = "RETURN_MUST_HAVE_LINES";

    /// <summary>Invalid return status for this operation.</summary>
    public const string InvalidReturnStatus = "INVALID_RETURN_STATUS";

    /// <summary>Invalid return status transition.</summary>
    public const string InvalidReturnStatusTransition = "INVALID_RETURN_STATUS_TRANSITION";
}
