using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;
#nullable enable

[RequireComponent(typeof(LineRenderer))]
public class LineStringEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }
    private Coordinate? lastCoor = null;
    private CellVertex? lastVertex = null;

    private Texture2D? cursorTexture;
    private Vector2 hotSpot;

    void Awake()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;

        cursorTexture = Resources.Load<Texture2D>("cursor/pen");
        hotSpot = new Vector2(0.0f, 0.0f);
        UnityEngine.Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }
    void Update()
    {
        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            Coordinate? currentCoor_ = U.Vec2Coor(CameraController.mousePositionOnGround());
            if (currentCoor_ != null)
            {
                Coordinate currentCoor = currentCoor_;
                Selectable? pointed = MousePickController.PointedEntity;

                if (lastCoor == null && pointed != null && pointed.type == SelectableType.Boundary) return;

                // snap to vertex
                CellVertex? currentVertex = null;
                CellBoundary? currentBoundary = null;
                if (pointed != null && pointed.type == SelectableType.Vertex)
                {
                    currentVertex = ((VertexController)pointed).Vertex;
                    currentCoor = currentVertex.Coordinate;
                }

                // handle split boundary
                bool splitBoundary = false;
                if (lastCoor != null && pointed != null && pointed.type == SelectableType.Boundary)
                {
                    splitBoundary = true;
                    currentBoundary = ((BoundaryController)pointed).Boundary;

                    Coordinate[] nearestCoor = DistanceOp.NearestPoints(currentBoundary.geom, new GeometryFactory().CreatePoint(currentCoor));
                    currentCoor = nearestCoor[0];
                }

                if (lastCoor != null)
                {
                    GeometryFactory gf = new GeometryFactory();
                    CellBoundary? boundary = null;

                    if (splitBoundary)
                        currentVertex = IndoorSimData!.SplitBoundary(currentBoundary!, currentCoor);

                    if (lastVertex == null && currentVertex == null) boundary = IndoorSimData!.AddBoundary(lastCoor, currentCoor);
                    else if (lastVertex != null && currentVertex == null) boundary = IndoorSimData!.AddBoundary(lastVertex, currentCoor);
                    else if (lastVertex == null && currentVertex != null) boundary = IndoorSimData!.AddBoundary(lastCoor, currentVertex);
                    else if (lastVertex != null && currentVertex != null)
                        if (lastVertex != currentVertex)
                            boundary = IndoorSimData!.AddBoundary(lastVertex, currentVertex);

                    if (boundary != null)
                    {
                        lastVertex = boundary.P1;
                        lastCoor = boundary.P1.Coordinate;
                    }
                    else if (lastVertex != null && currentVertex != null)
                    {
                        lastVertex = currentVertex;
                        lastCoor = lastVertex.Coordinate;
                    }
                }
                else
                {
                    lastCoor = currentCoor;
                    lastVertex = currentVertex;
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastCoor = null;
            lastVertex = null;
        }



        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        if (lastCoor == null)
        {
            GetComponent<LineRenderer>().positionCount = 0;
            return;
        }

        Coordinate? mousePosition = U.Vec2Coor(CameraController.mousePositionOnGround());
        Selectable? pointedVertex = MousePickController.PointedEntity;
        if (mousePosition != null)
        {
            if (pointedVertex != null && pointedVertex.type == SelectableType.Vertex)
                mousePosition = ((VertexController)pointedVertex).Vertex.Coordinate;

            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, U.Coor2Vec(mousePosition));
            lr.SetPosition(1, U.Coor2Vec(lastCoor));
        }
    }
}
