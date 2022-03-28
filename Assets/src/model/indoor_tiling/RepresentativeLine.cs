using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable
public class RepresentativeLine
{
    [JsonPropertyAttribute] private LineString geom;
    [JsonPropertyAttribute] private CellBoundary from;
    [JsonPropertyAttribute] private CellBoundary to;
    [JsonPropertyAttribute] private CellSpace through;
    public RepresentativeLine(LineString ls, CellBoundary from, CellBoundary to, CellSpace through)
    {
        geom = ls;
        this.from = from;
        this.to = to;
        this.through = through;
    }
}