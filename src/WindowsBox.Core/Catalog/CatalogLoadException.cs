namespace WindowsBox.Core.Catalog;

public sealed class CatalogLoadException : Exception
{
    public CatalogLoadException(string message)
        : base(message)
    {
    }

    public CatalogLoadException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
