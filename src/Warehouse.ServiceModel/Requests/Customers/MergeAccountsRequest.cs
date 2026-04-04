namespace Warehouse.ServiceModel.Requests.Customers;

/// <summary>
/// Request payload for merging two customer accounts with the same currency.
/// </summary>
public sealed record MergeAccountsRequest
{
    /// <summary>
    /// Gets the source account ID. Balance will be transferred from this account.
    /// </summary>
    public required int SourceAccountId { get; init; }

    /// <summary>
    /// Gets the target account ID. Balance will be transferred to this account.
    /// </summary>
    public required int TargetAccountId { get; init; }
}
