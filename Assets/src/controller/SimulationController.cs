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
            Destroy(toolObj);
            currentTool = null;
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (e.name == "line string")
            {
                toolObj = new GameObject("linestring");
                toolObj.transform.SetParent(transform);
                var lineString = toolObj.AddComponent<LineString>();
                lineString.draftMaterial = Resources.Load<Material>("material/tool linestring");
                currentTool = lineString;
                currentTool.IndoorSim = indoorSim;
            }
            else if (e.name == "select drag")
            {
                toolObj = new GameObject("select drag");
                toolObj.transform.SetParent(transform);
                var selectDrag = toolObj.AddComponent<SelectDrag>();
                selectDrag.draftMaterial = Resources.Load<Material>("material/tool select drag");
                currentTool = selectDrag;
                currentTool.IndoorSim = indoorSim;
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
