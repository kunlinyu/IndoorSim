using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public class Deleter : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    private Texture2D? cursurTexture;
    private Vector2 hotspot;
    // Start is called before the first frame update
    void Start()
    {
        cursurTexture = Resources.Load<Texture2D>("cursor/delete");
        hotspot = new Vector2(0.0f, 0.0f);
        UnityEngine.Cursor.SetCursor(cursurTexture, hotspot, CursorMode.Auto);
        MousePickController.pickType = CurrentPickType.Boundary;
    }

    // Update is called once per frame
    void Update()
    {
        if (MousePickController.PointedEntity != null && MousePickController.PointedEntity.type == SelectableType.Boundary)
            UnityEngine.Cursor.SetCursor(cursurTexture, hotspot, CursorMode.Auto);
        else
            UnityEngine.Cursor.SetCursor(null, hotspot, CursorMode.Auto);

        if (Input.GetMouseButtonUp(0) && MousePickController.PointedEntity != null && MousePickController.PointedEntity.type == SelectableType.Boundary)
            IndoorSimData!.indoorTiling.RemoveBoundary(((BoundaryController)MousePickController.PointedEntity).Boundary);

    }
}
