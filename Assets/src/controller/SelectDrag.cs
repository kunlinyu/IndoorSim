using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;
#nullable enable

enum Status
{
    Idle,
    Selecting,
    Selected,
    Dragging,
}

[RequireComponent(typeof(LineRenderer))]
public class SelectDrag : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { get; set; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { get; set; }
    public Material? draftMaterial { get; set; }
    public bool MouseOnUI { get; set; }

    private Status status = Status.Idle;

    private bool adhoc = false;

    private List<VertexController> selectedVertices = new List<VertexController>();
    private List<BoundaryController> selectedBoundaries = new List<BoundaryController>();
    private List<SpaceController> selectedSpaces = new List<SpaceController>();

    private Vector3? mouseDownPosition = null;

    private Texture2D? selectCursurTexture;
    private Vector2 selectHotspot;

    private Texture2D? dragCursurTexture;
    private Vector2 dragHotspot;

    void Start()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;

        selectCursurTexture = Resources.Load<Texture2D>("cursor/select");
        selectHotspot = new Vector2(selectCursurTexture.width / 2, selectCursurTexture.height / 2.0f);

        UnityEngine.Cursor.SetCursor(selectCursurTexture, selectHotspot, CursorMode.Auto);

        dragCursurTexture = Resources.Load<Texture2D>("cursor/drag");
        dragHotspot = new Vector2(0, 0);
    }

    void SwitchStatus(Status status)
    {
        Debug.Log(status);
        this.status = status;
    }

    void Update()
    {
        Selectable? pointedEntity = MousePickController.PointedEntity;

        switch (status)
        {
            case Status.Idle:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    if (pointedEntity != null && pointedEntity.type != SelectableType.Space)
                    {
                        adhoc = true;
                        if (pointedEntity.type == SelectableType.Vertex)
                            selectedVertices.Add((VertexController)pointedEntity);
                        else
                        {
                            CellBoundary boundary = ((BoundaryController)pointedEntity).Boundary;
                            selectedBoundaries.Add((BoundaryController)pointedEntity);
                            foreach (var entry in mapView.vertex2Obj)
                                if (boundary.Contains(entry.Key))
                                    selectedVertices.Add(entry.Value.GetComponent<VertexController>());
                        }
                        SwitchStatus(Status.Dragging);
                    }
                    else
                    {
                        SwitchStatus(Status.Selecting);
                    }
                    mouseDownPosition = CameraController.mousePositionOnGround();
                }
                else if (Input.GetMouseButtonUp(0))
                    throw new System.Exception("should not release button 0 in Idle status");

                if (pointedEntity != null && pointedEntity.type != SelectableType.Space)
                    UnityEngine.Cursor.SetCursor(dragCursurTexture, dragHotspot, CursorMode.Auto);
                else
                    UnityEngine.Cursor.SetCursor(selectCursurTexture, selectHotspot, CursorMode.Auto);
                break;

            case Status.Selecting:
                if (Input.GetMouseButtonUp(0))
                {
                    // check release immediately?
                    Vector3? currentUpPosition = CameraController.mousePositionOnGround();
                    if (currentUpPosition != null && mouseDownPosition != null)
                        if ((mouseDownPosition - currentUpPosition).Value.magnitude < 0.1f)
                            break;

                    // use bounding box to select vertices
                    List<Coordinate> coors = SquareFromCursor().Select(v => Utils.Vec2Coor(v)).ToList();
                    coors.Add(coors[0]);
                    Polygon selectBox = new GeometryFactory().CreatePolygon(coors.ToArray());
                    foreach (var entry in mapView.vertex2Obj)
                        if (selectBox.Contains(entry.Key.Geom))
                        {
                            var vc = entry.Value.GetComponent<VertexController>();
                            vc.selected = true;
                            if (!selectedVertices.Contains(vc))
                                selectedVertices.Add(vc);
                        }
                    foreach (var entry in mapView.boundary2Obj)
                        if (selectBox.Contains(entry.Key.Geom))
                        {
                            var bc = entry.Value.GetComponent<BoundaryController>();
                            bc.selected = true;
                            if (!selectedBoundaries.Contains(bc))
                                selectedBoundaries.Add(bc);
                        }

                    if (selectedVertices.Count > 0)
                        SwitchStatus(Status.Selected);
                    else
                        SwitchStatus(Status.Idle);
                    GetComponent<LineRenderer>().positionCount = 0;
                }
                else
                {
                    // render bounding box
                    List<Vector3> square = SquareFromCursor();
                    if (square.Count > 0)
                    {
                        LineRenderer lr = GetComponent<LineRenderer>();
                        lr.positionCount = 4;
                        lr.SetPositions(square.ToArray());
                        lr.useWorldSpace = true;
                        lr.loop = true;
                        lr.startWidth = 0.1f;
                        lr.endWidth = 0.1f;
                        lr.material = draftMaterial;
                        lr.sortingOrder = 1;
                    }
                    else
                    {
                        GetComponent<LineRenderer>().positionCount = 0;
                    }
                }
                break;

            case Status.Selected:
                if (Input.GetMouseButtonUp(0))
                    throw new System.Exception("should not release button 0 in Selected status");
                else if (Input.GetMouseButtonDown(0))
                {
                    mouseDownPosition = CameraController.mousePositionOnGround();
                    if (MousePickController.PointedEntity != null && MousePickController.PointedEntity.selected)
                    {
                        SwitchStatus(Status.Dragging);
                    }
                    else  // re select
                    {
                        // no "control" key pressed, clear selected
                        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                        {
                            foreach (var vc in selectedVertices)
                                vc.selected = false;
                            selectedVertices.Clear();
                            foreach (var bc in selectedBoundaries)
                                bc.selected = false;
                            selectedBoundaries.Clear();
                            selectedSpaces.Clear();
                        }

                        SwitchStatus(Status.Selecting);
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    foreach (var vc in selectedVertices)
                        vc.selected = false;
                    selectedVertices.Clear();
                    foreach (var bc in selectedBoundaries)
                        bc.selected = false;
                    selectedBoundaries.Clear();
                    selectedSpaces.Clear();

                    SwitchStatus(Status.Idle);
                }

                if (MousePickController.PointedEntity != null && MousePickController.PointedEntity.selected)
                    UnityEngine.Cursor.SetCursor(dragCursurTexture, dragHotspot, CursorMode.Auto);
                else
                    UnityEngine.Cursor.SetCursor(selectCursurTexture, selectHotspot, CursorMode.Auto);


                break;

            case Status.Dragging:
                if (Input.GetMouseButtonDown(0))
                    throw new System.Exception("should not press button 0 in GetMouseButtonUp status");

                Vector3? currentPosition = CameraController.mousePositionOnGround() ?? mouseDownPosition;

                if (currentPosition != null)
                {
                    Vector3? delta = currentPosition - mouseDownPosition;
                    List<Coordinate> newCoor = new List<Coordinate>();
                    foreach (VertexController vc in selectedVertices)
                    {
                        Vector3? newPosition = Utils.Coor2Vec(vc.Vertex.Coordinate) + delta;
                        if (Input.GetMouseButtonUp(0))
                            newCoor.Add(Utils.Vec2Coor(newPosition!.Value));
                        else
                            vc.updateRenderer(newPosition!.Value);
                    }
                    foreach (BoundaryController bc in selectedBoundaries)
                    {
                        Vector3[] positions = bc.Boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor) + delta.Value).ToArray();
                        bc.updateRenderer(positions);
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        IndoorSim.indoorTiling.UpdateVertices(selectedVertices.Select(vc => vc.Vertex).ToList(), newCoor);
                        if (adhoc)
                        {
                            adhoc = false;
                            selectedVertices.Clear();
                            selectedBoundaries.Clear();
                            SwitchStatus(Status.Idle);
                        }
                        else
                        {
                            SwitchStatus(Status.Selected);
                        }
                    }
                }
                break;
        }

        if (Input.GetMouseButtonDown(1))
        {
            status = Status.Idle;
            foreach (var vc in selectedVertices)
                vc.selected = false;
            foreach (var bc in selectedBoundaries)
                bc.selected = false;
            selectedVertices.Clear();
            selectedBoundaries.Clear();
            selectedSpaces.Clear();
            GetComponent<LineRenderer>().positionCount = 0;
        }

    }

    List<Vector3> SquareFromCursor()
    {
        List<Vector3> result = new List<Vector3>();
        Vector3? currentMousePosition = CameraController.mousePositionOnGround();
        if (mouseDownPosition == null || currentMousePosition == null) return result;

        Vector3 P0 = (Vector3)mouseDownPosition;
        Vector3 P2 = (Vector3)currentMousePosition;

        Vector3? midBottom0 = CameraController.screenPositionOnGround(new Vector3(Screen.width / 2, 0, 0));
        Vector3? midBottom1 = CameraController.screenPositionOnGround(new Vector3(Screen.width / 2, 2, 0));
        Vector3? up_ = midBottom1 - midBottom0;

        if (up_ != null)
        {
            Vector3 L = P0 - P2;
            Vector3 up = (Vector3)(up_.Value.normalized);
            float sin = Vector3.Dot(L.normalized, up);

            Vector3 P1 = (Vector3)P0 - L.magnitude * sin * up;
            Vector3 P3 = (Vector3)P2 + L.magnitude * sin * up;

            result.Add(P0);
            result.Add(P1);
            result.Add(P2);
            result.Add(P3);
        }

        return result;
    }
}
