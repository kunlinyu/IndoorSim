using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;

public class HumanPOI : IndoorPOI
{
    public HumanPOI(Point point, ICollection<Container> spaces) : base("human", spaces)
    {
        location.point.geometry = point;
    }
}
