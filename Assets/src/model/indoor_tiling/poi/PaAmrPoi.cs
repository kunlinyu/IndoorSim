using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class PaAmrPoi : IndoorPOI
{
    public PaAmrPoi(Point amrPoint, ICollection<Container> spaces) : base("PaAmr", spaces)
    {
        this.point = amrPoint;
    }
}
