using System;
using NetTopologySuite.Geometries;

public class IndoorPOI : poi.POI
{
    public Action<Point> OnLocationPointUpdate;
    public Point point
    {
        get => (Point)location.point.geometry;
        set
        {
            location.point.geometry = value;
            OnLocationPointUpdate?.Invoke(value);
        }
    }

}
