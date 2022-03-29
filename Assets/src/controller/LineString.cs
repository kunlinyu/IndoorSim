using System;
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

    void Awake()
    {
        lastPoint = null;
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
                if (lastPoint != null)
                {
                    GeometryFactory gf = new GeometryFactory();
                    var ls = gf.CreateLineString(new Coordinate[] { lastPoint.Coordinate, currentPoint.Coordinate });
                    IndoorSim.indoorTiling.AddBoundary(ls);
                }
                lastPoint = currentPoint;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastPoint = null;
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
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.numCapVertices = 3;

            lr.sortingLayerID = sortingLayerId;
            lr.sortingOrder = 0;

            lr.material = draftMaterial;
        }
    }
}
