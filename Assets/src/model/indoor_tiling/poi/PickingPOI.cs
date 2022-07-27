using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;

public class PickingPOI : IndoorPOI
{
    public PickingPOI(Point point)
    {
        location.point.geometry = point;
        AddLabel("picking");
    }
}
