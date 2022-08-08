using System;
using NetTopologySuite.Geometries;

public class PaAmrPoi : IndoorPOI
{
    public Action OnUpdate;

    public HumanPOI pickingPOI;

    public PaAmrPoi(Point amrPoint, HumanPOI pickingPOI) : base("PaAmr")
    {
        location.point.geometry = amrPoint;

        this.pickingPOI = pickingPOI;
        poi.POIProperties linkProperties = new poi.POIProperties()
        {
            href = new Uri("memoryObject:/" + pickingPOI.id),
            term = "related",
        };
        link.Add(linkProperties);
    }
}
