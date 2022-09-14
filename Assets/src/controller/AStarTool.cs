using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

enum AStarToolStatus
{
    Nothing,
    SourceClicked,
    TargetClicked,
}

public class AStarTool : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    private Vector3? sourcePoint = null;
    private Vector3? targetPoint = null;
    private CellSpace? targetSpace = null;

    private List<Vector3> path = new List<Vector3>();

    private AStarToolStatus status = AStarToolStatus.Nothing;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            switch (status)
            {
                case AStarToolStatus.Nothing:
                    Debug.Log("nothing");
                    break;
                case AStarToolStatus.SourceClicked:
                    status = AStarToolStatus.Nothing;
                    sourcePoint = null;
                    Debug.Log("clear source");
                    break;
                case AStarToolStatus.TargetClicked:
                    status = AStarToolStatus.SourceClicked;
                    targetPoint = null;
                    targetSpace = null;
                    path.Clear();
                    Debug.Log("clear target");
                    break;
                default: throw new System.Exception("unknown status: " + status);
            }
        }

        bool clickAStar = false;
        if (Input.GetMouseButtonUp(0))
        {
            switch (status)
            {
                case AStarToolStatus.Nothing:
                    sourcePoint = CameraController.mousePositionOnGround();
                    if (sourcePoint != null)
                    {
                        status = AStarToolStatus.SourceClicked;
                        Debug.Log("get source");
                    }
                    break;
                case AStarToolStatus.SourceClicked:
                    targetPoint = CameraController.mousePositionOnGround();
                    if (targetPoint != null)
                    {
                        var space = IndoorSimData!.indoorFeatures.activeLayer.FindSpaceGeom(U.Vec2Coor(targetPoint.Value));
                        if (space != null)
                        {
                            targetSpace = space;
                            status = AStarToolStatus.TargetClicked;
                            clickAStar = true;
                        }
                        Debug.Log("get target");
                    }
                    break;
                case AStarToolStatus.TargetClicked:
                    targetPoint = CameraController.mousePositionOnGround();
                    if (targetPoint != null)
                    {
                        var space = IndoorSimData!.indoorFeatures.activeLayer.FindSpaceGeom(U.Vec2Coor(targetPoint.Value));
                        if (space != null)
                        {
                            targetSpace = space;
                            status = AStarToolStatus.TargetClicked;
                            clickAStar = true;
                            Debug.Log("update target");
                        }
                        else
                        {
                            status = AStarToolStatus.SourceClicked;
                            targetPoint = null;
                            targetSpace = null;
                            path.Clear();
                            Debug.Log("can not find space container target");
                        }
                    }
                    else
                    {
                        status = AStarToolStatus.SourceClicked;
                        targetPoint = null;
                        targetSpace = null;
                        path.Clear();
                        Debug.Log("target illegal");
                    }
                    break;
                default: throw new System.Exception("unknown status: " + status);
            }
        }

        bool dynamicAStar = false;
        if (status == AStarToolStatus.SourceClicked)
        {
            targetPoint = CameraController.mousePositionOnGround();
            var space2 = IndoorSimData!.indoorFeatures.activeLayer.FindSpaceGeom(U.Vec2Coor(targetPoint.Value));
            if (space2 != null)
            {
                targetSpace = space2;
                dynamicAStar = true;
            }
        }
        else if (status == AStarToolStatus.Nothing)
        {
            targetPoint = null;
            path.Clear();
        }

        if (clickAStar || dynamicAStar)
        {
            PlanResult? result = new IndoorDataAStar(IndoorSimData!.indoorFeatures.activeLayer).Search(U.Vec2Coor(sourcePoint!.Value), targetSpace!);
            PlanSimpleResult? simpleResult = result?.ToSimple();
            if (simpleResult != null && simpleResult.boundaryCentroids.Count > 0)
            {
                path.Clear();
                path.Add(sourcePoint.Value);
                path.AddRange(simpleResult.boundaryCentroids.Select(p => U.Coor2Vec(p.Coordinate)));
                path.Add(targetPoint!.Value);
            }
            else
            {
                if (clickAStar)
                    Debug.LogWarning("can not get path");
                path.Clear();
            }
        }

        DrawPoint(transform.Find("Source").gameObject, sourcePoint);
        DrawPoint(transform.Find("Target").gameObject, targetPoint);
        DrawPath(transform.Find("Path").gameObject, path);
    }

    private static void DrawPoint(GameObject obj, Vector3? sourcePoint)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sourcePoint == null)
        {
            sr.enabled = false;
        }
        else
        {
            sr.enabled = true;
            obj.transform.position = sourcePoint.Value;
        }
    }

    private static void DrawPath(GameObject obj, List<Vector3> path)
    {
        var lr = obj.GetComponent<LineRenderer>();
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());
    }
}
