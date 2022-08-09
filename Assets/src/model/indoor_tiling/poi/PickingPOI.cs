using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class HumanPOI : IndoorPOI
{
    public HumanPOI(Point point, ICollection<Container> spaces) : base("human", spaces)
    {
        this.point = point;
    }
}
