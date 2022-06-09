using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class IDEditor : MonoBehaviour, ITool
{

    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }


    public Action<int, int>? PopContainerIdPanel;
    public Action? HideContainerIdPanel;
    private string currentSpaceId = "";

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            if (MousePickController.PointedSpace != null)
            {
                currentSpaceId = MousePickController.PointedSpace.Space.Id;
                PopContainerIdPanel?.Invoke((int)Input.mousePosition.x, (int)Input.mousePosition.y);
            }

        if (Input.GetMouseButtonDown(1))
            HideContainerIdPanel?.Invoke();
    }

    void SetContainerId(string containerId, List<string> sonsId)
    {
        CellSpace? space = IndoorSimData?.indoorData.FindSpaceId(currentSpaceId);
        if (space == null) throw new Exception("can not find cellspace with id : " + currentSpaceId);
        space.containerId = containerId;
        space.children.Clear();
        sonsId.ForEach(sonId => space.children.Add(new Container(sonId)));
    }
}
