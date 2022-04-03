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
        if (e.type == UIEventType.ButtonClick)
        {
            if (e.name == "line string")
            {
                Destroy(toolObj);
                toolObj = new GameObject("linestring");
                toolObj.transform.SetParent(transform);
                currentTool = toolObj.AddComponent<LineString>();
                currentTool.IndoorSim = indoorSim;
            }
            else if (e.name == "select drag")
            {
                Destroy(toolObj);
                currentTool = null;
            }
        }
        else if (e.type == UIEventType.EnterUIPanel)
        {
            if (currentTool != null)
                if (e.name == "enter")
                    currentTool.MouseOnUI = true;
                else if (e.name == "leave")
                    currentTool.MouseOnUI = false;
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
