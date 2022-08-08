using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;

public class HumanPOI : IndoorPOI
{
    public HumanPOI(Point point) : base("human")
    {
        location.point.geometry = point;
    }
}
