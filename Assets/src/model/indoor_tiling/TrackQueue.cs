using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

public class TrackQueue : IndoorPOI
{
    public TrackQueue(Point point, ICollection<Container> spaces, LineString track, params string[] category)
        : base(point, spaces, category)
    {
        this.track = track;
        AddCategory("TrackQueue");
    }

    [JsonIgnore]
    public LineString track
    {
        get => (LineString)location.line.geometry;
        set
        {
            location.line.geometry = value;
            OnLocationUpdate?.Invoke();
        }
    }
}
