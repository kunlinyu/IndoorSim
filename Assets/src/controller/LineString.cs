using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using UnityEngine;
#nullable enable

[RequireComponent(typeof(LineRenderer))]
public class LineString : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }
    private Coordinate? lastCoor = null;
    private CellVertex? lastVertex = null;

    private Texture2D? cursurTexture;
    private Vector2 hotspot;

    void Awake()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;

        cursurTexture = Resources.Load<Texture2D>("cursor/pen");
        hotspot = new Vector2(0.0f, 0.0f);
        UnityEngine.Cursor.SetCursor(cursurTexture, hotspot, CursorMode.Auto);
    }
    void Update()
    {
        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            Coordinate? currentCoor_ = Utils.Vec2Coor(CameraController.mousePositionOnGround());
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

                    Coordinate[] nearestCoor = DistanceOp.NearestPoints(currentBoundary.Geom, new GeometryFactory().CreatePoint(currentCoor));
                    currentCoor = nearestCoor[0];
                }

                if (lastCoor != null)
                {
                    GeometryFactory gf = new GeometryFactory();
                    CellBoundary? boundary = null;

                    if (splitBoundary)
                        currentVertex = IndoorSim!.indoorTiling.SplitBoundary(currentBoundary!, currentCoor);

                    if (lastVertex == null && currentVertex == null) boundary = IndoorSim!.indoorTiling.AddBoundary(lastCoor, currentCoor);
                    else if (lastVertex != null && currentVertex == null) boundary = IndoorSim!.indoorTiling.AddBoundary(lastVertex, currentCoor);
                    else if (lastVertex == null && currentVertex != null) boundary = IndoorSim!.indoorTiling.AddBoundary(lastCoor, currentVertex);
                    else if (lastVertex != null && currentVertex != null)
                        if (lastVertex != currentVertex)
                            boundary = IndoorSim!.indoorTiling.AddBoundary(lastVertex, currentVertex);

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

        Coordinate? mousePosition = Utils.Vec2Coor(CameraController.mousePositionOnGround());
        Selectable? pointedVertex = MousePickController.PointedEntity;
        if (mousePosition != null)
        {
            if (pointedVertex != null && pointedVertex.type == SelectableType.Vertex)
                mousePosition = ((VertexController)pointedVertex).Vertex.Coordinate;

            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, Utils.Coor2Vec(mousePosition));
            lr.SetPosition(1, Utils.Coor2Vec(lastCoor));
            lr.alignment = LineAlignment.TransformZ;    // border should face to sky
            lr.useWorldSpace = true;

            lr.loop = false;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.numCapVertices = 3;

            lr.sortingLayerID = sortingLayerId;
            lr.sortingOrder = 10;

            lr.material = draftMaterial;
        }
    }
}
