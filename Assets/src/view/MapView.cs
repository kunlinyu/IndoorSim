using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class MapView : MonoBehaviour
{
    public IndoorTiling indoorTiling;

    private GameObject vertexParentObj;
    private GameObject boundaryParentObj;
    private GameObject spaceParentObj;
    private GameObject rLineParentObj;

    private Transform vertexParent;
    private Transform boundaryParent;
    private Transform spaceParent;
    private Transform rLineParent;

    public Dictionary<CellVertex, GameObject> vertex2Obj = new Dictionary<CellVertex, GameObject>();
    public Dictionary<CellBoundary, GameObject> boundary2Obj = new Dictionary<CellBoundary, GameObject>();
    public Dictionary<CellSpace, GameObject> cellspace2Obj = new Dictionary<CellSpace, GameObject>();
    private Dictionary<RLineGroup, GameObject> cellspace2RLineObj = new Dictionary<RLineGroup, GameObject>();

    public UIEventDispatcher? eventDispatcher;

    void Start()
    {

        vertexParentObj = new GameObject("vertex parent");
        vertexParentObj.transform.SetParent(transform);
        vertexParentObj.transform.localPosition = Vector3.zero;
        vertexParentObj.transform.localRotation = Quaternion.identity;

        vertexParent = vertexParentObj.transform;

        boundaryParentObj = new GameObject("boundary parent");
        boundaryParentObj.transform.SetParent(transform);
        boundaryParentObj.transform.localPosition = Vector3.zero;
        boundaryParentObj.transform.localRotation = Quaternion.identity;
        boundaryParent = boundaryParentObj.transform;

        spaceParentObj = new GameObject("space parent");
        spaceParentObj.transform.SetParent(transform);
        spaceParentObj.transform.localPosition = Vector3.zero;
        spaceParentObj.transform.localRotation = Quaternion.identity;
        spaceParent = spaceParentObj.transform;

        rLineParentObj = new GameObject("rLine parent");
        rLineParentObj.transform.SetParent(transform);
        rLineParentObj.transform.localPosition = Vector3.zero;
        rLineParentObj.transform.localRotation = Quaternion.identity;
        rLineParent = rLineParentObj.transform;


        indoorTiling.OnVertexCreated += (vertex) =>
        {
            var obj = new GameObject(vertex.Id);
            obj.transform.SetParent(vertexParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            vertex2Obj[vertex] = obj;

            var controller = obj.AddComponent<VertexController>();
            controller.Vertex = vertex;
            controller.material = Resources.Load<Material>("Materials/vertex material");
            controller.highLightMaterial = Resources.Load<Material>("Materials/vertex highlight material");
            controller.selectedMaterial = Resources.Load<Material>("Materials/vertex selected material");
            controller.sortingLayerId = SortingLayer.NameToID("traffic");
        };
        indoorTiling.OnBoundaryCreated += (boundary) =>
        {
            var obj = new GameObject(boundary.Id);
            obj.transform.SetParent(boundaryParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            boundary2Obj[boundary] = obj;

            var controller = obj.AddComponent<BoundaryController>();
            controller.material = Resources.Load<Material>("Materials/boundary material");
            controller.logNonNaviMat = Resources.Load<Material>("Materials/boundary log non navi");
            controller.phyNonNaviMat = Resources.Load<Material>("Materials/boundary phy non navi");
            controller.highLightMaterial = Resources.Load<Material>("Materials/boundary highlight material");
            controller.selectedMaterial = Resources.Load<Material>("Materials/boundary selected material");
            controller.Boundary = boundary;
        };
        indoorTiling.OnSpaceCreated += (space) =>
        {
            var obj = new GameObject(space.Id);
            obj.transform.SetParent(spaceParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            cellspace2Obj[space] = obj;

            var controller = obj.AddComponent<SpaceController>();
            controller.material = Resources.Load<Material>("Materials/space material");
            controller.logNonNaviMat = Resources.Load<Material>("Materials/space log non navi");
            controller.phyNonNaviMat = Resources.Load<Material>("Materials/space phy non navi");

            controller.highLightMaterial = Resources.Load<Material>("Materials/space highlight material");
            controller.selectedMaterial = Resources.Load<Material>("Materials/space selected material");
            controller.triangulationMaterial = Resources.Load<Material>("Materials/space triangulation material");
            controller.Space = space;
        };
        indoorTiling.OnRLinesCreated += (rLines) =>
        {
            var obj = new GameObject(rLines.space.Id + " rLines");
            obj.transform.SetParent(rLineParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            cellspace2RLineObj[rLines] = obj;

            var controller = obj.AddComponent<RLinesController>();
            controller.material = Resources.Load<Material>("Materials/arrow");
            controller.materialDark = Resources.Load<Material>("Materials/arrow dark");
            controller.materialHighlight = Resources.Load<Material>("Materials/arrow highlight");
            controller.RLines = rLines;
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
            Destroy(cellspace2RLineObj[rLines]);
            cellspace2RLineObj.Remove(rLines);
        };

        indoorTiling.OnAssetUpdated += (assets) =>
        {
            var e = new UIEvent();
            e.type = UIEventType.Asset;
            e.name = "list";
            e.message = JsonConvert.SerializeObject(assets, new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.Indented });

            eventDispatcher?.Raise(this, e);
        };
    }

}
