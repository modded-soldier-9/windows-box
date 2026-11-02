using System.Text.Json.Serialization;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Engine.Journal;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ApplyJournal))]
[JsonSerializable(typeof(JournalEntry))]
[JsonSerializable(typeof(List<JournalEntry>))]
[JsonSerializable(typeof(ServiceStartType))]
[JsonSerializable(typeof(JournalEntryStatus))]
internal partial class JournalJsonContext : JsonSerializerContext
{
}
