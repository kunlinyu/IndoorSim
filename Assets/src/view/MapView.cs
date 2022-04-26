using System;
using System.Collections.Generic;
using UnityEngine;

public class MapView : MonoBehaviour
{
    public IndoorTiling indoorTiling;

    public GameObject vertexParentObj;
    public GameObject boundaryParentObj;
    public GameObject spaceParentObj;
    public Transform vertexParent;
    public Transform boundaryParent;
    public Transform spaceParent;

    public Dictionary<CellVertex, GameObject> vertex2Obj = new Dictionary<CellVertex, GameObject>();
    public Dictionary<CellBoundary, GameObject> boundary2Obj = new Dictionary<CellBoundary, GameObject>();
    private Dictionary<CellSpace, GameObject> cellspace2Obj = new Dictionary<CellSpace, GameObject>();
    private Dictionary<RLineGroup, GameObject> cellspace2RLineObj = new Dictionary<RLineGroup, GameObject>();

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


        indoorTiling.OnVertexCreated += (vertex) =>
        {
            var obj = new GameObject(vertex.Id);
            obj.transform.SetParent(vertexParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            vertex2Obj[vertex] = obj;

            var controller = obj.AddComponent<VertexController>();
            controller.Vertex = vertex;
            controller.material = Resources.Load<Material>("material/vertex material");
            controller.highLightMaterial = Resources.Load<Material>("material/vertex highlight material");
            controller.selectedMaterial = Resources.Load<Material>("material/vertex selected material");
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
            controller.material = Resources.Load<Material>("material/boundary material");
            controller.highLightMaterial = Resources.Load<Material>("material/boundary highlight material");
            controller.selectedMaterial = Resources.Load<Material>("material/boundary selected material");
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
            controller.material = Resources.Load<Material>("material/space material");
            controller.logNonNaviMat = Resources.Load<Material>("material/log non navi space");
            controller.phyNonNaviMat = Resources.Load<Material>("material/phy non navi space");

            controller.highLightMaterial = Resources.Load<Material>("material/space highlight material");
            controller.selectedMaterial = Resources.Load<Material>("material/space selected material");
            controller.triangulationMaterial = Resources.Load<Material>("material/space triangulation material");
            controller.Space = space;
        };
        indoorTiling.OnRLinesCreated += (rLines) =>
        {
            var obj = new GameObject(rLines.space.Id + " rLines");
            obj.transform.SetParent(spaceParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            cellspace2RLineObj[rLines] = obj;

            var controller = obj.AddComponent<RLinesController>();
            controller.material = new Material(Shader.Find("Sprites/Default"));
            controller.material.color = new Color(0.7f, 0.7f, 0.7f);
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
    }

}
