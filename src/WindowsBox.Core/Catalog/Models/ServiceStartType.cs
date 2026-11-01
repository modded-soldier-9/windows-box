namespace WindowsBox.Core.Catalog.Models;

/// <summary>
/// Catalog start type. Prefer <see cref="Manual"/> over <see cref="Disabled"/>.
/// </summary>
public enum ServiceStartType
{
    Automatic,
    AutomaticDelayed,
    Manual,
    ManualTrigger,
    Disabled,
}
