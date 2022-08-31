using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable

public class IndoorPOI : poi.POI
{
    public List<Container> foi = new List<Container>();  // feature of interest
    public List<Container> queue = new List<Container>();
    public Container? layOnSpace;

    // TODO: add layOnSpace, related space, and move POI when space moved
    [JsonIgnore] public Action OnLocationUpdate = () => { };

    public virtual bool CanLayOn(Container? container)
    => container != null && container.navigable == Navigable.Navigable;

    public virtual bool AcceptContainer(Container? container)
        => container != null && container.navigable != Navigable.Navigable;

    public IndoorPOI() { }

    public IndoorPOI(Point point, Container layOn, ICollection<Container> foi, ICollection<Container> queue, params string[] category)
    {
        this.point = point;
        this.layOnSpace = layOn;
        this.foi = new List<Container>(foi);
        foreach (var cate in category)
            AddCategory(cate);
    }

    [JsonIgnore]
    public Point point
    {
        get => (Point)location.point.geometry;
        set
        {
            location.point.geometry = value;
            location.point.term = "centroid";
            OnLocationUpdate?.Invoke();
        }
    }

}
