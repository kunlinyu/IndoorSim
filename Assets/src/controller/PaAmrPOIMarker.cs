using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

#nullable enable

enum PaAmrPoiMarkerStatus
{
    RelatedSpaceSelecting,
    RelatedSpaceCommitted,
    PaAmrPoiMarked,
    HumanPoiMarked,

    QueueEntryMarking,
    // end
}



public class PaAmrPOIMarker : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }


    private List<SpaceController> selectedSpace = new List<SpaceController>();
    private List<GameObject> pickingAgent2ContainerObj = new List<GameObject>();
    private Vector3 paAmrPoiPosition;
    private Container paAmrPoiLayOnSpace;
    private Vector3 humanPoiPosition;
    private Container humanPoiLayOnSpace;

    private PaAmrPoiMarkerStatus status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;

    public static float PaAmrFunctionDirection = Mathf.PI;

    private POIType poiType;

    private List<Vector3> path = new List<Vector3>();
    private List<GameObject> queueObj = new List<GameObject>();

    void Start()
    {
        mapView!.SwitchDualityGraph(true);
        MousePickController.pickType = CurrentPickType.Space;
    }

    public void Init(POIType poiType)
    {
        this.poiType = poiType;
    }

    private bool AcceptContainer(Container? container)
    {
        if (container == null) return false;
        return container.navigable != Navigable.Navigable;
    }

    private bool CanLayOn(Container? container)
    {
        if (container == null) return false;
        return container.navigable == Navigable.Navigable;
    }

    void UpdateStatus()
    {
        switch (status)
        {
            case PaAmrPoiMarkerStatus.RelatedSpaceSelecting:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;

                    if (AcceptContainer(sc?.Space))
                    {
                        if (selectedSpace.Contains(sc!))
                            selectedSpace.Remove(sc!);
                        else
                        {
                            selectedSpace.Add(sc!);
                            if (selectedSpace.Count > poiType.relatedCount)
                                selectedSpace.RemoveAt(0);
                        }

                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (selectedSpace.Count > 0)
                        selectedSpace.RemoveAt(selectedSpace.Count - 1);
                }
                else if (Input.GetMouseButtonDown(2))
                {
                    if (selectedSpace.Count > 0)
                        status = PaAmrPoiMarkerStatus.RelatedSpaceCommitted;
                }
                break;
            case PaAmrPoiMarkerStatus.RelatedSpaceCommitted:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (selectedSpace.Count > 0 && CanLayOn(sc?.Space))
                    {
                        paAmrPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
                        paAmrPoiPosition = ClosestEdgeNode(sc, paAmrPoiPosition);
                        paAmrPoiLayOnSpace = sc!.Space;
                        status = PaAmrPoiMarkerStatus.PaAmrPoiMarked;
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;
                }
                break;
            case PaAmrPoiMarkerStatus.PaAmrPoiMarked:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (CanLayOn(sc?.Space))
                    {
                        humanPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
                        humanPoiLayOnSpace = sc!.Space;

                        if (poiType.needQueue)
                        {
                            status = PaAmrPoiMarkerStatus.HumanPoiMarked;
                        }
                        else
                        {
                            Insert();
                            status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    Debug.Log("jump to related space committed");
                    status = PaAmrPoiMarkerStatus.RelatedSpaceCommitted;
                }
                break;
            case PaAmrPoiMarkerStatus.HumanPoiMarked:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (sc == null) break;

                    Vector3 last = paAmrPoiPosition;
                    if (queueObj.Count > 0)
                        last = queueObj[queueObj.Count - 1].transform.position;

                    Vector3 current = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
                    current = ClosestNode(sc, current);
                    CellSpace? lastSpace = IndoorSimData!.indoorData.FindSpaceGeom(U.Vec2Coor(last));
                    List<Vector3>? path = Astar(current, last, lastSpace!);

                    if (path == null) break;

                    GameObject queueSegment = Instantiate(Resources.Load<GameObject>("POI/QueueSegment"), transform);
                    LineRenderer lr = queueSegment.GetComponent<LineRenderer>();
                    lr.positionCount = path.Count;
                    lr.SetPositions(path.ToArray());
                    queueSegment.transform.position = current;

                    Vector3 delta = path[1] - current;
                    float rotation = Mathf.Atan2(delta.z, delta.x) * 180.0f / Mathf.PI;
                    queueSegment.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotation);


                    queueObj.Add(queueSegment);
                }
                else if (Input.GetMouseButtonDown(2))
                {
                    Insert();
                    queueObj.ForEach(queueSeg => Destroy(queueSeg));
                    queueObj.Clear();
                    status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (queueObj.Count > 0)
                    {
                        Destroy(queueObj[queueObj.Count - 1]);
                        queueObj.RemoveAt(queueObj.Count - 1);
                    }
                    else
                    {
                        Debug.Log("jump to poi marked");
                        status = PaAmrPoiMarkerStatus.PaAmrPoiMarked;
                    }
                }

                break;
        }

    }

    private void Insert()
    {
        var spaces = selectedSpace.Select(sc => sc.Space).ToList();
        var containers = new List<Container>(spaces);

        IndoorPOI humanPoi = new IndoorPOI(U.Vec2Point(humanPoiPosition), humanPoiLayOnSpace, containers, new List<Container>(), POICategory.Human.ToString());
        humanPoi.id = "picking poi";  // TODO: this is not ID
        IndoorSimData!.AddPOI(humanPoi);

        List<CellSpace?> queueSpace = queueObj.Select(obj => IndoorSimData.indoorData.FindSpaceGeom(U.Vec2Coor(obj.transform.position))).ToList();
        List<Container> queueContainer = new List<Container>();
        if (queueSpace.Count != 0)
        {
            queueContainer.Add(paAmrPoiLayOnSpace);
            queueContainer.AddRange(queueSpace);
        }

        IndoorPOI paAmrPoi = new IndoorPOI(U.Vec2Point(paAmrPoiPosition), paAmrPoiLayOnSpace, containers, queueContainer, POICategory.PaAmr.ToString());
        paAmrPoi.id = "pa amr poi";  // TODO: this is not ID
        paAmrPoi.AddLabel(poiType.name);
        IndoorSimData.AddPOI(paAmrPoi);
        Debug.Log("POI inserted");
        selectedSpace.Clear();
    }

    private Vector3 ClosestEdgeNode(SpaceController sc, Vector3 mousePosition)
    {
        List<CellBoundary> inOutBound = sc.Space.InOutBound();
        List<LineRenderer> lrs = inOutBound.Select(b => mapView.boundary2Obj[b].transform.Find("Edge").GetComponent<LineRenderer>()).ToList();
        List<LineString> lineStrings = lrs.Select(lr
          => new GeometryFactory().CreateLineString(new Coordinate[] {U.Vec2Coor(lr.GetPosition(0)),
                                                                      U.Vec2Coor(lr.GetPosition(1))})).ToList();

        List<Geometry> edgeNodeGeom = new List<Geometry>(lineStrings);
        edgeNodeGeom.Add(new GeometryFactory().CreatePoint(U.Vec2Coor(sc.transform.Find("Node").position)));

        GeometryCollection gc = new GeometryFactory().CreateGeometryCollection(edgeNodeGeom.ToArray());

        Coordinate[] nearestCoor = DistanceOp.NearestPoints(gc, new GeometryFactory().CreatePoint(U.Vec2Coor(mousePosition)));
        return U.Coor2Vec(nearestCoor[0]);
    }

    private Vector3 ClosestNode(SpaceController sc, Vector3 mousePosition)
    {
        return sc.transform.Find("Node").position;
    }

    private List<Vector3>? Astar(Vector3 source, Vector3 target, CellSpace targetSpace)
    {
        PlanResult? result = new IndoorDataAStar(IndoorSimData!.indoorData).Search(U.Vec2Coor(source), targetSpace);
        PlanSimpleResult? simpleResult = result?.ToSimple();

        List<Vector3> path = new List<Vector3>();
        if (simpleResult != null && simpleResult.boundaryCentroids.Count > 0)
        {
            path.Clear();
            path.Add(source);
            path.AddRange(simpleResult.boundaryCentroids.Select(p => U.Coor2Vec(p.Coordinate)));
            path.Add(target);
            return path;
        }

        return null;
    }

    void UpdateView()
    {
        while (pickingAgent2ContainerObj.Count > selectedSpace.Count)
        {
            int count = pickingAgent2ContainerObj.Count;
            Destroy(pickingAgent2ContainerObj[count - 1]);
            pickingAgent2ContainerObj.RemoveAt(count - 1);
        }
        while (pickingAgent2ContainerObj.Count < selectedSpace.Count)
        {
            var obj = Instantiate<GameObject>(Resources.Load<GameObject>("POI/Container2POI"), this.transform);
            int index = pickingAgent2ContainerObj.Count;
            obj.name = "Container2POI " + index;
            pickingAgent2ContainerObj.Add(obj);
        }
        for (int i = 0; i < pickingAgent2ContainerObj.Count; i++)
        {
            var obj = pickingAgent2ContainerObj[i];
            obj.GetComponent<LineRenderer>().positionCount = 2;
            obj.GetComponent<LineRenderer>().SetPosition(0, U.Coor2Vec(selectedSpace[i].Space.Geom!.Centroid.Coordinate));
        }

        transform.Find("QueueSegment").gameObject.GetComponent<LineRenderer>().enabled = false;
        transform.Find("QueueSegment").gameObject.GetComponent<SpriteRenderer>().enabled = false;

        Vector3? mousePosition = CameraController.mousePositionOnGround();
        switch (status)
        {
            case PaAmrPoiMarkerStatus.RelatedSpaceSelecting:
                transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                transform.Find("PickingPOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                transform.Find("PaAmr2Picking").gameObject.GetComponent<LineRenderer>().enabled = false;
                if (mousePosition != null)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    Vector3 position = mousePosition.Value;
                    if (sc != null)
                    {
                        transform.Find("PosePOIDark").gameObject.GetComponent<SpriteRenderer>().enabled = true;
                        transform.Find("PosePOIDark").position = position;
                    }
                    else
                    {
                        transform.Find("PosePOIDark").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    }
                    foreach (var obj in pickingAgent2ContainerObj)
                    {
                        obj.GetComponent<LineRenderer>().enabled = true;
                        obj.GetComponent<LineRenderer>().SetPosition(1, position);
                    }
                }
                else
                {
                    transform.Find("PosePOIDark").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                }
                break;
            case PaAmrPoiMarkerStatus.RelatedSpaceCommitted:
                if (mousePosition != null)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (sc != null)
                    {
                        Vector3 position = mousePosition.Value;
                        if (CanLayOn(sc.Space))
                        {
                            transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = true;
                            transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().color = poiType.color;
                            transform.Find("PosePOIDark").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                            position = ClosestEdgeNode(sc, mousePosition.Value);
                            transform.Find("PosePOI").position = position;
                        }
                        else
                        {
                            transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                            transform.Find("PosePOIDark").gameObject.GetComponent<SpriteRenderer>().enabled = true;
                            position = mousePosition.Value;
                            transform.Find("PosePOIDark").position = position;
                        }
                        foreach (var obj in pickingAgent2ContainerObj)
                        {
                            obj.GetComponent<LineRenderer>().enabled = true;
                            obj.GetComponent<LineRenderer>().SetPosition(1, position);
                        }
                    }
                    else
                    {
                        transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                        foreach (var obj in pickingAgent2ContainerObj)
                            obj.GetComponent<LineRenderer>().enabled = false;
                    }
                }
                else
                {
                    foreach (var obj in pickingAgent2ContainerObj)
                    {
                        LineRenderer lr = obj.GetComponent<LineRenderer>();
                        lr.SetPosition(1, lr.GetPosition(0));
                    }
                    transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                }
                transform.Find("PickingPOI").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                transform.Find("PaAmr2Picking").gameObject.GetComponent<LineRenderer>().enabled = false;
                break;
            case PaAmrPoiMarkerStatus.PaAmrPoiMarked:
                foreach (var obj in pickingAgent2ContainerObj)
                    obj.GetComponent<LineRenderer>().SetPosition(1, paAmrPoiPosition);

                // picking agent sprite
                GameObject PickingAgentSpriteObj = transform.Find("PickingPOI").gameObject;
                PickingAgentSpriteObj.GetComponent<SpriteRenderer>().enabled = true;
                if (mousePosition != null)
                    PickingAgentSpriteObj.transform.position = mousePosition.Value;
                else
                    PickingAgentSpriteObj.transform.position = paAmrPoiPosition;

                // PaAmr sprite
                GameObject PaAmrSpriteObj = transform.Find("PosePOI").gameObject;
                PaAmrSpriteObj.gameObject.GetComponent<SpriteRenderer>().color = poiType.color;
                PaAmrSpriteObj.transform.position = paAmrPoiPosition;
                Vector3 delta = PickingAgentSpriteObj.transform.position - paAmrPoiPosition;
                float rotation = (Mathf.Atan2(delta.z, delta.x) - PaAmrFunctionDirection) * 180.0f / Mathf.PI;
                PaAmrSpriteObj.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotation);

                // line renderer between them
                GameObject PaAmr2PickingAgentObj = transform.Find("PaAmr2Picking").gameObject;
                PaAmr2PickingAgentObj.GetComponent<LineRenderer>().enabled = true;
                PaAmr2PickingAgentObj.GetComponent<LineRenderer>().positionCount = 2;
                PaAmr2PickingAgentObj.GetComponent<LineRenderer>().SetPosition(0, PaAmrSpriteObj.transform.position);
                PaAmr2PickingAgentObj.GetComponent<LineRenderer>().SetPosition(1, PickingAgentSpriteObj.transform.position);
                break;
            case PaAmrPoiMarkerStatus.HumanPoiMarked:
                if (mousePosition != null)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    Vector3 current = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
                    var queueSegObj = transform.Find("QueueSegment").gameObject;
                    Vector3 last = paAmrPoiPosition;
                    if (this.queueObj.Count > 0)
                        last = this.queueObj[this.queueObj.Count - 1].transform.position;
                    CellSpace? lastCellSpace = IndoorSimData!.indoorData.FindSpaceGeom(U.Vec2Coor(last));

                    if (sc != null && sc.Space.navigable == Navigable.Navigable && sc.Space != paAmrPoiLayOnSpace && sc.Space != lastCellSpace)
                    {
                        queueSegObj.transform.position = ClosestNode(sc, current);

                        queueSegObj.GetComponent<SpriteRenderer>().enabled = true;

                        LineRenderer lr = queueSegObj.GetComponent<LineRenderer>();
                        lr.enabled = true;
                        lr.positionCount = 2;



                        var path = Astar(queueSegObj.transform.position, last, lastCellSpace!);
                        if (path != null)
                        {
                            lr.enabled = true;
                            lr.positionCount = path.Count;
                            lr.SetPositions(path.ToArray());

                            Vector3 d = path[1] - queueSegObj.transform.position;
                            float rot = Mathf.Atan2(d.z, d.x) * 180.0f / Mathf.PI;
                            queueSegObj.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rot);
                        }
                        else
                        {
                            lr.enabled = false;
                        }
                    }
                    else
                    {
                        queueSegObj.GetComponent<SpriteRenderer>().enabled = false;
                        queueSegObj.GetComponent<LineRenderer>().enabled = false;
                    }
                }
                break;
        }
    }

    void Update()
    {
        UpdateStatus();
        UpdateView();
    }
}
