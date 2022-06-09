using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using SFB;

public class SimDataController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public MapView mapView;
    GameObject toolObj;
    ITool currentTool;

    public UIEventDispatcher eventDispatcher;

    public Camera screenshotCamera;

    void Start()
    {
        eventDispatcher.eventListener += EventListener;
        indoorSimData.OnAssetUpdated += (assets) =>
        {
            var e = new UIEvent();
            e.type = UIEventType.Asset;
            e.name = "list";
            e.message = JsonConvert.SerializeObject(assets, new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.Indented });

            eventDispatcher?.Raise(this, e);
        };
        indoorSimData.OnIndoorDataUpdated += (indoorData) =>
        {
            var e = new UIEvent();
            e.type = UIEventType.Hierarchy;
            e.name = "indoordata";
            e.message = indoorData.Serialize(false);

            eventDispatcher?.Raise(this, e);
        };
        indoorSimData.OnSimulationListUpdated += (sims) =>
        {
            List<SimData> noHistorySims = new List<SimData>();
            sims.ForEach(sim => noHistorySims.Add(new SimData(sim.name) { active = sim.active, agents = sim.agents, tasks = sim.tasks, history = null }));

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                Formatting = Newtonsoft.Json.Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
            };

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
            StringBuilder sb = new StringBuilder(256);
            StringWriter sw = new StringWriter(sb);
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = jsonSerializer.Formatting;
                jsonWriter.IndentChar = '\t';
                jsonWriter.Indentation = 1;
                jsonSerializer.Serialize(jsonWriter, noHistorySims);
            }
            string simsJson = sw.ToString();


            var e = new UIEvent();
            e.type = UIEventType.Hierarchy;
            e.name = "simulation";
            e.message = simsJson;

            eventDispatcher?.Raise(this, e);
        };

        indoorSimData.OnAgentCreate += (agent) =>
        {

        };
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButtonClick)
        {
            string oldToolName = "";
            if (toolObj != null && e.name != "save asset")  // TODO: fuck
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
                    currentTool = toolObj.AddComponent<LineStringEditor>();
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool linestring");
                    Debug.Log("Switch to tool lineString");
                }
            }
            else if (e.name == "select drag")
            {
                if (oldToolName != "select drag")
                {
                    toolObj = new GameObject("select drag");
                    SelectDrag selectDrag = toolObj.AddComponent<SelectDrag>();
                    selectDrag.draftMaterial = Resources.Load<Material>("Materials/tool select drag");
                    selectDrag.screenshotCamera = screenshotCamera;

                    currentTool = selectDrag;
                    Debug.Log("Switch to tool select drag");
                }
            }
            else if (e.name == "save asset")
            {
                if (toolObj != null && toolObj.name == "select drag")
                {
                    Debug.Log("going to save asset");
                    ((SelectDrag)currentTool).ExtractSelected2Asset();
                }
                else
                {
                    Debug.LogWarning("use select & drag tool to select something before save asset");
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
            else if (e.name == "shelves")
            {
                if (oldToolName != "shelves")
                {
                    toolObj = new GameObject("shelves");
                    currentTool = toolObj.AddComponent<Shelves>();
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool linestring");
                    Debug.Log("Switch to tool shelves");
                }
            }
            else if (e.name == "shelves2")
            {
                if (oldToolName != "shelves2")
                {
                    toolObj = new GameObject("shelves2");
                    currentTool = toolObj.AddComponent<Shelves2>();
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool linestring");
                    Debug.Log("Switch to tool shelves2");
                }
            }
            else if (e.name == "id")
            {
                if (oldToolName != "id")
                {
                    toolObj = new GameObject("id");
                    IDEditor idEditor = toolObj.AddComponent<IDEditor>();
                    idEditor.PopContainerIdPanel = (x, y) =>
                    {
                        eventDispatcher.Raise(idEditor, new UIEvent()
                        {
                            name = "id panel",
                            message = $"{{\"predicate\":\"popup\", \"x\":{x}, \"y\":{y}}}",
                            type = UIEventType.PopUp
                        });
                    };
                    idEditor.HideContainerIdPanel = () =>
                    {
                        eventDispatcher.Raise(idEditor, new UIEvent() { name = "id panel", message = "{\"predicate\":\"hide\"}", type = UIEventType.PopUp });
                    };
                    currentTool = idEditor;
                    Debug.Log("Switch to tool id editor");
                }
            }
            else if (e.name == "apply asset")
            {
                // if (oldToolName != "apply asset")
                {
                    toolObj = new GameObject("apply asset");
                    AssetApplier assetApplier = toolObj.AddComponent<AssetApplier>();
                    assetApplier.assetId = Int32.Parse(e.message);
                    currentTool = assetApplier;
                    currentTool.draftMaterial = Resources.Load<Material>("Materials/tool linestring");
                    Debug.Log("Switch to asset applier");
                }
            }
            else if (e.name == "remove asset")
            {

            }
            else if (e.name == "capsule")
            {
                if (oldToolName != "capsule")
                {
                    toolObj = new GameObject("capsule");
                    var agentEditor = toolObj.AddComponent<AgentEditor>();
                    agentEditor.agentType = "capsule";
                    currentTool = agentEditor;

                    Debug.Log("Switch to tool capsule");
                }
            }
            else if (e.name == "boxcapsule")
            {
                if (oldToolName != "boxcapsule")
                {
                    toolObj = new GameObject("boxcapsule");
                    var agentEditor = toolObj.AddComponent<AgentEditor>();
                    agentEditor.agentType = "boxcapsule";
                    currentTool = agentEditor;

                    Debug.Log("Switch to tool boxcapsule");
                }
            }
            else if (e.name == "bronto")
            {
                if (oldToolName != "bronto")
                {
                    toolObj = new GameObject("bronto");
                    var agentEditor = toolObj.AddComponent<AgentEditor>();
                    agentEditor.agentType = "bronto";
                    currentTool = agentEditor;

                    Debug.Log("Switch to tool bronto");
                }
            }
            else if (e.name == "save")
            {
                SaveToFile(indoorSimData.Serialize(true));
            }
            else if (e.name == "load")
            {
                indoorSimData.DeserializeInPlace(LoadFromFile(), false);
            }
            else if (e.name == "redo")
            {
                indoorSimData.Redo();
            }
            else if (e.name == "undo")
            {
                indoorSimData.Undo();
            }



            toolObj?.transform.SetParent(transform);
            if (currentTool != null)
            {
                currentTool.mapView = mapView;
                currentTool.IndoorSimData = indoorSimData;
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
        else if (e.type == UIEventType.Simulation)
        {
        }
        else if (e.type == UIEventType.Hierarchy)
        {
            if (e.name == "add simulation")
                indoorSimData.AddSimulation(e.message);
            if (e.name == "select simulation")
                indoorSimData.SelectSimulation(e.message);
            if (e.name == "select indoor map")
                indoorSimData.SelectMap();
            // if (e.name == "select grid map")
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
