using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable


public class RLineEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
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

            if (pointedRLine.rLine.pass == PassType.DoNotPass)
                IndoorSimData?.UpdateRLinePassType(pointedRLine.rLines, pointedRLine.fr, pointedRLine.to, PassType.AllowedToPass);
            else if (pointedRLine.rLine.pass == PassType.AllowedToPass)
                IndoorSimData?.UpdateRLinePassType(pointedRLine.rLines, pointedRLine.fr, pointedRLine.to, PassType.DoNotPass);
            else
                throw new System.Exception("unknown passtype");
        }
    }
}
