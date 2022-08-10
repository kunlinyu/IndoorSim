using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;

[RequireComponent(typeof(SphereCollider))]
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
            poi.OnLocationPointUpdate += UpdateRenderer;
            poi.OnLocationPointUpdate += UpdateCollider;
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
        => (float)poi.point.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));
    public string Tip()
    {
        List<string> spaceChildrens = poi.spaces.Select(space => string.Join(',', space.children.Select(child => child.containerId))).ToList();
        return $"type: {poi.indoorPOIType}\n" +
               $"container: {string.Join(',', spaceChildrens)}";
    }

    public Func<Container, HashSet<IndoorPOI>> Space2IndoorPOI;

    private List<GameObject> toPOILineObj = new List<GameObject>();

    void UpdateRenderer()
    {
        transform.position = Utils.Point2Vec(poi.point);

        if (poi.indoorPOIType == "PaAmr")
        {
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, Utils.Point2Vec(poi.point));

            HashSet<IndoorPOI> humanPOIs = Space2IndoorPOI(poi.spaces[0]);
            IndoorPOI humanPoi = humanPOIs.FirstOrDefault((poi) => poi.LabelContains("human"));
            if (humanPoi != null)
            {
                lr.SetPosition(1, Utils.Point2Vec(humanPoi.point));
                Vector3 delta = Utils.Point2Vec(humanPoi.point) - Utils.Point2Vec(poi.point);
                float rotation = (Mathf.Atan2(delta.z, delta.x) - PaAmrFunctionDirection) * 180.0f / Mathf.PI;
                transform.localRotation = Quaternion.Euler(90.0f, 0.0f, rotation);
            }
            else
            {
                lr.SetPosition(1, lr.GetPosition(0));
            }
        }

        // to POI line
        GameObject linePrefab = Resources.Load<GameObject>("POIObj/Container2POI");
        while (toPOILineObj.Count < poi.spaces.Count)
        {
            GameObject obj = Instantiate(linePrefab, transform);
            int index = toPOILineObj.Count;
            obj.name = "container 2 POI " + index;
            toPOILineObj.Add(obj);
        }
        while (toPOILineObj.Count > poi.spaces.Count)
        {
            Destroy(toPOILineObj[toPOILineObj.Count - 1]);
            toPOILineObj.RemoveAt(toPOILineObj.Count - 1);
        }

        for (int i = 0; i < poi.spaces.Count; i++)
        {
            var obj = toPOILineObj[i];
            var lr = obj.GetComponent<LineRenderer>();
            if (poi.indoorPOIType == "PaAmr")
            {
                lr.positionCount = 2;
                lr.SetPosition(0, Utils.Point2Vec(poi.spaces[i].Geom.Centroid));
                lr.SetPosition(1, Utils.Point2Vec(poi.point));
            }
            else
            {
                lr.positionCount = 0;
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

    void Update()
    {
        if (needUpdateRenderer)
            UpdateRenderer();
    }

    void OnDestroy()
    {
        poi.OnLocationPointUpdate -= UpdateRenderer;
        poi.OnLocationPointUpdate -= UpdateCollider;
    }
}
