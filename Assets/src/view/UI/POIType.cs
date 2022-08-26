using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[CreateAssetMenu]
public class POIType : ScriptableObject
{
    public new string name;
    public bool multiRelated;
    public bool relatedNavigable;
    public bool needDirection;
    public bool relatesToCurrent;
    public bool needQueue;
    public Color color;

    public static POIType FromJson(string json)
    {
        JObject jObj = JObject.Parse(json);

        POIType result = ScriptableObject.CreateInstance<POIType>();
        result.name = jObj["name"].Value<string>();
        result.multiRelated = jObj["multiRelated"].Value<bool>();
        result.relatedNavigable = jObj["relatedNavigable"].Value<bool>();
        result.needDirection = jObj["needDirection"].Value<bool>();
        result.relatesToCurrent = jObj["relatesToCurrent"].Value<bool>();
        result.needQueue = jObj["needQueue"].Value<bool>();

        JToken color = jObj["color"];
        result.color = new Color(color["r"].Value<float>(), color["g"].Value<float>(), color["b"].Value<float>(), color["a"].Value<float>());

        return result;

    }

    public static void WriteColor(Color color, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("r");
        writer.WriteValue(color.r);
        writer.WritePropertyName("g");
        writer.WriteValue(color.g);
        writer.WritePropertyName("b");
        writer.WriteValue(color.b);
        writer.WritePropertyName("a");
        writer.WriteValue(color.a);
        writer.WriteEndObject();
    }

    public string ToJson()
    {
        StringWriter sw = new StringWriter();
        JsonTextWriter writer = new JsonTextWriter(sw);

        writer.WriteStartObject();  // {

        writer.WritePropertyName("name");
        writer.WriteValue(name);

        writer.WritePropertyName("multiRelated");
        writer.WriteValue(multiRelated);

        writer.WritePropertyName("relatedNavigable");
        writer.WriteValue(relatedNavigable);

        writer.WritePropertyName("needDirection");
        writer.WriteValue(needDirection);

        writer.WritePropertyName("relatesToCurrent");
        writer.WriteValue(relatesToCurrent);

        writer.WritePropertyName("needQueue");
        writer.WriteValue(needQueue);

        writer.WritePropertyName("color");
        WriteColor(color, writer);

        writer.WriteEndObject();  // }

        return sw.ToString();
    }
}
