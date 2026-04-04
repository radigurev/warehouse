namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the type of a customer email address.
/// </summary>
public enum EmailType
{
    /// <summary>
    /// General-purpose email address.
    /// </summary>
    General,

    /// <summary>
    /// Email address used for billing correspondence.
    /// </summary>
    Billing,

    /// <summary>
    /// Email address used for support inquiries.
    /// </summary>
    Support
}
