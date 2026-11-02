using System.Diagnostics;
using System.Reflection;

namespace WindowsBox.Tests;

public class ElevatorAdminGuardTests
{
    [Fact]
    public void Elevator_source_requires_admin_for_mutating_commands()
    {
        // Guardrail test: mutating entry points must call RequireAdminThen.
        var elevatorProgram = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "WindowsBox.Elevator", "Program.cs"));
        Assert.True(File.Exists(elevatorProgram), $"Missing {elevatorProgram}");
        var source = File.ReadAllText(elevatorProgram);
        Assert.Contains("RequireAdminThen", source, StringComparison.Ordinal);
        Assert.Contains("Refusing to mutate services", source, StringComparison.Ordinal);
        Assert.Contains("--apply-plan\" => RequireAdminThen", source, StringComparison.Ordinal);
        Assert.Contains("--rollback-journal\" => RequireAdminThen", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Ping_remains_allowed_without_admin_gate()
    {
        var elevatorProgram = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "WindowsBox.Elevator", "Program.cs"));
        var source = File.ReadAllText(elevatorProgram);
        Assert.Contains("\"--ping\" => Ping()", source, StringComparison.Ordinal);
    }
}
