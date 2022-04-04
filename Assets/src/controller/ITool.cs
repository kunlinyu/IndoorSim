using System;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public interface ITool
{
    IndoorSim? IndoorSim { set; get; }
    int sortingLayerId { set; get; }
    Material? draftMaterial { set; get; }
    bool MouseOnUI { set; get; }
}
