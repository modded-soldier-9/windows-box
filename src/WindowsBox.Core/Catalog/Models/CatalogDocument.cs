namespace WindowsBox.Core.Catalog.Models;

public sealed class CatalogDocument
{
    public required IReadOnlyList<ServiceCatalogEntry> Services { get; init; }

    public ServiceCatalogEntry? FindById(string id) =>
        Services.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public ServiceCatalogEntry? FindByName(string name) =>
        Services.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
}
