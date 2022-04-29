using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using SFB;

public class SimulationController : MonoBehaviour
{
    public IndoorSim indoorSim;
    public MapView mapView;
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
            string oldToolName = "";
            if (toolObj != null)
            {
                oldToolName = toolObj.name;
                Debug.Log("Disable tool " + toolObj.name);
                Destroy(toolObj);
                toolObj = null;
                currentTool = null;
                MousePickController.pickType = CurrentPickType.All;
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            if (e.name == "line string")
            {
                if (oldToolName != "lineString")
                {
                    toolObj = new GameObject("lineString");
                    currentTool = toolObj.AddComponent<LineString>();
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool linestring");
                    Debug.Log("Switch to tool lineString");
                }
            }
            else if (e.name == "select drag")
            {
                if (oldToolName != "select drag")
                {
                    toolObj = new GameObject("select drag");
                    currentTool = toolObj.AddComponent<SelectDrag>();
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool select drag");
                    Debug.Log("Switch to tool select drag");
                }
            }
            else if (e.name == "delete")
            {
                if (oldToolName != "delete")
                {
                    toolObj = new GameObject("delete");
                    currentTool = toolObj.AddComponent<Deleter>();
                    Debug.Log("Switch to tool deleter");
                }
            }
            else if (e.name == "navigable")
            {
                if (oldToolName != "navigable")
                {
                    toolObj = new GameObject("navigable");
                    currentTool = toolObj.AddComponent<NavigableEditor>();
                    Debug.Log("Switch to tool navigable");
                }
            }
            else if (e.name == "direction")
            {
                if (oldToolName != "direction")
                {
                    toolObj = new GameObject("boundary direction");
                    currentTool = toolObj.AddComponent<BoundaryDirectionEditor>();
                    Debug.Log("Switch to tool boundary direction");
                }
            }
            else if (e.name == "rline")
            {
                if (oldToolName != "rline")
                {
                    toolObj = new GameObject("rline");
                    currentTool = toolObj.AddComponent<RLineEditor>();
                    Debug.Log("Switch to tool rline");
                }
            }
            else if (e.name == "save")
            {
                SaveToFile(indoorSim.indoorTiling.Serialize());
            }
            else if (e.name == "load")
            {
                indoorSim.indoorTiling.DeserializeInPlace(LoadFromFile(), true);
            }
            else if (e.name == "redo")
            {
                indoorSim.indoorTiling.Redo();
            }
            else if (e.name == "undo")
            {
                indoorSim.indoorTiling.Undo();
            }



            toolObj?.transform.SetParent(transform);
            if (currentTool != null)
            {
                currentTool.mapView = mapView;
                currentTool.IndoorSim = indoorSim;
            }
        }
        else if (e.type == UIEventType.EnterLeaveUIPanel)
        {
            if (currentTool != null)
                if (e.message == "enter")
                    currentTool.MouseOnUI = true;
                else if (e.message == "leave")
                    currentTool.MouseOnUI = false;
        }
    }

    string LoadFromFile()
    {
        if (UnityEngine.Application.platform == RuntimePlatform.LinuxPlayer || UnityEngine.Application.platform == RuntimePlatform.LinuxEditor)
        {
            string[] path = StandaloneFileBrowser.OpenFilePanel("Load File", "Assets/src/Tests/", "json", false);
            return File.ReadAllText(path[0]);
        }
        return "";
    }

    void SaveToFile(string content)
    {
        if (UnityEngine.Application.platform == RuntimePlatform.LinuxPlayer || UnityEngine.Application.platform == RuntimePlatform.LinuxEditor)
        {
            string path = StandaloneFileBrowser.SaveFilePanel("Save File", "Assets/src/Tests/", "unnamed_map.indoor.json", "indoor.json");
            Debug.Log("save file to: " + path);
            File.WriteAllText(path, content);
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
