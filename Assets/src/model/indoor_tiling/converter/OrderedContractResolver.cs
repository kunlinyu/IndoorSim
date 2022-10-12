using System.Linq;

public class OrderedContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
    protected override System.Collections.Generic.IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(System.Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
        => base.CreateProperties(type, memberSerialization).OrderByDescending(p => p.PropertyName).ToList();
}