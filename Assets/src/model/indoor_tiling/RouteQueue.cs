using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class RouteQueue : TrackQueue
{
    List<CellSpace> queue = new List<CellSpace>();

    public RouteQueue(Point point, ICollection<Container> spaces, ICollection<CellSpace> queueSpaces, params string[] category)
        : base(point,
               spaces,
               new GeometryFactory().CreateLineString(queueSpaces.Select(s => s.Geom.Centroid.Coordinate).ToArray()),
               category)
    {
        // TODO: check queueSpaces should connect as a valid path
        AddCategory("RouteQueue");
        queue = new List<CellSpace>(queueSpaces);
    }

}
