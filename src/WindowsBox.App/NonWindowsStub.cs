#if !WINDOWS
namespace WindowsBox.App;

/// <summary>
/// Allows the App project to restore/build as an empty library on non-Windows CI hosts.
/// Real WinUI sources compile only on Windows.
/// </summary>
internal static class NonWindowsStub
{
}
#endif
