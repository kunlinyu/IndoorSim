using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public class AssetApplier : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    public int assetId;
    private Asset? asset = null;

    List<GameObject> boundaryRenderObjs = new List<GameObject>();

    IndoorData? assetIndoorData = null;

    float rotation = 0.0f;
    float rotationAnchor = 0.0f;
    Vector3 anchorMouse;


    const float kRatationSpeed = 0.05f;  // should be same to CameraController.rotationSpeed

    void Update()
    {
        if (asset == null)
        {
            Debug.Log("asset id: " + assetId);
            asset = IndoorSim.indoorTiling.assets[assetId];
            assetIndoorData = IndoorData.Deserialize(asset.Value.json);
            if (assetIndoorData == null) throw new System.Exception("can not deserialize asset");

            boundaryRenderObjs.Clear();
            foreach (var boundary in assetIndoorData.boundaryPool)
            {
                GameObject obj = new GameObject("asset boundary");
                obj.transform.SetParent(transform);
                obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.material = draftMaterial;
                lr.alignment = LineAlignment.TransformZ;    // border should face to sky
                lr.useWorldSpace = true;
                lr.loop = false;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.numCapVertices = 3;
                lr.sortingLayerID = sortingLayerId;
                lr.sortingOrder = 10;
                boundaryRenderObjs.Add(obj);
            }

        }

        Vector3? mousePosition = CameraController.mousePositionOnGround();
        if (mousePosition == null) return;
        if (assetIndoorData == null) return;


        Vector3 center = new Vector3((float)asset.Value.centerX, 0.0f, (float)asset.Value.centerY);
        for (int i = 0; i < assetIndoorData.boundaryPool.Count; i++)
        {
            Quaternion rot = Quaternion.AngleAxis(rotation, Vector3.up);
            Vector3 p0 = rot * (Utils.Coor2Vec(assetIndoorData.boundaryPool[i].P0.Coordinate) - center) + mousePosition.Value;
            Vector3 p1 = rot * (Utils.Coor2Vec(assetIndoorData.boundaryPool[i].P1.Coordinate) - center) + mousePosition.Value;
            boundaryRenderObjs[i].GetComponent<LineRenderer>().positionCount = 2;
            boundaryRenderObjs[i].GetComponent<LineRenderer>().SetPosition(0, p0);
            boundaryRenderObjs[i].GetComponent<LineRenderer>().SetPosition(1, p1);
        }

        if (Input.GetMouseButtonDown(0) && !MouseOnUI)
        {
            IndoorSim.indoorTiling.SessionStart();
            foreach (var obj in boundaryRenderObjs)
            {
                Coordinate coor0 = Utils.Vec2Coor(obj.GetComponent<LineRenderer>().GetPosition(0));
                Coordinate coor1 = Utils.Vec2Coor(obj.GetComponent<LineRenderer>().GetPosition(1));
                IndoorSim.indoorTiling.AddBoundaryAutoSnap(coor0, coor1);
            }
            IndoorSim.indoorTiling.SessionCommit();

            assetIndoorData = null;
            boundaryRenderObjs.ForEach(obj => Destroy(obj));
            boundaryRenderObjs.Clear();
        }

        if (Input.GetMouseButtonDown(1))
        {
            anchorMouse = Input.mousePosition;
            rotationAnchor = rotation;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - anchorMouse;
            rotation = rotationAnchor - (delta.x * kRatationSpeed);
        }

    }
}
