using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(SpriteRenderer))]
public class POIController : MonoBehaviour, Selectable
{
    private IndoorPOI poi;

    public static float PaAmrFunctionDirection = Mathf.PI;

    public IndoorPOI Poi
    {
        get => poi;
        set
        {
            poi = value;
            poi.OnLocationUpdate += UpdateRenderer;
            poi.OnLocationUpdate += UpdateCollider;
        }
    }

    private bool _highLight = false;
    private bool needUpdateRenderer = true;
    public bool highLight
    {
        get => _highLight;
        set
        {
            _highLight = value;
            needUpdateRenderer = true;
        }
    }
    private bool _selected = false;
    public bool selected
    {
        get => _selected;
        set
        {
            _selected = value;
            needUpdateRenderer = true;
        }
    }
    public SelectableType type { get => SelectableType.POI; }

    public float Distance(Vector3 vec)
        => (float)poi.point.Distance(new GeometryFactory().CreatePoint(U.Vec2Coor(vec)));
    public string Tip()
    {
        List<string> spaceChildrens = poi.foi.Select(space => string.Join(',', space.children.Select(child => child.containerId))).ToList();
        return $"category: {string.Join(',', poi.category.Select(category => category.term))}\n" +
               $"labels: {string.Join(',', poi.label.Select(label => label.value))}\n" +
               $"container: {string.Join(',', spaceChildrens)}";
    }

    public Func<Container, HashSet<IndoorPOI>> Space2IndoorPOI;

    private List<GameObject> toPOILineObj = new List<GameObject>();
    private List<GameObject> queueSpace = new List<GameObject>();

    private List<POIType> allPOITypes;

    void UpdateRenderer()
    {
        transform.position = U.Point2Vec(poi.point);
        GetComponent<SpriteRenderer>().size = spriteSize;

        // PaAmr 2 Human linerenderer
        if (poi.CategoryContains(POICategory.PaAmr.ToString()))
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, U.Point2Vec(poi.point));

            HashSet<IndoorPOI> humanPOIs = Space2IndoorPOI?.Invoke(poi.foi[0]);
            IndoorPOI humanPoi = humanPOIs.FirstOrDefault((poi) => poi.CategoryContains(POICategory.Human.ToString()));
            if (humanPoi != null)
            {
                lr.SetPosition(1, U.Point2Vec(humanPoi.point));
                Vector3 delta = U.Point2Vec(humanPoi.point) - U.Point2Vec(poi.point);
                float rotation = (Mathf.Atan2(delta.z, delta.x) - PaAmrFunctionDirection) * 180.0f / Mathf.PI;
                transform.localRotation = Quaternion.Euler(90.0f, 0.0f, rotation);
            }
            else
            {
                lr.SetPosition(1, lr.GetPosition(0));
            }
        }

        // container to POI linerenderer
        GameObject linePrefab = Resources.Load<GameObject>("POI/Container2POI");
        while (toPOILineObj.Count < poi.foi.Count)
        {
            GameObject obj = Instantiate(linePrefab, transform);
            obj.name = "container_2_POI " + toPOILineObj.Count;
            toPOILineObj.Add(obj);
        }
        while (toPOILineObj.Count > poi.foi.Count)
        {
            Destroy(toPOILineObj[toPOILineObj.Count - 1]);
            toPOILineObj.RemoveAt(toPOILineObj.Count - 1);
        }

        for (int i = 0; i < poi.foi.Count; i++)
        {
            var obj = toPOILineObj[i];
            var lr = obj.GetComponent<LineRenderer>();
            if (poi.CategoryContains(POICategory.PaAmr.ToString()))
            {
                lr.positionCount = 2;
                lr.SetPosition(0, U.Point2Vec(poi.foi[i].Geom.Centroid));
                lr.SetPosition(1, U.Point2Vec(poi.point));
            }
            else
            {
                lr.positionCount = 0;
            }
        }

        // queue
        if (poi.queue.Count == 1)
            throw new ArgumentException("Count queue of poi should not be 1");
        else if (poi.queue.Count > 1)
        {


            GameObject queueSpacePrefab = Resources.Load<GameObject>("POI/QueueSegment");
            while (queueSpace.Count < poi.queue.Count)
            {
                GameObject obj = Instantiate(queueSpacePrefab, transform);
                obj.name = "queue space " + queueSpace.Count;
                queueSpace.Add(obj);
            }
            while (queueSpace.Count > poi.queue.Count)
            {
                Destroy(queueSpace[queueSpace.Count - 1]);
                queueSpace.RemoveAt(queueSpace.Count - 1);
            }
            for (int i = 0; i < poi.queue.Count; i++)
            {
                var obj = queueSpace[i];
                if (i == 0)
                {
                    obj.GetComponent<LineRenderer>().enabled = false;
                    continue;
                }
                var lastObj = queueSpace[i - 1];
                obj.transform.position = U.Point2Vec(poi.queue[i].Geom.Centroid);

                LineRenderer lr = obj.GetComponent<LineRenderer>();
                lr.SetPosition(0, obj.transform.position);
                lr.SetPosition(1, lastObj.transform.position);

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                Vector3 d = lastObj.transform.position - obj.transform.position;
                float rot = Mathf.Atan2(d.z, d.x) * 180.0f / Mathf.PI;
                obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, rot);

            }
        }
    }

    void UpdateCollider()
    {
        GetComponent<SphereCollider>().center = Vector3.zero;
    }

    void Start()
    {
        UpdateRenderer();
        UpdateCollider();
    }


    private int lastCameraHeightInt;
    private Vector2 spriteSize = new Vector2(0.5f, 0.5f);
    void Update()
    {
        int newHeightInt = (int)(CameraController.CameraPosition.y * 0.5f);
        if (lastCameraHeightInt != newHeightInt)
        {
            lastCameraHeightInt = newHeightInt;
            needUpdateRenderer = true;

            spriteSize.x = Mathf.Sqrt(newHeightInt + 2) * 0.2f + 0.1f;
            spriteSize.y = Mathf.Sqrt(newHeightInt + 2) * 0.2f + 0.1f;
        }
        if (needUpdateRenderer)
            UpdateRenderer();
    }

    void OnDestroy()
    {
        poi.OnLocationUpdate -= UpdateRenderer;
        poi.OnLocationUpdate -= UpdateCollider;
    }
}
