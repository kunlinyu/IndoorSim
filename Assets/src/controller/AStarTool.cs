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
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
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

        bool shouldRunAStar = false;
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
                        var space = IndoorSimData!.indoorData.FindSpaceGeom(Utils.Vec2Coor(targetPoint.Value));
                        if (space != null)
                        {
                            targetSpace = space;
                            status = AStarToolStatus.TargetClicked;
                            shouldRunAStar = true;
                        }
                        Debug.Log("get target");
                    }
                    break;
                case AStarToolStatus.TargetClicked:
                    targetPoint = CameraController.mousePositionOnGround();
                    if (targetPoint != null)
                    {
                        var space = IndoorSimData!.indoorData.FindSpaceGeom(Utils.Vec2Coor(targetPoint.Value));
                        if (space != null)
                        {
                            targetSpace = space;
                            status = AStarToolStatus.TargetClicked;
                            shouldRunAStar = true;
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

        if (shouldRunAStar)
        {
            Debug.Log("run A*");
            shouldRunAStar = false;
            PlanResult? result = new IndoorDataAStar(IndoorSimData!.indoorData).Search(Utils.Vec2Coor(sourcePoint!.Value), targetSpace!);
            if (result != null)
            {
                PlanSimpleResult simpleResult = result.ToSimple();
                path = simpleResult.boundaryCentroids.Select(p => Utils.Coor2Vec(p.Coordinate)).ToList();
                Debug.Log("get path");
            }
            else
            {
                Debug.Log("can not get path");
                path.Clear();
            }
        }

        DrawSourcePoint(transform.Find("Source").gameObject, sourcePoint);
        DrawTargetPoint(transform.Find("Target").gameObject, targetPoint);
        DrawPath(transform.Find("Path").gameObject, path);
    }

    private static void DrawSourcePoint(GameObject obj, Vector3? sourcePoint)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sourcePoint == null)
        {
            sr.sprite = null;
        }
        else
        {
            sr.sprite = Resources.Load<Sprite>("astar/astar_node");
            obj.transform.position = sourcePoint.Value;
            obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }
    }

    private static void DrawTargetPoint(GameObject obj, Vector3? targetPoint)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (targetPoint == null)
        {
            sr.sprite = null;
        }
        else
        {
            sr.sprite = Resources.Load<Sprite>("astar/astar_node");
            obj.transform.position = targetPoint.Value;
            obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }
    }

    private static void DrawPath(GameObject obj, List<Vector3> path)
    {
        var lr = obj.GetComponent<LineRenderer>();
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());
    }
}