using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

[RequireComponent(typeof(LineRenderer))]
public class LineString : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    private Point? lastPoint = null;
    private CellVertex? lastVertex = null;

    void Awake()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
    }
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Coordinate? currentCoor = Utils.Vec2Coor(CameraController.mousePositionOnGround());
            if (currentCoor != null)
            {
                Point currentPoint = new GeometryFactory().CreatePoint(currentCoor);

                Selectable? selectable = MousePickController.SelectedEntity;
                CellVertex? currentVertex = null;
                if (selectable != null && selectable.type == SelectableType.Vertex)
                {
                    currentVertex = ((VertexController)selectable).Vertex;
                    currentPoint = currentVertex.Geom;
                }

                if (lastPoint != null)
                {
                    GeometryFactory gf = new GeometryFactory();

                    if (lastVertex == null && currentVertex == null)
                    {
                        var ls = gf.CreateLineString(new Coordinate[] { lastPoint.Coordinate, currentPoint.Coordinate });
                        CellVertex newVertexStart = new CellVertex(lastPoint);
                        CellVertex newVertexEnd = new CellVertex(currentPoint);
                        IndoorSim!.indoorTiling.AddBoundary(ls, newVertexStart, newVertexEnd);
                        lastVertex = newVertexEnd;
                        lastPoint = currentPoint;
                    }
                    else if (lastVertex != null && currentVertex == null)
                    {
                        var ls = gf.CreateLineString(new Coordinate[] { lastVertex.Coordinate, currentPoint.Coordinate });
                        CellVertex newVertex = new CellVertex(currentPoint);
                        IndoorSim!.indoorTiling.AddBoundary(ls, lastVertex, newVertex);
                        lastVertex = newVertex;
                        lastPoint = currentPoint;
                    }
                    else if (lastVertex == null && currentVertex != null)
                    {
                        var ls = gf.CreateLineString(new Coordinate[] { lastPoint.Coordinate, currentVertex.Coordinate });
                        CellVertex newVertex = new CellVertex(lastPoint);
                        IndoorSim!.indoorTiling.AddBoundary(ls, newVertex, currentVertex);
                        lastVertex = currentVertex;
                        lastPoint = currentVertex.Geom;
                    }
                    else if (lastVertex != null && currentVertex != null)
                    {
                        if (lastVertex != currentVertex)
                        {
                            var ls = gf.CreateLineString(new Coordinate[] { lastVertex.Coordinate, currentVertex.Coordinate });
                            IndoorSim!.indoorTiling.AddBoundary(ls, lastVertex, currentVertex);
                            lastVertex = currentVertex;
                            lastPoint = currentVertex.Geom;
                        }
                    }
                    else
                        throw new System.Exception("Oops!");
                }
                else
                {
                    lastPoint = currentPoint;
                    lastVertex = currentVertex;
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastPoint = null;
            lastVertex = null;
        }

        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        if (lastPoint == null)
        {
            GetComponent<LineRenderer>().positionCount = 0;
            return;
        }

        Coordinate? mousePosition = Utils.Vec2Coor(CameraController.mousePositionOnGround());
        if (mousePosition != null)
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, Utils.Coor2Vec(mousePosition));
            lr.SetPosition(1, Utils.Coor2Vec(lastPoint.Coordinate));
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
