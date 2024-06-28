using Newtonsoft.Json;

namespace ObjectivesTracker.Models;

public class Frequency(
    FrequencyType type,
    List<int> days)
{
    [JsonProperty(PropertyName = "type")]
    public FrequencyType Type { get; set; } = type;
    [JsonProperty(PropertyName = "days")]
    public List<int> Days { get; set; } = days;
    public bool this[int index]
    {
        get => Days?.Contains(index) ?? false;
        set
        {
            Days ??= [];

            if (Days.Contains(index) && !value)
            {
                Days.Remove(index);
            }
            else if (!Days.Contains(index) && value)
            {
                Days.Add(index);
            }
        }
    }
}
