using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    }

    private bool PaAmrTarget(SpaceController? sc)
        => sc != null && sc.Space.navigable != Navigable.Navigable;

    private bool PaAmrPoiSpace(SpaceController? sc)
        => sc != null && sc.Space.navigable == Navigable.Navigable;

    private bool PickingAgentPoiSpace(SpaceController? sc)
        => sc != null && sc.Space.navigable == Navigable.Navigable;

    void UpdateStatus()
    {
        switch (status)
        {
            case PaAmrPoiMarkerStatus.ContainerSelecting:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    SpaceController? sc = MousePickController.PointedSpace;
                    if (PaAmrTarget(sc))
                    {
                        selectedSpace.Add(sc!);
                    }
                    else if (selectedSpace.Count > 0 && PaAmrPoiSpace(sc))
                    {
                        paAmrPoiPosition = CameraController.mousePositionOnGround() ?? throw new System.Exception("Oops");
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
                    if (PickingAgentPoiSpace(sc))
                    {
                        // TODO: insert
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

    void UpdateView()
    {
        while (pickingAgent2ContainerObj.Count < selectedSpace.Count)
        {
            var obj = Instantiate<GameObject>(Resources.Load<GameObject>("BasicShape/PickingAgent2Container"), this.transform);
            int index = pickingAgent2ContainerObj.Count;
            obj.name = "PickingAgent2Container " + index;
            pickingAgent2ContainerObj.Add(obj);
            obj.GetComponent<LineRenderer>().positionCount = 2;
            obj.GetComponent<LineRenderer>().SetPosition(0, Utils.Coor2Vec(selectedSpace[index].Space.Geom!.Centroid.Coordinate));
        }
        while (pickingAgent2ContainerObj.Count > selectedSpace.Count)
        {
            Destroy(pickingAgent2ContainerObj[pickingAgent2ContainerObj.Count - 1]);
            pickingAgent2ContainerObj.RemoveAt(pickingAgent2ContainerObj.Count - 1);
        }

        Vector3? mousePosition = CameraController.mousePositionOnGround();
        switch (status)
        {
            case PaAmrPoiMarkerStatus.ContainerSelecting:
                if (mousePosition != null)
                {
                    foreach (var obj in pickingAgent2ContainerObj)
                        obj.GetComponent<LineRenderer>().SetPosition(1, mousePosition.Value);
                    transform.Find("PaAmrSprite").gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    transform.Find("PaAmrSprite").position = mousePosition.Value;
                }
                else
                {
                    foreach (var obj in pickingAgent2ContainerObj)
                    {
                        LineRenderer lr = obj.GetComponent<LineRenderer>();
                        lr.SetPosition(1, lr.GetPosition(0));
                    }
                    transform.Find("PaAmrSprite").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                }
                transform.Find("PickingAgentSprite").gameObject.GetComponent<SpriteRenderer>().enabled = false;
                transform.Find("PaAmr2PickingAgent").gameObject.GetComponent<LineRenderer>().enabled = false;
                break;
            case PaAmrPoiMarkerStatus.PaAmrPoiMarked:
                foreach (var obj in pickingAgent2ContainerObj)
                    obj.GetComponent<LineRenderer>().SetPosition(1, paAmrPoiPosition);

                // picking agent sprite
                GameObject PickingAgentSpriteObj = transform.Find("PickingAgentSprite").gameObject;
                PickingAgentSpriteObj.GetComponent<SpriteRenderer>().enabled = true;
                if (mousePosition != null)
                    PickingAgentSpriteObj.transform.position = mousePosition.Value;
                else
                    PickingAgentSpriteObj.transform.position = paAmrPoiPosition;

                // PaAmr sprite
                GameObject PaAmrSpriteObj = transform.Find("PaAmrSprite").gameObject;
                PaAmrSpriteObj.transform.position = paAmrPoiPosition;
                Vector3 delta = PickingAgentSpriteObj.transform.position - paAmrPoiPosition;
                float rotation = (Mathf.Atan2(delta.z, delta.x) - PaAmrFunctionDirection) * 180.0f / Mathf.PI;
                PaAmrSpriteObj.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotation);

                // line renderer between them
                GameObject PaAmr2PickingAgentObj = transform.Find("PaAmr2PickingAgent").gameObject;
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
