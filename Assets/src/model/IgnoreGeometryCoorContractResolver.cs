using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Serialization;

class IgnoreGeometryCoorContractResolver : DefaultContractResolver
{
    protected override JsonContract CreateContract(Type objectType)
    {
        JsonContract contract = base.CreateContract(objectType);

        if (objectType.IsSubclassOf(typeof(Geometry)) || objectType == typeof(Geometry))
            contract.Converter = new WKTConverter();

        if (objectType == typeof(Coordinate))
            contract.Converter = new CoorConverter();

        return contract;
    }
}