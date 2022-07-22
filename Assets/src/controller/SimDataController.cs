using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SFB;

public class SimDataController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public IndoorMapView mapView;
    public SimulationView simView;
    GameObject toolObj;
    ITool currentTool;

    public UIEventDispatcher eventDispatcher;

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
        indoorSimData.OnGridMapUpdated += (gridMaps) =>
        {
            var e = new UIEvent();
            e.type = UIEventType.Hierarchy;
            e.name = "gridmap";
            e.message = String.Join('\n', gridMaps.Select(gridMaps => gridMaps.id));
            eventDispatcher?.Raise(this, e);
        };

        indoorSimData.OnAgentCreate += (agent) =>
        {

        };
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButton)
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
                toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/LineStringEditor"), this.transform);
                toolObj.name = "lineString";
                currentTool = toolObj.GetComponent<LineStringEditor>();
                Debug.Log("Switch to tool lineString");
            }
            else if (e.name == "select drag")
            {
                toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/SelectDrag"), this.transform);
                toolObj.name = "select drag";
                currentTool = toolObj.GetComponent<SelectDrag>();
                Debug.Log("Switch to tool select drag");
            }
            else if (e.name == "save asset")
            {
                toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/AssetSaver"), this.transform);
                toolObj.name = "asset saver";
                toolObj.transform.SetParent(this.transform);
                var assetSaver = toolObj.AddComponent<AssetSaver>();
                assetSaver.mapView = mapView;
                assetSaver.simView = simView;
                assetSaver.IndoorSimData = indoorSimData;
                assetSaver.ExtractSelected2Asset();
                Debug.Log("Use tool AssetSaver");
                toolObj = null;
                currentTool = null;
            }
            else if (e.name == "delete")
            {
                toolObj = new GameObject("delete");
                toolObj.transform.SetParent(this.transform);
                currentTool = toolObj.AddComponent<Deleter>();
                Debug.Log("Switch to tool deleter");
            }
            else if (e.name == "navigable")
            {
                toolObj = new GameObject("navigable");
                toolObj.transform.SetParent(this.transform);
                currentTool = toolObj.AddComponent<NavigableEditor>();
                Debug.Log("Switch to tool navigable");
            }
            else if (e.name == "direction")
            {
                toolObj = new GameObject("boundary direction");
                toolObj.transform.SetParent(this.transform);
                currentTool = toolObj.AddComponent<BoundaryDirectionEditor>();
                Debug.Log("Switch to tool boundary direction");
            }
            else if (e.name == "rline")
            {
                toolObj = new GameObject("rline");
                toolObj.transform.SetParent(this.transform);
                currentTool = toolObj.AddComponent<RLineEditor>();
                Debug.Log("Switch to tool rline");
            }
            else if (e.name == "shelves")
            {
                toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesEditor"), this.transform);
                toolObj.name = "shelves";
                currentTool = toolObj.GetComponent<ShelvesEditor>();
                Debug.Log("Switch to tool shelves");
            }
            else if (e.name == "shelves2")
            {
                toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesEditor2"), this.transform);
                toolObj.name = "shelves2";
                currentTool = toolObj.GetComponent<ShelvesEditor2>();
                Debug.Log("Switch to tool shelves2");
            }
            else if (e.name == "id")
            {
                toolObj = new GameObject("id");
                toolObj.transform.SetParent(this.transform);
                IDEditor idEditor = toolObj.AddComponent<IDEditor>();
                idEditor.PopContainerIdPanel = (x, y, containerId, childrenId) =>
                {
                    eventDispatcher.Raise(idEditor, new UIEvent()
                    {
                        name = "id panel",
                        message = $"{{\"predicate\":\"popup\", \"x\":{x}, \"y\":{y}, \"containerId\":\"{containerId}\", \"childrenId\":\"{childrenId}\"}}",
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
            else if (e.name == "apply asset")
            {
                // if (oldToolName != "apply asset")
                {
                    toolObj = new GameObject("apply asset");
                    toolObj.transform.SetParent(this.transform);
                    AssetApplier assetApplier = toolObj.AddComponent<AssetApplier>();
                    assetApplier.assetId = Int32.Parse(e.message);
                    currentTool = assetApplier;
                    Debug.Log("Switch to asset applier");
                }
            }
            else if (e.name == "remove asset")
            {

            }
            else if (e.name == "capsule")
            {
                if (indoorSimData.currentSimData != null)
                {
                    toolObj = new GameObject("capsule");
                    toolObj.transform.SetParent(this.transform);
                    var agentEditor = toolObj.AddComponent<AgentEditor>();
                    agentEditor.agentType = "capsule";
                    currentTool = agentEditor;

                    Debug.Log("Switch to tool capsule");
                }
            }
            else if (e.name == "bronto")
            {
                if (indoorSimData.currentSimData != null)
                {
                    toolObj = new GameObject("bronto");
                    toolObj.transform.SetParent(this.transform);
                    var agentEditor = toolObj.AddComponent<AgentEditor>();
                    agentEditor.agentType = "bronto";
                    currentTool = agentEditor;

                    Debug.Log("Switch to tool bronto");
                }
            }
            else if (e.name == "astar")
            {
                GameObject prefab = Resources.Load<GameObject>("ToolObj/AStarTool");
                toolObj = Instantiate(prefab, transform);
                var astarTool = toolObj.AddComponent<AStarTool>();
                currentTool = astarTool;

                Debug.Log("Switch to tool astar");
            }
            else if (e.name == "save")
            {
                string content = indoorSimData.Serialize(true);
                eventDispatcher.Raise(this, new UIEvent() { name = "save", message = content, type = UIEventType.Resources });
            }
            else if (e.name == "redo")
            {
                indoorSimData.Redo();
            }
            else if (e.name == "undo")
            {
                indoorSimData.Undo();
            }

            if (currentTool != null)
            {
                currentTool.mapView = mapView;
                currentTool.simView = simView;
                currentTool.IndoorSimData = indoorSimData;
            }
        }
        else if (e.type == UIEventType.Resources && e.name == "load")
        {
            indoorSimData.DeserializeInPlace(e.message, false);
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
        else if (e.type == UIEventType.IndoorSimData)
        {
            if (e.name == "container id")
            {
                if (toolObj.name == "id")
                {
                    var idEditor = toolObj.GetComponent<IDEditor>();
                    var jsonData = JObject.Parse(e.message);
                    idEditor.SetContainerId(jsonData["containerId"].Value<string>(), jsonData["childrenId"].Value<string>());
                }
                else
                {
                    Debug.LogWarning("receive container id but current tool is not \"idEditor\" but: " + toolObj.name);
                }

            }
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
