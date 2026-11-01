namespace WindowsBox.Core.Catalog.Models;

public enum TouchPolicy
{
    /// <summary>Engine may change start type when recommended.</summary>
    Allow,

    /// <summary>Catalogued for awareness; do not recommend changes.</summary>
    Leave,

    /// <summary>Hard refuse — boot/security/update critical.</summary>
    Never,

    /// <summary>Offer policy alternative only; do not sc config.</summary>
    PolicyOnly,
}
