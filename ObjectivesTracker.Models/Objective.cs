using Newtonsoft.Json;

namespace ObjectivesTracker.Models;

public class Objective(
    string id,
    string partitionKey,
    string name,
    string description,
    Frequency frequency,
    List<CompletionHistoryEntry> completionHistory)
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = id;
    [JsonProperty(PropertyName = "partitionKey")]
    public string PartitionKey { get; set; } = partitionKey;
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; } = name;
    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; } = description;
    [JsonProperty(PropertyName = "frequency")]
    public Frequency Frequency { get; set; } = frequency;
    [JsonProperty(PropertyName = "completionHistory")]
    public List<CompletionHistoryEntry> CompletionHistory { get; set; } = completionHistory;

    public bool IsCompletedOn(DateOnly date)
    {
        return CompletionHistory.Any(e => e.Date == date && e.Completed);
    }
}
