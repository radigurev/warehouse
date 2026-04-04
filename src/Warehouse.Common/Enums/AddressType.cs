namespace Warehouse.Common.Enums;

/// <summary>
/// Defines the type of a customer address.
/// </summary>
public enum AddressType
{
    /// <summary>
    /// Address used for billing purposes.
    /// </summary>
    Billing,

    /// <summary>
    /// Address used for shipping purposes.
    /// </summary>
    Shipping,

    /// <summary>
    /// Address used for both billing and shipping.
    /// </summary>
    Both
}
