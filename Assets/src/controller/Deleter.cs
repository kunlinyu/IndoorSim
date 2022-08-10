using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public class Deleter : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    private Texture2D? cursorTexture;
    private Vector2 hotSpot;
    // Start is called before the first frame update
    void Start()
    {
        cursorTexture = Resources.Load<Texture2D>("cursor/delete");
        hotSpot = new Vector2(0.0f, 0.0f);
        UnityEngine.Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        MousePickController.pickType = CurrentPickType.Boundary | CurrentPickType.Agent | CurrentPickType.POI;
    }

    // Update is called once per frame
    void Update()
    {
        if (MousePickController.PointedEntity != null &&
           (MousePickController.PointedEntity.type == SelectableType.Boundary ||
            MousePickController.PointedEntity.type == SelectableType.Agent ||
            MousePickController.PointedEntity.type == SelectableType.POI)
            )
            UnityEngine.Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        else
            UnityEngine.Cursor.SetCursor(null, hotSpot, CursorMode.Auto);

        if (Input.GetMouseButtonUp(0) && MousePickController.PointedEntity != null && MousePickController.PointedEntity.type == SelectableType.Boundary)
            IndoorSimData!.RemoveBoundary(MousePickController.PointedBoundary!.Boundary);

        if (Input.GetMouseButtonUp(0) && MousePickController.PointedEntity != null && MousePickController.PointedEntity.type == SelectableType.Agent)
            IndoorSimData!.RemoveAgent(MousePickController.PointedAgent!.agentDescriptor);

        if (Input.GetMouseButtonUp(0) && MousePickController.PointedEntity != null && MousePickController.PointedEntity.type == SelectableType.POI)
            IndoorSimData!.RemovePOI(MousePickController.PointedPOI!.Poi);
    }
}
