using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Abstractions;

/// <summary>
/// Abstraction over SCM so Core and tests run without Windows.
/// </summary>
public interface IServiceController
{
    bool Exists(string serviceName);

    ServiceConfig? GetConfig(string serviceName);

    IReadOnlyList<string> EnumerateServices();

    void SetStartType(string serviceName, ServiceStartType startType, bool delayedAutostart = false);
}
