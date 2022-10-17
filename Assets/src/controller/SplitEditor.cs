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

    private List<GameObject> ACLines = new List<GameObject>();
    private List<GameObject> BDLines = new List<GameObject>();

    void Start()
    {

    }


    // 0 --- 3
    // |     |
    // |     |
    // 1 --- 2
    void Update()
    {
        SpaceController? sc = MousePickController.PointedSpace;
        if (sc == null) return;
        if (!ContainerFilter(sc.Space)) return;
        Polygon polygon = sc.Space.Polygon;

        Coordinate? currentCoor = U.Vec2Coor(CameraController.mousePositionOnGround());
        if (currentCoor == null) return;
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

        ACNum--;
        BDNum--;

        Debug.Log(ACNum + "\t" + BDNum + "\t" + ratioAC + "\t" + ratioBD);

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
            lr.SetPosition(0, polygonVec[1] + V12 * ratio);
            lr.SetPosition(1, polygonVec[0] + V03 * ratio);
        }

        for (int i = 0; i < BDLines.Count; i++)
        {
            GameObject obj = BDLines[i];
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            float ratio = (float)(i + 1) / (BDNum + 1);
            lr.SetPosition(0, polygonVec[0] + V01 * ratio);
            lr.SetPosition(1, polygonVec[3] + V32 * ratio);
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
