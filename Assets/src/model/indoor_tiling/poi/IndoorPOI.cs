using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable

public class IndoorPOI : poi.POI
{
    public List<Container> spaces = new List<Container>();

    // TODO: add layOnSpace, related space, and move POI when space moved
    [JsonIgnore] public Action OnLocationPointUpdate = () => { };

    public virtual bool CanLayOn(Container? container)
    => container != null && container.navigable == Navigable.Navigable;

    public virtual bool AcceptContainer(Container? container)
        => container != null && container.navigable != Navigable.Navigable;

    public IndoorPOI() {}

    public IndoorPOI(Point point, ICollection<Container> spaces, params string[] category)
    {
        this.point = point;
        this.spaces = new List<Container>(spaces);
        foreach (var cate in category)
            AddCategory(cate);
    }

    [JsonIgnore] public Point point
    {
        get => (Point)location.point.geometry;
        set
        {
            location.point.geometry = value;
            location.point.term = "navigation point";
            OnLocationPointUpdate?.Invoke();
        }
    }

}
