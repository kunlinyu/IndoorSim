using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

struct BoundaryType
{

}
public class CellBoundary
{
    [JsonPropertyAttribute] private LineString geom;
    [JsonPropertyAttribute] public CellVertex P0 { get; private set; }
    [JsonPropertyAttribute] public CellVertex P1 { get; private set; }

    //      P1
    //      |
    // left | right
    //      |
    //      P0

    // navigable: at least one of two boolean variables below are true
    // non-navigable: both of these two boolean variables below are false
    [JsonPropertyAttribute] public bool right2Left = true;
    [JsonPropertyAttribute] public bool left2Right = true;

    // left/right Functional == true means agents may stop at the left/right side of this boundary to do something
    [JsonPropertyAttribute] public bool leftFunctional = false;
    [JsonPropertyAttribute] public bool rightFunctional = false;

    public CellBoundary(LineString ls, CellVertex p0, CellVertex p1)
    {
        geom = ls;
        P0 = p0;
        P1 = p1;
    }
}
