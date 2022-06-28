using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable
public class BoundaryDirectionEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { set; get; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    void Start()
    {
        MousePickController.pickType = CurrentPickType.Boundary;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
        {
            Selectable? pointed = MousePickController.PointedEntity;
            if (pointed == null) return;
            if (pointed.type != SelectableType.Boundary) return;

            BoundaryController bc = (BoundaryController)pointed;
            if (bc.Boundary.SmartNavigable() != Navigable.Navigable) return;
            switch (bc.Boundary.NaviDir)
            {
                case NaviDirection.BiDirection:
                    IndoorSimData?.UpdateBoundaryNaviDirection(bc.Boundary, NaviDirection.Left2Right);
                    break;
                case NaviDirection.Left2Right:
                    IndoorSimData?.UpdateBoundaryNaviDirection(bc.Boundary, NaviDirection.Right2Left);
                    break;
                case NaviDirection.Right2Left:
                    IndoorSimData?.UpdateBoundaryNaviDirection(bc.Boundary, NaviDirection.NoneDirection);
                    break;
                case NaviDirection.NoneDirection:
                    IndoorSimData?.UpdateBoundaryNaviDirection(bc.Boundary, NaviDirection.BiDirection);
                    break;
            }
        }
    }
}
