namespace Warehouse.Common.Enums;

/// <summary>
/// Defines when a sequence counter resets to zero.
/// Used by <c>SequenceDefinition</c> to control counter lifecycle.
/// </summary>
public enum SequenceResetPolicy
{
    /// <summary>
    /// Counter resets at the start of each calendar day (UTC).
    /// </summary>
    Daily,

    /// <summary>
    /// Counter resets at the start of each calendar month (UTC).
    /// </summary>
    Monthly,

    /// <summary>
    /// Counter never resets — monotonically increasing.
    /// </summary>
    Never
}
