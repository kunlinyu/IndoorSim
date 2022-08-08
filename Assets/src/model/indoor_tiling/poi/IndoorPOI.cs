using System;
using NetTopologySuite.Geometries;

public class IndoorPOI : poi.POI
{
    public Action OnLocationPointUpdate;

    public string indoorPOIType { get; private set; }

    public IndoorPOI(string type)
    {
        this.indoorPOIType = type;
        AddLabel(type);
    }

    public Point point
    {
        get => (Point)location.point.geometry;
        set
        {
            location.point.geometry = value;
            OnLocationPointUpdate?.Invoke();
        }
    }

}
