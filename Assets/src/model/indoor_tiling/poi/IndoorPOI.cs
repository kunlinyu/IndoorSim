using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class IndoorPOI : poi.POI
{
    public Action OnLocationPointUpdate;

    public string indoorPOIType { get; private set; }

    public List<Container> spaces;

    public IndoorPOI(string type, ICollection<Container> spaces)
    {
        this.indoorPOIType = type;
        this.spaces = new List<Container>(spaces);
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
