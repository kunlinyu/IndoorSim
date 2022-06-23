using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;
#nullable enable

enum SelectStatus
{
    Idle,
    Selecting,
    Selected,
    Dragging,
}

[RequireComponent(typeof(LineRenderer))]
public class SelectDrag : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { get; set; }
    public MapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public int sortingLayerId { get; set; }
    public Material? draftMaterial { get; set; }
    public bool MouseOnUI { get; set; }

    public Camera? screenshotCamera;

    private SelectStatus status = SelectStatus.Idle;

    private bool adhoc = false;

    private List<VertexController> selectedVertices = new List<VertexController>();
    private List<BoundaryController> selectedBoundaries = new List<BoundaryController>();
    private List<SpaceController> selectedSpaces = new List<SpaceController>();
    private List<AgentController> selectedAgents = new List<AgentController>();

    private Vector3? mouseDownPosition = null;

    private Texture2D? selectCursorTexture;
    private Vector2 selectHotSpot;

    private Texture2D? dragCursorTexture;
    private Vector2 dragHotSpot;

    void Start()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;

        selectCursorTexture = Resources.Load<Texture2D>("cursor/select");
        selectHotSpot = new Vector2(selectCursorTexture.width / 2, selectCursorTexture.height / 2.0f);

        UnityEngine.Cursor.SetCursor(selectCursorTexture, selectHotSpot, CursorMode.Auto);

        dragCursorTexture = Resources.Load<Texture2D>("cursor/drag");
        dragHotSpot = new Vector2(0, 0);
    }

    void SwitchStatus(SelectStatus status)
    {
        Debug.Log(status);
        this.status = status;
    }

    void Update()
    {
        if (mapView == null) throw new InvalidOperationException("mapView null");
        if (simView == null) throw new InvalidOperationException("simView null");
        if (IndoorSimData == null) throw new InvalidOperationException("IndoorSim null");
        Selectable? pointedEntity = MousePickController.PointedEntity;

        switch (status)
        {
            case SelectStatus.Idle:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    if (pointedEntity != null)
                    {
                        adhoc = true;
                        if (pointedEntity.type == SelectableType.Vertex)
                            selectedVertices.Add((VertexController)pointedEntity);
                        else if (pointedEntity.type == SelectableType.Boundary)
                        {
                            CellBoundary boundary = ((BoundaryController)pointedEntity).Boundary;
                            selectedBoundaries.Add((BoundaryController)pointedEntity);
                            foreach (var entry in mapView.vertex2Obj)
                                if (boundary.Contains(entry.Key))
                                    selectedVertices.Add(entry.Value.GetComponent<VertexController>());
                        }
                        else if (pointedEntity.type == SelectableType.Space)
                        {
                            CellSpace space = ((SpaceController)pointedEntity).Space;
                            selectedSpaces.Add((SpaceController)pointedEntity);
                            foreach (var entry in mapView.vertex2Obj)
                                if (space.allVertices.Contains(entry.Key))
                                    selectedVertices.Add(entry.Value.GetComponent<VertexController>());
                            foreach (var entry in mapView.boundary2Obj)
                                if (space.allBoundaries.Contains(entry.Key))
                                    selectedBoundaries.Add(entry.Value.GetComponent<BoundaryController>());
                        }
                        else if (pointedEntity.type == SelectableType.Agent)
                        {
                            selectedAgents.Add((AgentController)pointedEntity);
                        }
                        else
                        {
                            Debug.LogWarning("unknown selectable type");
                        }
                        SwitchStatus(SelectStatus.Dragging);
                    }
                    else
                    {
                        SwitchStatus(SelectStatus.Selecting);
                    }
                    mouseDownPosition = CameraController.mousePositionOnGround();
                }
                else if (Input.GetMouseButtonUp(0) && !MouseOnUI)
                    throw new System.Exception("should not release button 0 in Idle status");

                if (pointedEntity != null)
                    UnityEngine.Cursor.SetCursor(dragCursorTexture, dragHotSpot, CursorMode.Auto);
                else
                    UnityEngine.Cursor.SetCursor(selectCursorTexture, selectHotSpot, CursorMode.Auto);
                break;

            case SelectStatus.Selecting:
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
                        if (selectBox.Contains(entry.Key.geom))
                        {
                            var bc = entry.Value.GetComponent<BoundaryController>();
                            bc.selected = true;
                            if (!selectedBoundaries.Contains(bc))
                                selectedBoundaries.Add(bc);
                        }
                    foreach (var entry in mapView.cellspace2Obj)
                        if (selectBox.Contains(entry.Key.Geom))
                        {
                            var sc = entry.Value.GetComponent<SpaceController>();
                            sc.selected = true;
                            if (!selectedSpaces.Contains(sc))
                                selectedSpaces.Add(sc);
                        }
                    foreach (var entry in simView.agent2Obj)
                        if (selectBox.Contains(new Point(entry.Key.x, entry.Key.y)))
                        {
                            var ac = entry.Value.GetComponent<AgentController>();
                            ac.selected = true;
                            if (!selectedAgents.Contains(ac))
                                selectedAgents.Add(ac);
                        }


                    if (selectedVertices.Count > 0 || selectedAgents.Count > 0)
                        SwitchStatus(SelectStatus.Selected);
                    else
                        SwitchStatus(SelectStatus.Idle);
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

            case SelectStatus.Selected:
                if (Input.GetMouseButtonUp(0) && !MouseOnUI)
                    throw new System.Exception("should not release button 0 in Selected status");
                else if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    mouseDownPosition = CameraController.mousePositionOnGround();
                    if (MousePickController.PointedEntity != null && MousePickController.PointedEntity.selected)
                    {
                        SwitchStatus(SelectStatus.Dragging);
                    }
                    else  // re select
                    {
                        // no CTRL key pressed, clear selected
                        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                        {
                            selectedVertices.ForEach(vc => vc.selected = false);
                            selectedVertices.Clear();
                            selectedBoundaries.ForEach(bc => bc.selected = false);
                            selectedBoundaries.Clear();
                            selectedSpaces.ForEach(sc => sc.selected = false);
                            selectedSpaces.Clear();
                            selectedAgents.ForEach(ac => ac.selected = false);
                            selectedAgents.Clear();
                        }

                        // if CTRL key pressed, add new selected entities
                        SwitchStatus(SelectStatus.Selecting);
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    selectedVertices.ForEach(vc => vc.selected = false);
                    selectedVertices.Clear();
                    selectedBoundaries.ForEach(bc => bc.selected = false);
                    selectedBoundaries.Clear();
                    selectedSpaces.ForEach(sc => sc.selected = false);
                    selectedSpaces.Clear();
                    selectedAgents.ForEach(ac => ac.selected = false);
                    selectedAgents.Clear();

                    SwitchStatus(SelectStatus.Idle);
                }

                if (MousePickController.PointedEntity != null && MousePickController.PointedEntity.selected)
                    UnityEngine.Cursor.SetCursor(dragCursorTexture, dragHotSpot, CursorMode.Auto);
                else
                    UnityEngine.Cursor.SetCursor(selectCursorTexture, selectHotSpot, CursorMode.Auto);


                break;

            case SelectStatus.Dragging:
                if (Input.GetMouseButtonDown(0))
                    throw new System.Exception("should not press button 0 in GetMouseButtonUp status");

                Vector3? currentPosition = CameraController.mousePositionOnGround() ?? mouseDownPosition;

                if (currentPosition != null)
                {
                    Vector3 delta = (currentPosition - mouseDownPosition)!.Value;
                    List<Coordinate> newVertexCoor = new List<Coordinate>();
                    List<Coordinate> newAgentCoor = new List<Coordinate>();
                    foreach (VertexController vc in selectedVertices)
                    {
                        Vector3? newPosition = Utils.Coor2Vec(vc.Vertex.Coordinate) + delta;
                        if (Input.GetMouseButtonUp(0))
                            newVertexCoor.Add(Utils.Vec2Coor(newPosition!.Value));
                        else
                            vc.updateRenderer(newPosition!.Value);
                    }
                    foreach (BoundaryController bc in selectedBoundaries)
                    {
                        Vector3[] positions = bc.Boundary.geom.Coordinates.Select(coor => Utils.Coor2Vec(coor) + delta).ToArray();
                        bc.updateRenderer(positions);
                    }
                    foreach (SpaceController sc in selectedSpaces)
                    {
                        sc.updateRenderer(delta);
                    }
                    foreach (AgentController ac in selectedAgents)
                    {
                        Vector3 position = new Vector3(ac.AgentDescriptor.x, 0.0f, ac.AgentDescriptor.y);
                        ac.transform.position = position + delta;
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (selectedVertices.Count > 0)
                            IndoorSimData.UpdateVertices(selectedVertices.Select(vc => vc.Vertex).ToList(), newVertexCoor);

                        if (selectedAgents.Count > 0)
                        {
                            var oldAgentDescriptors = selectedAgents.Select(ac => ac.AgentDescriptor).ToList();
                            var newAgentDescriptors = oldAgentDescriptors.Select(ad =>
                            {
                                var newAgentDesc = ad.Clone();
                                newAgentDesc.x += delta.x;
                                newAgentDesc.y += delta.z;
                                return newAgentDesc;
                            }).ToList();
                            IndoorSimData.UpdateAgents(oldAgentDescriptors, newAgentDescriptors);
                        }

                        if (adhoc)
                        {
                            adhoc = false;
                            selectedVertices.Clear();
                            selectedBoundaries.Clear();
                            selectedSpaces.Clear();
                            selectedAgents.Clear();
                            SwitchStatus(SelectStatus.Idle);
                        }
                        else
                        {
                            SwitchStatus(SelectStatus.Selected);
                        }
                    }
                }
                break;
        }

        if (Input.GetMouseButtonDown(1))
        {
            status = SelectStatus.Idle;
            selectedVertices.ForEach(vc => vc.selected = false);
            selectedVertices.Clear();
            selectedBoundaries.ForEach(bc => bc.selected = false);
            selectedBoundaries.Clear();
            selectedSpaces.ForEach(sc => sc.selected = false);
            selectedSpaces.Clear();
            selectedAgents.ForEach(ac => ac.selected = false);
            selectedAgents.Clear();
            GetComponent<LineRenderer>().positionCount = 0;
        }

    }

    void OnDestroy()
    {
        selectedVertices.ForEach(vc => vc.selected = false);
        selectedVertices.Clear();
        selectedBoundaries.ForEach(bc => bc.selected = false);
        selectedBoundaries.Clear();
        selectedSpaces.ForEach(sc => sc.selected = false);
        selectedSpaces.Clear();
        selectedAgents.ForEach(ac => ac.selected = false);
        selectedAgents.Clear();
    }

    private static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
    private string capture(float maxX, float minX, float maxY, float minY)
    {
        if (mapView == null)
            return "";
        if (screenshotCamera == null)
            return "";

        foreach (var entry in mapView.vertex2Obj)
            if (!entry.Value.GetComponent<VertexController>().selected)
                entry.Value.SetActive(false);
        foreach (var entry in mapView.boundary2Obj)
            if (!entry.Value.GetComponent<BoundaryController>().selected)
                entry.Value.SetActive(false);
        foreach (var entry in mapView.cellspace2Obj)
            if (!entry.Value.GetComponent<SpaceController>().selected)
                entry.Value.SetActive(false);

        int resWidth = 128;
        int resHeight = 128;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

        screenshotCamera.orthographicSize = Mathf.Max(maxX - minX, maxY - minY);
        Vector3 position = screenshotCamera.transform.position;
        position.x = (maxX + minX) / 2.0f;
        position.z = (maxY + minY) / 2.0f;
        screenshotCamera.transform.position = position;

        screenshotCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        screenshotCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(resWidth, resHeight);
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", filename));


        foreach (var entry in mapView.vertex2Obj)
            entry.Value.SetActive(true);
        foreach (var entry in mapView.boundary2Obj)
            entry.Value.SetActive(true);
        foreach (var entry in mapView.cellspace2Obj)
            entry.Value.SetActive(true);
        return Convert.ToBase64String(bytes);
    }

    public void ExtractSelected2Asset()
    {
        if (selectedVertices.Count > 0 && selectedBoundaries.Count > 0)
            IndoorSimData?.ExtractAsset("untitled asdf",
                selectedVertices.Select(vc => vc.Vertex).ToList(),
                selectedBoundaries.Select(bc => bc.Boundary).ToList(),
                selectedSpaces.Select(sc => sc.Space).ToList(),
                capture);
        else
            Debug.LogWarning("nothing can be save as asset");
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
