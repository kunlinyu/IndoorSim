using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SimDataController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public IndoorMapView mapView;
    public SimulationView simView;
    GameObject toolObj;
    ITool currentTool;

    ITool keyBoardDeleter;

    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;
    private Thread serializationThread;

    private List<IExporter> exporters = new List<IExporter>();


    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }

    void SerializeAndPublish()
    {
        UIEvent e = new UIEvent();

        e.type = UIEventType.Hierarchy;
        e.name = "indoordata";
        e.message = indoorSimData.indoorFeatures.Serialize(false);  // slow

        eventDispatcher?.Raise(this, e);
    }

    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);

        indoorSimData.OnAssetListUpdated += (assets) =>
        {
            var e = new UIEvent();
            e.type = UIEventType.Asset;
            e.name = "list";
            e.message = JsonConvert.SerializeObject(assets, new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.Indented });

            eventDispatcher?.Raise(this, e);
        };
        indoorSimData.OnIndoorFeatureUpdated += (indoorFeatues) =>
        {
            SerializeAndPublish();
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
        indoorSimData.OnGridMapListUpdated += (gridMaps) =>
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

        keyBoardDeleter = transform.Find("KeyboardDeleter").GetComponent<KeyBoardDeleter>();
        keyBoardDeleter.mapView = mapView;
        keyBoardDeleter.simView = simView;
        keyBoardDeleter.IndoorSimData = indoorSimData;

        exporters.Add(new LocationsYamlExporter());
        exporters.Add(new BinLocationsJsonExporter());
        exporters.Add(new BinLocationCsvExporter());
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButton && e.message == "leave")
        {
            if (toolObj != null)
            {
                Debug.Log("Disable tool " + toolObj.name);
                Destroy(toolObj);
                toolObj = null;
                currentTool = null;
                MousePickController.pickType = CurrentPickType.All;
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        else if (e.type == UIEventType.ToolButton && e.message == "enter")
        {
            if (toolObj != null)
            {
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
            else if (e.name == "paamrpoi")
            {
                Debug.Log("Waiting for POIType");
            }
            else if (e.name == "apply asset")
            {
                toolObj = new GameObject("apply asset");
                toolObj.transform.SetParent(this.transform);
                AssetApplier assetApplier = toolObj.AddComponent<AssetApplier>();
                assetApplier.assetId = Int32.Parse(e.message);
                currentTool = assetApplier;
                Debug.Log("Switch to asset applier");
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
                else
                {
                    Debug.LogWarning("You should create or select one simulation too create robot in it.");
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
                else
                {
                    Debug.LogWarning("You should create or select one simulation too create robot in it.");
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


            if (currentTool != null)
            {
                currentTool.mapView = mapView;
                currentTool.simView = simView;
                currentTool.IndoorSimData = indoorSimData;
            }
        }
        else if (e.type == UIEventType.ToolButton && e.message == "trigger")
        {
            if (e.name == "save")
            {
                string content = indoorSimData.Serialize(Application.version, true);
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
        }
        else if (e.type == UIEventType.ToolButton && e.name == "paamrpoi")
        {

            Debug.Log(e.message);
            POIType poiType = POIType.FromJson(e.message);

            toolObj = Instantiate(Resources.Load<GameObject>("ToolObj/PaAmrPOIMarker"), this.transform);
            toolObj.name = "PaAmrPOIMarker";
            PaAmrPOIMarker poiMarker = toolObj.GetComponent<PaAmrPOIMarker>();
            poiMarker.mapView = mapView;
            poiMarker.IndoorSimData = indoorSimData;
            poiMarker.Init(poiType);
            currentTool = poiMarker;
            Debug.Log("Switch to tool PaAmrPOIMarker");
        }
        else if (e.type == UIEventType.Resources && e.name == "load")
        {
            indoorSimData.DeserializeInPlace(e.message, false);
        }
        else if (e.type == UIEventType.Resources && e.name == "exportInfo")
        {
            Debug.Log(e.message);
            var jsonData = JObject.Parse(e.message);

            string exportFileName = jsonData["file"].Value<string>();

            IExporter exporter = exporters.Find(exporter => exporter.name == exportFileName);

            if (exporter == null) throw new ArgumentException("unknow exporter: " + exportFileName);

            exporter.Load(indoorSimData);
            if (exporter.Translate(jsonData["layer"].Value<string>()))
            {
                string result = exporter.Export(Application.version, jsonData["include"].Value<bool>());
                eventDispatcher.Raise(this, new UIEvent() { name = "export", message = e.message, data = result, type = UIEventType.Resources });
            }
            else
            {
                Debug.LogWarning(exporter.name + " data model translate failed");
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
        else if (e.type == UIEventType.Resources)
        {
            if (e.name == "gridmap")
            {
                Debug.Log("controller get gridmap");
                GridMap gridMap = new GridMap();

                var jsonData = JObject.Parse(e.message);
                gridMap.id = jsonData["id"].Value<string>();
                gridMap.resolution = jsonData["resolution"].Value<double>();
                gridMap.zippedBase64Image = jsonData["zipBase64Image"].Value<string>();
                gridMap.localOrigin.x = jsonData["origin_x"].Value<double>();
                gridMap.localOrigin.y = jsonData["origin_y"].Value<double>();
                gridMap.localOrigin.theta = jsonData["origin_theta"].Value<double>();

                if (jsonData["format"].Value<string>() == "PGM")
                    gridMap.format = GridMapImageFormat.PGM;
                else if (jsonData["format"].Value<string>() == "PNG")
                    gridMap.format = GridMapImageFormat.PNG;
                else
                    Debug.LogError("unrecognize file format: " + jsonData["format"].Value<string>());

                while (indoorSimData.gridMaps.Count > 0)  // TODO: we need to support multiple grid map
                    indoorSimData.RemoveGridMap(0);
                indoorSimData.AddGridMap(gridMap);
            }
        }
    }
}
