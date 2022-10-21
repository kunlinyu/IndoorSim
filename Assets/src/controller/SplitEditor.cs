using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

#nullable enable

public class SplitEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    public int rows = 0;
    public int coloums = 0;

    private List<GameObject> ACLines = new List<GameObject>();
    private List<GameObject> BDLines = new List<GameObject>();

    private void Clear()
    {
        ACLines.ForEach(obj => Destroy(obj));
        BDLines.ForEach(obj => Destroy(obj));
        ACLines.Clear();
        BDLines.Clear();
    }

    void Start()
    {

    }


    // 0 ---D--- 3
    // |    |    |
    // A ------- C
    // |    |    |
    // 1 ---B--- 2
    void Update()
    {
        SpaceController? sc = MousePickController.PointedSpace;
        if (sc == null || !ContainerFilter(sc.Space))
        {
            Clear();
            return;
        }
        Polygon polygon = sc.Space.Polygon;

        Coordinate? currentCoor = U.Vec2Coor(CameraController.mousePositionOnGround());
        if (currentCoor == null)
        {
            Clear();
            return;
        }

        Point currentPoint = new GeometryFactory().CreatePoint(currentCoor);

        Coordinate[] coors = polygon.Coordinates;
        Vector3[] polygonVec = new Vector3[] {
            U.Coor2Vec(coors[0]),
            U.Coor2Vec(coors[1]),
            U.Coor2Vec(coors[2]),
            U.Coor2Vec(coors[3])
        };

        Coordinate A = new LineSegment(coors[0], coors[1]).MidPoint;
        Coordinate B = new LineSegment(coors[1], coors[2]).MidPoint;
        Coordinate C = new LineSegment(coors[2], coors[3]).MidPoint;
        Coordinate D = new LineSegment(coors[3], coors[4]).MidPoint;

        LineString lsAC = new LineString(new Coordinate[] { A, C });
        LineString lsBD = new LineString(new Coordinate[] { B, D });

        Coordinate nearAC = DistanceOp.NearestPoints(lsAC, currentPoint)[0];
        Coordinate nearBD = DistanceOp.NearestPoints(lsBD, currentPoint)[0];

        float ratioAC = (float)((A.Distance(nearAC)) / lsAC.Length);
        float ratioBD = (float)((B.Distance(nearBD)) / lsBD.Length);

        if (ratioAC > 0.5f) ratioAC = 1.0f - ratioAC;
        if (ratioBD > 0.5f) ratioBD = 1.0f - ratioBD;

        int ACNum = Mathf.FloorToInt(1.0f / ratioAC);
        int BDNum = Mathf.FloorToInt(1.0f / ratioBD);

        if (ACNum < 0 || ACNum > 10) ACNum = 10;
        if (BDNum < 0 || BDNum > 10) BDNum = 10;


        if (rows != 0 && coloums != 0)
        {
            if (lsAC.Length < lsBD.Length)
            {
                ACNum = rows;
                BDNum = coloums;
            }
            else
            {
                BDNum = rows;
                ACNum = coloums;
            }
        }

        ACNum--;
        BDNum--;

        GameObject template = transform.Find("draft").gameObject;
        while (ACLines.Count > ACNum)
        {
            Destroy(ACLines[ACLines.Count - 1]);
            ACLines.RemoveAt(ACLines.Count - 1);
        }
        while (ACLines.Count < ACNum)
        {
            GameObject newDraft = Instantiate(template, transform);
            newDraft.name = "AC draft " + ACLines.Count;
            ACLines.Add(newDraft);
        }
        while (BDLines.Count > BDNum)
        {
            Destroy(BDLines[BDLines.Count - 1]);
            BDLines.RemoveAt(BDLines.Count - 1);
        }
        while (BDLines.Count < BDNum)
        {
            GameObject newDraft = Instantiate(template, transform);
            newDraft.name = "BD draft " + BDLines.Count;
            BDLines.Add(newDraft);
        }

        Vector3 V12 = polygonVec[2] - polygonVec[1];
        Vector3 V03 = polygonVec[3] - polygonVec[0];
        Vector3 V01 = polygonVec[1] - polygonVec[0];
        Vector3 V32 = polygonVec[2] - polygonVec[3];

        for (int i = 0; i < ACLines.Count; i++)
        {
            GameObject obj = ACLines[i];
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            float ratio = (float)(i + 1) / (ACNum + 1);
            lr.positionCount = 2;
            lr.SetPosition(0, polygonVec[1] + V12 * ratio);
            lr.SetPosition(1, polygonVec[0] + V03 * ratio);
        }

        for (int i = 0; i < BDLines.Count; i++)
        {
            GameObject obj = BDLines[i];
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            float ratio = (float)(i + 1) / (BDNum + 1);
            lr.positionCount = 2;
            lr.SetPosition(0, polygonVec[0] + V01 * ratio);
            lr.SetPosition(1, polygonVec[3] + V32 * ratio);
        }

        if (Input.GetMouseButtonDown(0))
        {
            IndoorSimData!.activeTiling.DisableResultValidate();
            IndoorSimData!.SessionStart();
            List<CellBoundary> newBoundaries = new List<CellBoundary>();

            foreach (var obj in ACLines)
            {
                LineRenderer lr = obj.GetComponent<LineRenderer>();
                CellVertex V1 = IndoorSimData!.SplitBoundary(U.Vec2Coor(lr.GetPosition(0)));
                CellVertex V2 = IndoorSimData!.SplitBoundary(U.Vec2Coor(lr.GetPosition(1)));
                CellBoundary? newboundary = IndoorSimData!.AddBoundary(V1, V2);
                if (newboundary == null) throw new System.Exception("Oops");
                newBoundaries.Add(newboundary);
            }

            GeometryFactory factory = new GeometryFactory();
            List<CellBoundary> switchBoundaries = new List<CellBoundary>();
            foreach (var obj in BDLines)
            {
                LineRenderer lr = obj.GetComponent<LineRenderer>();
                CellVertex VStart = IndoorSimData!.SplitBoundary(U.Vec2Coor(lr.GetPosition(0)));
                CellVertex VEnd = IndoorSimData!.SplitBoundary(U.Vec2Coor(lr.GetPosition(1)));
                LineSegment lineSeg = new LineSegment(VStart.Coordinate, VEnd.Coordinate);

                List<CellVertex> splitVertices = new List<CellVertex>();
                foreach (var b in newBoundaries)
                {
                    LineSegment bSeg = new LineSegment(b.P0.Coordinate, b.P1.Coordinate);
                    Point intersectionPoint = new Point(bSeg.Intersection(lineSeg));
                    var lingeSeg1 = new LineSegment(new Coordinate(0, 0), new Coordinate(1, 1));
                    var lingeSeg2 = new LineSegment(new Coordinate(0, 0), new Coordinate(1, 1));
                    CellVertex vertex = IndoorSimData!.SplitBoundary(b, intersectionPoint.Coordinate);
                    var bs = new List<CellBoundary>(IndoorSimData!.activeTiling.layer.Vertex2Boundaries(vertex));
                    CellBoundary longerOne = bs[0].geom.Length > bs[1].geom.Length ? bs[0] : bs[1];
                    switchBoundaries.Add(longerOne);
                    splitVertices.Add(vertex);
                }
                newBoundaries = new List<CellBoundary>(switchBoundaries);
                switchBoundaries.Clear();

                if (splitVertices.Count > 0)
                {
                    IndoorSimData!.AddBoundary(VStart, splitVertices[0]);
                    for (int i = 0; i < splitVertices.Count - 1; i++)
                        IndoorSimData!.AddBoundary(splitVertices[i], splitVertices[i + 1]);
                    IndoorSimData!.AddBoundary(splitVertices[splitVertices.Count - 1], VEnd);
                }
                else
                {
                    IndoorSimData!.AddBoundary(VStart, VEnd);
                }
            }

            IndoorSimData!.SessionCommit();
            IndoorSimData!.activeTiling.EnableResultValidateAndDoOnce();
        }
    }

    static bool ContainerFilter(Container? container)
    {
        if (container == null) return false;
        if (container.navigable == Navigable.Navigable) return false;
        if (container.Geom!.NumPoints != 5) return false;
        return true;
    }
}
