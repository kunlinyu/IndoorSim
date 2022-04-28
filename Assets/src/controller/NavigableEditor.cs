using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class NavigableEditor : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public MapView? mapView { set; get; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    void Start()
    {
        MousePickController.pickType = CurrentPickType.Space;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
        {
            Selectable? pointed = MousePickController.PointedEntity;
            if (pointed == null) return;
            if (pointed.type != SelectableType.Space) return;

            SpaceController sc = (SpaceController)pointed;
            switch (sc.Space.Navigable)
            {
                case Navigable.PhysicallyNonNavigable:
                    IndoorSim.indoorTiling.UpdateSpaceNavigable(sc.Space, Navigable.LogicallyNonNavigable);
                    break;
                case Navigable.LogicallyNonNavigable:
                    IndoorSim.indoorTiling.UpdateSpaceNavigable(sc.Space, Navigable.Navigable);
                    break;
                case Navigable.Navigable:
                    IndoorSim.indoorTiling.UpdateSpaceNavigable(sc.Space, Navigable.PhysicallyNonNavigable);
                    break;
            }
        }
    }
}
