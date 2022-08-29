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

    private PaAmrPoiMarkerStatus status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;

    public static float PaAmrFunctionDirection = Mathf.PI;

    private POIType poiType;

    void Start()
    {
        mapView!.SwitchDualityGraph(true);
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
                        Insert();

                        // if (poiType.needQueue)
                        //     status = PaAmrPoiMarkerStatus.HumanPoiMarked;
                        // else
                        //     status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;
                        status = PaAmrPoiMarkerStatus.RelatedSpaceSelecting;
                    }
                }
                else if (Input.GetMouseButton(1))
                {

                    status = PaAmrPoiMarkerStatus.RelatedSpaceCommitted;
                }
                break;
            // case PaAmrPoiMarkerStatus.HumanPoiMarked:
            //     break;
        }

    }

    private void Insert()
    {
        Vector3 pickingPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");

        var spaces = selectedSpace.Select(sc => sc.Space).ToList();
        var containers = new List<Container>(spaces);

        IndoorPOI humanPoi = new IndoorPOI(U.Vec2Point(pickingPoiPosition), containers, POICategory.Human.ToString());
        humanPoi.id = "picking poi";  // TODO: this is not ID
        IndoorSimData!.AddPOI(humanPoi);

        IndoorPOI paAmrPoi = new IndoorPOI(U.Vec2Point(paAmrPoiPosition), containers, POICategory.PaAmr.ToString());
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
        }
    }

    void Update()
    {
        UpdateStatus();
        UpdateView();
    }
}
