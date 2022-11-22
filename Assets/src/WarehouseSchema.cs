using Newtonsoft.Json;

public class WarehouseSchema
{
    public string binLocationPattern = "$(Floor)-$(Area)-$(Lane)-$(Segment)-$(Level)";
    public double defautShelvesWidth = 1.0;
    public double defautCorridorWidth = 0.8;

    public string Serialize()
    => JsonConvert.SerializeObject(this, Formatting.Indented);

    public WarehouseSchema Deserialize(string json)
        => JsonConvert.DeserializeObject<WarehouseSchema>(json);
}
