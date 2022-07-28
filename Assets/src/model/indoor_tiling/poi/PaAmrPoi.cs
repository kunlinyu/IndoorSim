using System;
using NetTopologySuite.Geometries;

public class PaAmrPoi : IndoorPOI
{
    public Action OnUpdate;

    private PickingPOI pickingPOI;

    public PaAmrPoi(Point amrPoint, PickingPOI pickingPOI)
    {
        location.point.geometry = amrPoint;
        AddLabel("PaAmr");
        AddLabel("PaAmrPoi");

        this.pickingPOI = pickingPOI;
        poi.POIProperties linkProperties = new poi.POIProperties()
        {
            href = new Uri("memoryObject:/" + pickingPOI.id),
            term = "related",
        };
        link.Add(linkProperties);
    }
}
