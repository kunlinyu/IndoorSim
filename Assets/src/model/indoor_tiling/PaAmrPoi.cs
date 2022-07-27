using System;
using System.Linq;
using NetTopologySuite.Geometries;

using POI = poi.POI;
using POIS = poi.POIS;

public class PaAmrPoi : POIS
{
    public Action OnUpdate;

    PaAmrPoi(Point amrPoint, Point pickingAgentPoint)
    {
        POI amrPoi = new POI();
        amrPoi.AddLabel("amr");
        amrPoi.location.point.geometry = amrPoint;
        this.poi.Add(amrPoi);

        POI pickingAgentPoi = new POI();
        pickingAgentPoi.AddLabel("picking");
        pickingAgentPoi.location.point.geometry = pickingAgentPoint;
        this.poi.Add(pickingAgentPoi);
    }

    public void SetAmrPoint(Point point)
    {
        FindByLabel("amr").location.point.geometry = point;
        OnUpdate?.Invoke();
    }

    public void SetPickingAgentPoint(Point point)
    {
        FindByLabel("picking").location.point.geometry = point;
        OnUpdate?.Invoke();
    }

    public Point GetAmrPoint() => (Point)FindByLabel("amr").location.point.geometry;
    public Point GetPickingAgentPoint() => (Point)FindByLabel("picking").location.point.geometry;

    private POI FindByLabel(string label)
    {
        POI poi = this.poi.Find(poi => poi.LabelContains(label));
        if (poi != null)
            return poi;
        else
            throw new Exception("can not find poi with label: " + label);
    }


}
