using Newtonsoft.Json;

public  class GlobalSettings
{
    public bool ReverseMouseScroll = false;
    public string DefaultFileName = "unnamed_map.indoor.json";

    public string Serialize()
        => JsonConvert.SerializeObject(this, Formatting.Indented);

    public GlobalSettings Deserialize(string json)
    {
        if (json == "") return new();
        return JsonConvert.DeserializeObject<GlobalSettings>(json);
    }
}
