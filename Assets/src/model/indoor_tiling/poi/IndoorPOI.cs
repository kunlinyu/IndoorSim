using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable

public class IndoorPOI : poi.POI
{
    public string indoorPOIType { get; private set; }

    public List<Container> spaces;

    // TODO: add layOnSpace, related space, and move POI when space moved
    [JsonIgnore] public Action OnLocationPointUpdate = () => { };

    public virtual bool CanLayOn(Container? container)
    => container != null && container.navigable == Navigable.Navigable;

    public virtual bool AcceptContainer(Container? container)
        => container != null && container.navigable != Navigable.Navigable;

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
