using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Gtk;

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
            Destroy(toolObj);
            toolObj = null;
            currentTool = null;
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (e.name == "line string")
            {
                toolObj = new GameObject("lineString");
                currentTool = toolObj.AddComponent<LineString>();
                currentTool.draftMaterial = Resources.Load<Material>("material/tool linestring");
            }
            else if (e.name == "select drag")
            {
                toolObj = new GameObject("select drag");
                currentTool = toolObj.AddComponent<SelectDrag>();
                currentTool.draftMaterial = Resources.Load<Material>("material/tool select drag");
            }
            else if (e.name == "delete")
            {
                toolObj = new GameObject("delete");
                currentTool = toolObj.AddComponent<Deleter>();
            }
            else if (e.name == "save")
            {
                SaveToFile(indoorSim.indoorTiling.Serialize());
            }
            else if (e.name == "load")
            {
                Debug.Log("Load sth");
            }

            toolObj?.transform.SetParent(transform);
            if (currentTool != null)
            {
                currentTool.mapView = mapView;
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

    void SaveToFile(string content)
    {
        Debug.Log(content);
        Debug.Log(UnityEngine.Application.platform);

        if (UnityEngine.Application.platform == RuntimePlatform.LinuxPlayer || UnityEngine.Application.platform == RuntimePlatform.LinuxEditor)
        {
            // var dialog = new Gtk.FileChooserDialog("save map", null, FileChooserAction.Save,
            //                         "Cancel", ResponseType.Cancel,
            //                         "Save", ResponseType.Accept);
            // // Gtk.ResponseType response = (Gtk.ResponseType)dialog.Run();
            // // if (response == Gtk.ResponseType.Accept)
            // //     Debug.Log(dialog.Filename);
            // // else
            // //     Debug.Log(response);
            // dialog.Destroy();
        }

    }



    // Update is called once per frame
    void Update()
    {

    }
}
