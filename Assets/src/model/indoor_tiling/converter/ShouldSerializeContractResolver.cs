using System;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using UnityEngine;

// TODO: move this file to a better place

public class ShouldSerializeContractResolver : DefaultContractResolver
{
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

        return property;
    }
}
