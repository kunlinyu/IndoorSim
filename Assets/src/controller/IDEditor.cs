using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class IDEditor : MonoBehaviour, ITool
{

    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }


    public Action<int, int, string, string>? PopContainerIdPanel;
    public Action? HideContainerIdPanel;

    private CellSpace? currentSpace;

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
            if (MousePickController.PointedSpace != null)
            {
                currentSpace = MousePickController.PointedSpace.Space;
                string childrenIds = string.Join(',', currentSpace.children.Select(child => child.containerId));
                PopContainerIdPanel?.Invoke((int)Input.mousePosition.x, (int)Input.mousePosition.y, currentSpace.containerId, childrenIds);
            }

        if (Input.GetMouseButtonDown(1))
        {
            HideContainerIdPanel?.Invoke();
            currentSpace = null;
        }
    }
    public void SetContainerId(string containerId, string childrenIdStr)
    {
        List<string> childrenId = new List<string>(childrenIdStr.Split(',', ' ', '\t', '\n'));
        childrenId.RemoveAll(childId => childId.Length == 0);

        if (currentSpace == null) throw new Exception("never select one space");

        IndoorSimData?.UpdateSpaceId(currentSpace, containerId, childrenId);

        Debug.Log("container id set");
    }
}