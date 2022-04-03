using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public IndoorSim indoorSim;
    GameObject toolObj;
    ITool currentTool;

    public UIEventDispatcher eventDispatcher;



    void Start()
    {


        eventDispatcher.eventListener += EventListener;
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.from == "line string")
        {
            Destroy(toolObj);
            toolObj = new GameObject("linestring");
            toolObj.transform.SetParent(transform);
            currentTool = toolObj.AddComponent<LineString>();
            currentTool.IndoorSim = indoorSim;
        }
        else if (e.from == "select drag")
        {
            Destroy(toolObj);
            currentTool = null;
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
