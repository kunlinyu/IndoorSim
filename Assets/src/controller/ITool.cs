using System;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public interface ITool
{
    IndoorSimData? IndoorSimData { set; get; }
    IndoorMapView? mapView { set; get; }
    SimulationView? simView { set; get; }
    bool MouseOnUI { set; get; }
}
