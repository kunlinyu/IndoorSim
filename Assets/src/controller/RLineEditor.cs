using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable


public class RLineEditor : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    void Start()
    {
        MousePickController.pickType = CurrentPickType.RLine;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
        {
            RLineController? pointedRLine = MousePickController.PointedRLine;
            if (pointedRLine == null) return;

            if (pointedRLine.rLine.passType == PassType.DoNotPass)
                IndoorSim.indoorTiling.UpdateRLinePassType(pointedRLine.rLine, PassType.AllowedToPass);
            else if (pointedRLine.rLine.passType == PassType.AllowedToPass)
                IndoorSim.indoorTiling.UpdateRLinePassType(pointedRLine.rLine, PassType.DoNotPass);
            else
                throw new System.Exception("unknown passtype");
        }
    }
}
