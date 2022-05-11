using System;
using Newtonsoft.Json;

public struct Asset
{
    [JsonPropertyAttribute] public string name;
    [JsonPropertyAttribute] public string thumbnailBase64;
    [JsonPropertyAttribute] public DateTime dateTime;
    [JsonPropertyAttribute] public int verticesCount;
    [JsonPropertyAttribute] public int boundariesCount;
    [JsonPropertyAttribute] public int spacesCount;
    [JsonPropertyAttribute] public double centerX;
    [JsonPropertyAttribute] public double centerY;
    [JsonPropertyAttribute] public string json;
}
