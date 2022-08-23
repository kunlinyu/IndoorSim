using System;
using System.Collections.Generic;
using UnityEngine;


public class IndoorMapView : MonoBehaviour
{
    public IndoorTiling indoorTiling;
    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;

    private GameObject vertexParentObj;
    private GameObject boundaryParentObj;
    private GameObject spaceParentObj;
    private GameObject rLineParentObj;
    private GameObject POIParentObj;

    public Dictionary<CellVertex, GameObject> vertex2Obj = new Dictionary<CellVertex, GameObject>();
    public Dictionary<CellBoundary, GameObject> boundary2Obj = new Dictionary<CellBoundary, GameObject>();
    public Dictionary<CellSpace, GameObject> cellspace2Obj = new Dictionary<CellSpace, GameObject>();
    public Dictionary<RLineGroup, GameObject> rLine2Obj = new Dictionary<RLineGroup, GameObject>();
    public Dictionary<IndoorPOI, GameObject> poi2Obj = new Dictionary<IndoorPOI, GameObject>();

    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }
    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);

        vertexParentObj = transform.Find("Vertices").gameObject;
        boundaryParentObj = transform.Find("Boundaries").gameObject;
        spaceParentObj = transform.Find("Spaces").gameObject;
        rLineParentObj = transform.Find("RLines").gameObject;
        POIParentObj = transform.Find("POIs").gameObject;

        indoorTiling.OnVertexCreated += (vertex) =>
        {
            var obj = Instantiate(Resources.Load<GameObject>("BasicShape/Vertex"), vertexParentObj.transform);
            obj.name = vertex.Id;
            obj.GetComponent<VertexController>().Vertex = vertex;
            vertex2Obj[vertex] = obj;
        };
        indoorTiling.OnBoundaryCreated += (boundary) =>
        {
            var obj = Instantiate(Resources.Load<GameObject>("BasicShape/Boundary"), boundaryParentObj.transform);
            obj.name = boundary.Id;
            obj.GetComponent<BoundaryController>().Boundary = boundary;
            boundary2Obj[boundary] = obj;
        };
        indoorTiling.OnSpaceCreated += (space) =>
        {
            var obj = Instantiate(Resources.Load<GameObject>("BasicShape/Space"), spaceParentObj.transform);
            obj.name = space.Id;
            obj.GetComponent<SpaceController>().Space = space;
            cellspace2Obj[space] = obj;
        };
        indoorTiling.OnRLinesCreated += (rLines) =>
        {
            // TODO: make rline game object to be prefab
            var obj = new GameObject(rLines.space.Id + " rLines");
            obj.transform.SetParent(rLineParentObj.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            rLine2Obj[rLines] = obj;

            var controller = obj.AddComponent<RLinesController>();
            controller.material = Resources.Load<Material>("Materials/arrow");
            controller.materialDark = Resources.Load<Material>("Materials/arrow dark");
            controller.materialHighlight = Resources.Load<Material>("Materials/arrow highlight");
            controller.RLines = rLines;
        };
        indoorTiling.OnPOICreated += (poi) =>
        {
            string poiObjPath;
            if (poi.indoorPOIType == "PaAmr")
                poiObjPath = "POI/PaAmrPOI";
            else if (poi.indoorPOIType == "human")
                poiObjPath = "POI/HumanPOI";
            else
            {
                poiObjPath = "POI/DefaultPOI";
                Debug.LogWarning("Unknow poi type");
            }

            var obj = Instantiate(Resources.Load<GameObject>(poiObjPath), POIParentObj.transform);
            obj.name = poi.id;
            obj.GetComponent<POIController>().Poi = poi;
            obj.GetComponent<POIController>().Space2IndoorPOI = (space) => indoorTiling.indoorData.Space2POIs(space);
            poi2Obj[poi] = obj;
        };

        indoorTiling.OnVertexRemoved += (vertex) =>
        {
            Destroy(vertex2Obj[vertex]);
            vertex2Obj.Remove(vertex);
        };
        indoorTiling.OnBoundaryRemoved += (boundary) =>
        {
            Destroy(boundary2Obj[boundary]);
            boundary2Obj.Remove(boundary);
        };
        indoorTiling.OnSpaceRemoved += (space) =>
        {
            Destroy(cellspace2Obj[space]);
            cellspace2Obj.Remove(space);
        };
        indoorTiling.OnRLinesRemoved += (rLines) =>
        {
            Destroy(rLine2Obj[rLines]);
            rLine2Obj.Remove(rLines);
        };
        indoorTiling.OnPOIRemoved += (poi) =>
        {
            Destroy(poi2Obj[poi]);
            poi2Obj.Remove(poi);
        };
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ViewButton)
        {
            bool status;
            if (e.message == "enable") status = true;
            else if (e.message == "disable") status = false;
            else throw new ArgumentException("unknow message of view button");

            if (e.name == "view rline")
            {
                rLineParentObj.SetActive(status);
            }
            if (e.name == "duality graph")
            {
                SwitchDualityGraph(status);
            }
        }
    }

    public void SwitchDualityGraph(bool status)
    {
        foreach (var obj in cellspace2Obj.Values)
        {
            GameObject node = obj.transform.Find("Node").gameObject;
            node.SetActive(status);
        }
        foreach (var obj in boundary2Obj.Values)
        {
            GameObject edge = obj.transform.Find("Edge").gameObject;
            edge.SetActive(status);
        }
    }

}
