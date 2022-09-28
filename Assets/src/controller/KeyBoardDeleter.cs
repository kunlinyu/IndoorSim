using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class KeyBoardDeleter : MonoBehaviour, ITool
{
    public IndoorSimData IndoorSimData { get; set; }
    public IndoorMapView mapView { get; set; }
    public SimulationView simView { set; get; }

    public bool MouseOnUI { set; get; }

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Debug.Log("press delete");
            var boundaryObjs = mapView.activeLayerView.boundary2Obj.Values;
            List<CellBoundary> boundaries = boundaryObjs.Select(obj => obj.GetComponent<BoundaryController>())
                                                        .Where(bc => bc.selected)
                                                        .Select(bc => bc.Boundary)
                                                        .ToList();

            foreach (var obj in boundaryObjs)
            {
                if (obj.GetComponent<BoundaryController>().selected)
                IndoorSimData.RemoveBoundaries(boundaries);
            }

        }

    }
}
