using System;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using Newtonsoft.Json;

public class WKTConverter : JsonConverter<Geometry>
{
    public override Geometry ReadJson(JsonReader reader, Type objectType, Geometry existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value != null)
            return new WKTReader().Read(reader.Value.ToString());
        else
            return null;
    }

    public override void WriteJson(JsonWriter writer, Geometry value, JsonSerializer serializer)
    {
        writer.WriteValue(value.AsText());
    }
}

public class CoorConverter : JsonConverter<Coordinate>
{
    public override Coordinate ReadJson(JsonReader reader, Type objectType, Coordinate existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value != null)
            return new WKTReader().Read(reader.Value.ToString()).Coordinate;
        else
            return null;
    }

    public override void WriteJson(JsonWriter writer, Coordinate value, JsonSerializer serializer)
    {
        Point p = new GeometryFactory().CreatePoint(value);
        writer.WriteValue(p.AsText());
    }
}
