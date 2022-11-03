using System;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// TODO: move this file to a better place

public class ShouldSerializeContractResolver : DefaultContractResolver
{
    public static readonly ShouldSerializeContractResolver Instance = new();

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        if (property.DeclaringType == typeof(poi.POIProperties) && property.PropertyType == typeof(DateTime))
        {
            property.ShouldSerialize =
                instance =>
                {
                    poi.POIProperties poip = (poi.POIProperties)instance;
                    DateTime dt = (DateTime)(poip.GetType().GetField(property.PropertyName).GetValue(poip));
                    return dt != DateTime.MinValue;
                };
        }
        if (property.DeclaringType == typeof(ThematicLayer) && property.PropertyType == typeof(DateTime))
        {
            property.ShouldSerialize =
                instance =>
                {
                    ThematicLayer layer = (ThematicLayer)instance;
                    DateTime dt = (DateTime)(layer.GetType().GetField(property.PropertyName).GetValue(layer));
                    return dt != DateTime.MinValue;
                };
        }
        return property;
    }

    protected override JsonContract CreateContract(Type objectType)
    {
        JsonContract contract = base.CreateContract(objectType);

        if (StackConverter.StackParameterType(objectType) != null)
        {
            contract.Converter = new StackConverter();
        }

        return contract;
    }
}
