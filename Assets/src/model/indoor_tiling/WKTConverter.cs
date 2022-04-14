using System;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using Newtonsoft.Json;

public class WKTConverter : JsonConverter<Geometry>
{
    public override Geometry ReadJson(JsonReader reader, Type objectType, Geometry existingValue, bool hasExistingValue, JsonSerializer serializer)
        => new WKTReader().Read(reader.Value.ToString());

    public override void WriteJson(JsonWriter writer, Geometry value, JsonSerializer serializer)
        => writer.WriteValue(value.AsText());
}
