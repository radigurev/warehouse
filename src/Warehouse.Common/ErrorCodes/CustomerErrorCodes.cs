namespace Warehouse.Common.ErrorCodes;

/// <summary>
/// Error codes for the Customers domain.
/// </summary>
public static class CustomerErrorCodes
{
    /// <summary>Customer not found.</summary>
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";

    /// <summary>Duplicate customer code.</summary>
    public const string DuplicateCode = "DUPLICATE_CUSTOMER_CODE";

    /// <summary>Duplicate tax ID.</summary>
    public const string DuplicateTaxId = "DUPLICATE_TAX_ID";

    /// <summary>Duplicate customer email.</summary>
    public const string DuplicateEmail = "DUPLICATE_CUSTOMER_EMAIL";

    /// <summary>Duplicate currency account for customer.</summary>
    public const string DuplicateCurrencyAccount = "DUPLICATE_CURRENCY_ACCOUNT";

    /// <summary>Category not found.</summary>
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";

    /// <summary>Duplicate category name.</summary>
    public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";

    /// <summary>Category is in use by customers.</summary>
    public const string CategoryInUse = "CATEGORY_IN_USE";

    /// <summary>Category has child categories.</summary>
    public const string CategoryHasChildren = "CATEGORY_HAS_CHILDREN";

    /// <summary>Category cannot be its own parent.</summary>
    public const string CategorySelfParent = "CATEGORY_SELF_PARENT";

    /// <summary>Address not found.</summary>
    public const string AddressNotFound = "ADDRESS_NOT_FOUND";

    /// <summary>Phone not found.</summary>
    public const string PhoneNotFound = "PHONE_NOT_FOUND";

    /// <summary>Email not found.</summary>
    public const string EmailNotFound = "EMAIL_NOT_FOUND";

    /// <summary>Account not found.</summary>
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";

    /// <summary>Cannot deactivate last active account.</summary>
    public const string LastActiveAccount = "LAST_ACTIVE_ACCOUNT";

    /// <summary>Account has non-zero balance.</summary>
    public const string AccountHasBalance = "ACCOUNT_HAS_BALANCE";

    /// <summary>Cannot merge accounts with different currencies.</summary>
    public const string MergeCurrencyMismatch = "MERGE_CURRENCY_MISMATCH";

    /// <summary>Cannot merge accounts from different customers.</summary>
    public const string MergeDifferentCustomers = "MERGE_DIFFERENT_CUSTOMERS";

    /// <summary>Cannot merge account with itself.</summary>
    public const string MergeSelfNotAllowed = "MERGE_SELF_NOT_ALLOWED";
}
