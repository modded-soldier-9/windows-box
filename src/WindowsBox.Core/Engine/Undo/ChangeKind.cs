namespace WindowsBox.Core.Engine.Undo;

/// <summary>
/// Every mutable tweak must declare a change kind with a recorded prior value.
/// One-way changes are rejected by the undo layer unless explicitly flagged
/// and confirmed — Windows Box prefers reversible work.
/// </summary>
public enum ChangeKind
{
    ServiceStartType,
    RegistryValue,
    ScheduledTaskState,
    StartupEntry,
    PolicyValue,
}
