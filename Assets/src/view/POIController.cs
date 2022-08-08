using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;

public class POIController : MonoBehaviour
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
        }
    }

    private List<GameObject> toPOILineObj = new List<GameObject>();

    void UpdateRenderer()
    {
        transform.position = Utils.Point2Vec((Point)poi.location.point.geometry);

        if (poi.indoorPOIType == "PaAmr")
        {
            PaAmrPoi paAmrPoi = poi as PaAmrPoi;
            LineRenderer lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, Utils.Point2Vec((Point)paAmrPoi.pickingPOI.location.point.geometry));
            lr.SetPosition(1, Utils.Point2Vec((Point)paAmrPoi.location.point.geometry));


            Vector3 delta = lr.GetPosition(0) - lr.GetPosition(1);
            float rotation = (Mathf.Atan2(delta.z, delta.x) - PaAmrFunctionDirection) * 180.0f / Mathf.PI;
            transform.localRotation = Quaternion.Euler(90.0f, 0.0f, rotation);
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
                lr.SetPosition(1, Utils.Point2Vec((Point)poi.location.point.geometry));
            }
            else
            {
                lr.positionCount = 0;
            }
        }
    }

    void Start()
    {
        UpdateRenderer();
    }

    void Update()
    {

    }

    void OnDestroy()
    {
        poi.OnLocationPointUpdate -= UpdateRenderer;
    }
}
