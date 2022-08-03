using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMapView : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public Dictionary<GridMap, GameObject> gridMap2Obj = new Dictionary<GridMap, GameObject>();
    void Start()
    {
        indoorSimData.OnGridMapCreated += (gridMap) =>
        {
            var obj = Instantiate(Resources.Load<GameObject>("BasicShape/GridMap"), this.transform);
            obj.name = gridMap.id;
            obj.layer = gameObject.layer;
            obj.GetComponent<GridMapController>().GridMap = gridMap;
            gridMap2Obj[gridMap] = obj;
        };

        indoorSimData.OnGridMapRemoved += (gridMap) =>
        {
            Destroy(gridMap2Obj[gridMap]);
            gridMap2Obj.Remove(gridMap);
        };


    }

    void Update()
    {

    }
}
