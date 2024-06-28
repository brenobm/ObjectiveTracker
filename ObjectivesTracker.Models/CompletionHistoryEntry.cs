using Newtonsoft.Json;

namespace ObjectivesTracker.Models;

public class CompletionHistoryEntry(
    DateOnly date,
    bool completed)
{
    [JsonProperty(PropertyName = "date")]
    public DateOnly Date { get; set; } = date;
    [JsonProperty(PropertyName = "completed")]
    public bool Completed { get; set; } = completed;
}
