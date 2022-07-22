using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class NavigableEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { set; get; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    void Start()
    {
        MousePickController.pickType = CurrentPickType.Space | CurrentPickType.Boundary;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
        {
            Selectable? pointed = MousePickController.PointedEntity;
            if (pointed == null) return;

            if (pointed.type == SelectableType.Space)
            {
                SpaceController sc = (SpaceController)pointed;
                switch (sc.Space.Navigable)
                {
                    case Navigable.PhysicallyNonNavigable:
                        IndoorSimData?.UpdateSpaceNavigable(sc.Space, Navigable.LogicallyNonNavigable);
                        break;
                    case Navigable.LogicallyNonNavigable:
                        IndoorSimData?.UpdateSpaceNavigable(sc.Space, Navigable.Navigable);
                        break;
                    case Navigable.Navigable:
                        IndoorSimData?.UpdateSpaceNavigable(sc.Space, Navigable.PhysicallyNonNavigable);
                        break;
                }
            }
            else if (pointed.type == SelectableType.Boundary)
            {
                BoundaryController bc = (BoundaryController)pointed;
                switch (bc.Boundary.Navigable)
                {
                    case Navigable.PhysicallyNonNavigable:
                        IndoorSimData?.UpdateBoundaryNavigable(bc.Boundary, Navigable.LogicallyNonNavigable);
                        break;
                    case Navigable.LogicallyNonNavigable:
                        IndoorSimData?.UpdateBoundaryNavigable(bc.Boundary, Navigable.Navigable);
                        break;
                    case Navigable.Navigable:
                        IndoorSimData?.UpdateBoundaryNavigable(bc.Boundary, Navigable.PhysicallyNonNavigable);
                        break;
                }

            }
            else
            {
                throw new System.Exception("unexpected pointed type: " + pointed.type);
            }
        }
    }
}
