using System;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public interface ITool
{
    IndoorSimData? IndoorSimData { set; get; }
    MapView? mapView { set; get; }
    SimulationView? simView { set; get; }
    int sortingLayerId { set; get; }
    Material? draftMaterial { set; get; }
    bool MouseOnUI { set; get; }
}
