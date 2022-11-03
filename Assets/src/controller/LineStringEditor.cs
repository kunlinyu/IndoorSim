using System.Collections.Generic;
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

                CellVertex? currentVertex = null;
                CellBoundary? currentBoundary = null;

                // handle split boundary
                if (pointed != null && pointed.type == SelectableType.Boundary)
                {
                    currentBoundary = ((BoundaryController)pointed).Boundary;
                    Coordinate[] nearestCoor = DistanceOp.NearestPoints(currentBoundary.geom, new GeometryFactory().CreatePoint(currentCoor));
                    currentCoor = nearestCoor[0];
                    currentVertex = IndoorSimData!.SplitBoundary(currentBoundary!, currentCoor);
                }

                // snap to vertex
                if (pointed != null && pointed.type == SelectableType.Vertex)
                {
                    currentVertex = ((VertexController)pointed).Vertex;
                    currentCoor = currentVertex.Coordinate;
                }

                if (lastCoor != null)
                {
                    GeometryFactory gf = new GeometryFactory();
                    CellBoundary? boundary = null;

                    Coordinate tempLastCoor = lastVertex != null ? lastVertex.Coordinate : lastCoor;
                    Coordinate tempCurrentCoor = currentVertex != null ? currentVertex.Coordinate : currentCoor;
                    LineString tempLs = new GeometryFactory().CreateLineString(new Coordinate[] { tempLastCoor, tempCurrentCoor });
                    bool lessthan3 = IndoorSimData!.IntersectionLessThan(tempLs, 3, out List<CellBoundary> crossesBoundaries, out List<Coordinate> intersections);
                    if (lessthan3 && crossesBoundaries.Count == 0)
                    {
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
                        else
                        {
                            lastVertex = currentVertex;
                            lastCoor = lastVertex!.Coordinate;
                        }
                    }
                    else if (lessthan3 && crossesBoundaries.Count > 0)
                    {
                        IndoorSimData!.SessionStart();
                        List<CellVertex> newVertices = new List<CellVertex>();
                        for (int i = 0; i < crossesBoundaries.Count; i++)
                            newVertices.Add(IndoorSimData!.SplitBoundary(crossesBoundaries[i], intersections[i]));
                        for (int i = 0; i < newVertices.Count - 1; i++)
                            IndoorSimData!.AddBoundary(newVertices[i], newVertices[i + 1]);

                        CellBoundary? firstB = IndoorSimData!.AddBoundaryAutoSnap(tempLastCoor, newVertices[0].Coordinate);
                        CellBoundary? lastB = IndoorSimData!.AddBoundaryAutoSnap(newVertices[newVertices.Count - 1].Coordinate, tempCurrentCoor);

                        IndoorSimData!.SessionCommit();

                        lastVertex = lastB!.P1;
                        lastCoor = lastB!.P1.Coordinate;
                    }
                    else
                    {
                        if (lastVertex != null && currentVertex != null)
                        {
                            lastVertex = currentVertex;
                            lastCoor = lastVertex.Coordinate;
                        }
                    }
                }
                else
                {
                    lastCoor = currentCoor;
                    lastVertex = currentVertex;
                }
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
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
