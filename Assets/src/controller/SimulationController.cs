using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public IndoorSim indoorSim;
    ITool currentTool;
    GameObject toolObj;
    // Start is called before the first frame update
    void Start()
    {
        toolObj = new GameObject("linestring");
        toolObj.transform.SetParent(transform);
        currentTool = toolObj.AddComponent<LineString>();
        currentTool.IndoorSim = indoorSim;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
