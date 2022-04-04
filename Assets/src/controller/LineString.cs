using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

[RequireComponent(typeof(LineRenderer))]
public class LineString : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }
    private Point? lastPoint = null;
    private CellVertex? lastVertex = null;

    private Sprite? cursurSprite;
    private Texture2D? cursurTexture;
    private Vector2 hotspot;

    void Awake()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;

        cursurSprite = Resources.Load<Sprite>("cursor/cursor line");
        cursurTexture = cursurSprite.texture;
        hotspot = new Vector2(cursurTexture.width / 2, cursurTexture.height / 2.0f);
    }
    void Update()
    {
        Selectable? pointedVertex = MousePickController.PointedEntity;
        if (pointedVertex != null && pointedVertex.type == SelectableType.Vertex)
        {
            var vertexScreenPosition3 = Utils.Coor2Screen(((VertexController)pointedVertex).Vertex.Coordinate);
            Vector2 vertexScreenPosition2 = new Vector2(vertexScreenPosition3.x, vertexScreenPosition3.y);

            var mousePosition = Input.mousePosition;
            Vector2 delta = mousePosition - vertexScreenPosition3;
            delta.y = -delta.y;
            delta += hotspot;

            UnityEngine.Cursor.SetCursor(cursurSprite?.texture, delta, CursorMode.ForceSoftware);
        }
        else
        {
            UnityEngine.Cursor.SetCursor(cursurSprite?.texture, hotspot, CursorMode.ForceSoftware);
        }

        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            Coordinate? currentCoor = Utils.Vec2Coor(CameraController.mousePositionOnGround());
            if (currentCoor != null)
            {
                Point currentPoint = new GeometryFactory().CreatePoint(currentCoor);

                Selectable? pointed = MousePickController.PointedEntity;
                CellVertex? currentVertex = null;
                if (pointed != null && pointed.type == SelectableType.Vertex)
                {
                    currentVertex = ((VertexController)pointed).Vertex;
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
        Selectable? pointedVertex = MousePickController.PointedEntity;
        if (mousePosition != null)
        {
            if (pointedVertex != null && pointedVertex.type == SelectableType.Vertex)
                mousePosition = ((VertexController)pointedVertex).Vertex.Coordinate;

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
