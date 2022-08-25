using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

#nullable enable

enum PaAmrPoiMarkerStatus
{
    ContainerSelecting,
    PaAmrPoiMarked,

    // PickingAgentPoiMarked
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

    private PaAmrPoiMarkerStatus status = PaAmrPoiMarkerStatus.ContainerSelecting;

    public static float PaAmrFunctionDirection = Mathf.PI;

    void Start()
    {
        mapView!.SwitchDualityGraph(true);
    }

    void UpdateStatus()
    {
        switch (status)
        {
            case PaAmrPoiMarkerStatus.ContainerSelecting:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;

                    if (PaAmrPoi.AcceptContainerStatic(sc?.Space) && HumanPOI.AcceptContainerStatic(sc?.Space))
                    {
                        if (selectedSpace.Contains(sc!))
                            selectedSpace.Remove(sc!);
                        else selectedSpace.Add(sc!);
                    }
                    else if (selectedSpace.Count > 0 && PaAmrPoi.CanLayOnStatic(sc?.Space))
                    {
                        paAmrPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
                        paAmrPoiPosition = ClosestEdgeNode(sc, paAmrPoiPosition);
                        status = PaAmrPoiMarkerStatus.PaAmrPoiMarked;
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (selectedSpace.Count > 0)
                        selectedSpace.RemoveAt(selectedSpace.Count - 1);
                }
                break;
            case PaAmrPoiMarkerStatus.PaAmrPoiMarked:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (HumanPOI.CanLayOnStatic(sc?.Space))
                    {
                        // insert
                        Vector3 pickingPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");

                        var spaces = selectedSpace.Select(sc => sc.Space).ToList();
                        var containers = new List<Container>(spaces);

                        HumanPOI humanPoi = new HumanPOI(U.Vec2Point(pickingPoiPosition), containers);
                        humanPoi.id = "picking poi";
                        IndoorSimData!.AddPOI(humanPoi);

                        var paAmrPoi = new PaAmrPoi(U.Vec2Point(paAmrPoiPosition), containers);
                        paAmrPoi.id = "pa amr poi";
                        IndoorSimData.AddPOI(paAmrPoi);
                        Debug.Log("POI inserted");
                        selectedSpace.Clear();
                        status = PaAmrPoiMarkerStatus.ContainerSelecting;
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    status = PaAmrPoiMarkerStatus.ContainerSelecting;
                }
                break;
        }

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
        if (pickingAgent2ContainerObj.Count > selectedSpace.Count)
        {
            pickingAgent2ContainerObj.ForEach(obj => Destroy(obj));
            pickingAgent2ContainerObj.Clear();
        }
        while (pickingAgent2ContainerObj.Count < selectedSpace.Count)
        {
            var obj = Instantiate<GameObject>(Resources.Load<GameObject>("POI/Container2POI"), this.transform);
            int index = pickingAgent2ContainerObj.Count;
            obj.name = "Container2POI " + index;
            pickingAgent2ContainerObj.Add(obj);
            obj.GetComponent<LineRenderer>().positionCount = 2;
            obj.GetComponent<LineRenderer>().SetPosition(0, U.Coor2Vec(selectedSpace[index].Space.Geom!.Centroid.Coordinate));
        }

        Vector3? mousePosition = CameraController.mousePositionOnGround();
        switch (status)
        {
            case PaAmrPoiMarkerStatus.ContainerSelecting:
                if (mousePosition != null)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (sc != null)
                    {
                        Vector3 position = mousePosition.Value;
                        if (PaAmrPoi.CanLayOnStatic(sc.Space))
                        {
                            transform.Find("PosePOI").gameObject.GetComponent<SpriteRenderer>().enabled = true;
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
