using System;
using Newtonsoft.Json;

[Serializable]
public struct Asset
{
    [JsonPropertyAttribute] public string name;
    [JsonPropertyAttribute] public string json;
    [JsonPropertyAttribute] public DateTime dateTime;
}
