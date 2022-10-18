using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using NetTopologySuite.Geometries;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

using System.Runtime.Serialization;

using UnityEngine;

#nullable enable

public class IndoorSimData
{
    [JsonPropertyAttribute] public string softwareVersion = null;
    [JsonPropertyAttribute] public string schemaHash = null;
    [JsonIgnore]
    public static Dictionary<string, string> schemaHashHistory = new Dictionary<string, string>()
    {
        {"0.7.0", "3AC0BED35318C7E853CAFCBCA198537F"},
        {"0.8.0", "3AC0BED35318C7E853CAFCBCA198537F"},
        {"0.8.1", "3AC0BED35318C7E853CAFCBCA198537F"},
        {"0.8.2", "3AC0BED35318C7E853CAFCBCA198537F"},
        {"0.8.3", "3AC0BED35318C7E853CAFCBCA198537F"}
    };
    [JsonPropertyAttribute] public List<GridMap> gridMaps = new List<GridMap>();

    [JsonPropertyAttribute] public IndoorFeatures indoorFeatures = null;
    [JsonIgnore] public List<IndoorTiling> indoorTilings = new List<IndoorTiling>();
    [JsonIgnore] public IndoorTiling activeTiling = null;
    [JsonPropertyAttribute] public InstructionHistory<ReducedInstruction> history = new InstructionHistory<ReducedInstruction>();

    [JsonPropertyAttribute] private List<SimData> simDataList = new List<SimData>();
    [JsonPropertyAttribute] public Dictionary<string, AgentTypeMeta> agentMetaList = new Dictionary<string, AgentTypeMeta>();
    [JsonIgnore] public SimData? currentSimData { get; private set; }
    [JsonIgnore] public bool simulating = false;

    [JsonIgnore] public InstructionHistory<ReducedInstruction> activeHistory;
    [JsonIgnore] private InstructionInterpreter instructionInterpreter = new InstructionInterpreter();
    [JsonIgnore] private InstructionInterpreter activeInstructionInterpreter;

    [JsonPropertyAttribute] public List<Asset> assets = new List<Asset>();
    [JsonPropertyAttribute] public string digestCache = "";

    [JsonIgnore] public Action<List<Asset>> OnAssetListUpdated = (a) => { };
    [JsonIgnore] public Action<List<GridMap>> OnGridMapListUpdated = (gridMaps) => { };
    [JsonIgnore] public Action<GridMap> OnGridMapCreated = (gridmap) => { };
    [JsonIgnore] public Action<GridMap> OnGridMapRemoved = (gridmap) => { };
    [JsonIgnore] public Action<IndoorFeatures> OnIndoorFeatureUpdated = (indoorFeatues) => { };
    [JsonIgnore] public Action<List<SimData>> OnSimulationListUpdated = (sims) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentCreate = (a) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentRemoved = (a) => { };

    [JsonIgnore] private bool inSession = false;

    public IndoorSimData()
    {
        RegisterInstructionExecutor();
    }

    static public JSchema JSchema()
        => new JSchemaGenerator() { ContractResolver = new IgnoreGeometryCoorContractResolver() }.Generate(typeof(IndoorSimData));

    static public string JSchemaStableString()
    {
        var str = JSchema().ToString();
        str = str.Replace("\r\n", "\n");  // for windows
        return str;
    }

    static public string JSchemaHash()
        => Hash.GetHashString(JSchemaStableString());

    [OnDeserialized]
    private void OnSerializedMethod(StreamingContext context)
    {
        indoorTilings.Clear();
        indoorFeatures.layers.ForEach(layer =>
        {
            var indoorTiling = new IndoorTiling(layer, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));
            indoorTiling.AssignIndoorData(layer);
            indoorTilings.Add(indoorTiling);
        });
        activeTiling = indoorTilings[0];
        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;
    }

    public IndoorSimData(bool nonDeserialization)
    {
        indoorFeatures = new IndoorFeatures("floor one");
        indoorTilings.Clear();
        indoorFeatures.layers.ForEach(layer =>
        {
            var indoorTiling = new IndoorTiling(layer, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));
            indoorTiling.AssignIndoorData(layer);
            indoorTilings.Add(indoorTiling);
        });
        activeTiling = indoorTilings[0];

        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;

        RegisterInstructionExecutor();
    }

    public string Serialize(string softwareVersion, bool indent = false)
    {
        this.softwareVersion = softwareVersion;
        this.schemaHash = JSchemaHash();

        Debug.Log("schemaHash: " + this.schemaHash);

        digestCache = indoorFeatures.CalcDigest();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            Formatting = indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
            ContractResolver = new ShouldSerializeContractResolver(),
        };

        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
        StringBuilder sb = new StringBuilder(256);
        StringWriter sw = new StringWriter(sb);
        using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
        {
            jsonWriter.Formatting = jsonSerializer.Formatting;
            jsonWriter.IndentChar = '\t';
            jsonWriter.Indentation = 1;
            jsonSerializer.Serialize(jsonWriter, this, null);
        }

        return sw.ToString();  // return JsonConvert.SerializeObject(this);
    }

    public bool DeserializeInPlace(string json, bool historyOnly = false)
    {
        assets.Clear();
        history.Clear();
        IndoorSimData? indoorSimData = Deserialize(json, historyOnly);

        if (indoorSimData == null) return false;

        if (indoorSimData.schemaHash == null)
            Debug.LogWarning("schemaHash == null. This file is not official file format. Resave it to generate an official file format.");
        else if (indoorSimData.schemaHash.Length == 0)
            Debug.LogWarning("schemaHash is empty. This file is not official file format. Resave it to generate an official file format.");
        else
        {
            var expectedSchemahash = JSchemaHash();
            if (indoorSimData.schemaHash != expectedSchemahash)
            {
                Debug.Log("schema hash history:");
                foreach (var entry in schemaHashHistory)
                    Debug.Log(entry.Key + ": " + entry.Value);
                Debug.Log("current version is " + Application.version);
                Debug.Log("current schemaHash is " + expectedSchemahash);
                throw new ArgumentException($"schemaHash is incorrect: {indoorSimData.schemaHash}");
            }
        }


        indoorFeatures.layers.ForEach(layer =>
        {
            layer.cellVertexMember.ForEach(v => layer.OnVertexRemoved?.Invoke(v));
            layer.cellBoundaryMember.ForEach(b => layer.OnBoundaryRemoved?.Invoke(b));
            layer.cellSpaceMember.ForEach(s => layer.OnSpaceRemoved?.Invoke(s));
            layer.rLineGroupMember.ForEach(r => layer.OnRLinesRemoved?.Invoke(r));
            layer.poiMember.ForEach(p => layer.OnPOIRemoved?.Invoke(p));
            indoorFeatures.OnLayerRemoved?.Invoke(layer);
            indoorFeatures.activeLayer = null;
        });

        indoorFeatures.layers.Clear();
        indoorFeatures.layerConnections?.Clear();

        indoorFeatures.layers = indoorSimData.indoorFeatures.layers;
        indoorFeatures.layerConnections = indoorSimData.indoorFeatures.layerConnections;
        indoorFeatures.layers.ForEach(layer => indoorFeatures.OnLayerCreated?.Invoke(layer));
        if (indoorFeatures.layers.Count > 0)
            indoorFeatures.activeLayer = indoorFeatures.layers[0];

        indoorTilings.Clear();
        indoorFeatures.layers.ForEach(layer =>
        {
            layer.cellVertexMember.ForEach(v => layer.OnVertexCreated?.Invoke(v));
            layer.cellBoundaryMember.ForEach(b => layer.OnBoundaryCreated?.Invoke(b));
            layer.cellSpaceMember.ForEach(s => layer.OnSpaceCreated?.Invoke(s));
            layer.rLineGroupMember.ForEach(r => layer.OnRLinesCreated?.Invoke(r));
            layer.poiMember.ForEach(p => layer.OnPOICreated?.Invoke(p));

            var indoorTiling = new IndoorTiling(layer, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));
            indoorTiling.AssignIndoorData(layer);
            indoorTilings.Add(indoorTiling);
        });
        if (indoorTilings.Count > 0)
            activeTiling = indoorTilings[0];
        else
            activeTiling = null;

        simDataList = indoorSimData.simDataList;
        simDataList.ForEach(sim => sim.active = false);
        assets = indoorSimData.assets;
        history = indoorSimData.history;
        currentSimData = null;
        activeHistory = history;
        OnAssetListUpdated?.Invoke(assets);
        OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        OnSimulationListUpdated?.Invoke(simDataList);

        gridMaps.ForEach(gridmap => OnGridMapRemoved?.Invoke(gridmap));
        gridMaps = indoorSimData.gridMaps;
        gridMaps.ForEach(gridmap => OnGridMapCreated?.Invoke(gridmap));

        return true;
    }

    public static IndoorSimData? Deserialize(string json, bool historyOnly = false)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
            ContractResolver = new ShouldSerializeContractResolver(),
        };
        IndoorSimData? indoorSimData = JsonConvert.DeserializeObject<IndoorSimData>(json, settings);
        if (indoorSimData == null) return null;

        if (historyOnly)
        {
            indoorSimData.indoorFeatures = new IndoorFeatures("floor 1");
            indoorSimData.simDataList = new List<SimData>();
            indoorSimData.history.Uuundo();
        }
        else
        {
            if (indoorSimData.indoorFeatures != null)
                indoorSimData.indoorFeatures.layers.ForEach(layer => layer.UpdateIndices());
        }
        indoorSimData.indoorTilings.Clear();
        indoorSimData.indoorFeatures!.layers.ForEach(layer =>
        {
            var indoorTiling = new IndoorTiling(layer, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));
            indoorTiling.AssignIndoorData(layer);
            indoorSimData.indoorTilings.Add(indoorTiling);
        });
        indoorSimData.activeTiling = indoorSimData.indoorTilings[0];
        return indoorSimData;
    }

    public Asset ExtractAsset(string name,
                              List<CellVertex> vertices,
                              List<CellBoundary> boundaries,
                              List<CellSpace> spaces,
                              Func<float, float, float, float, string>? captureThumbnailBase64)
    {
        if (vertices.Any(v => !indoorFeatures.Contains(v))) throw new ArgumentException("can not find some vertex");
        if (boundaries.Any(b => !indoorFeatures.Contains(b))) throw new ArgumentException("can not find some boundary");
        if (spaces.Any(s => !indoorFeatures.Contains(s))) throw new ArgumentException("can not find some space");

        ThematicLayer newLayer = new ThematicLayer("asset");
        newLayer.cellVertexMember.AddRange(vertices);
        newLayer.cellBoundaryMember.AddRange(boundaries);
        newLayer.cellSpaceMember.AddRange(spaces);
        newLayer.rLineGroupMember.AddRange(spaces.Select(s => s.rLines!));
        string json = newLayer.Serialize(false);

        GeometryCollection gc = new GeometryFactory().CreateGeometryCollection(boundaries.Select(b => b.geom).ToArray());
        Envelope evl = gc.EnvelopeInternal;
        Point centroid = gc.Centroid;

        Asset asset = new Asset()
        {
            name = name,
            thumbnailBase64 = captureThumbnailBase64?.Invoke((float)evl.MaxX, (float)evl.MinX, (float)evl.MaxY, (float)evl.MinY),
            dateTime = DateTime.Now,
            verticesCount = vertices.Count,
            boundariesCount = boundaries.Count,
            spacesCount = spaces.Count,
            json = json,
            centerX = centroid.X,
            centerY = centroid.Y,
        };
        assets.Add(asset);
        OnAssetListUpdated?.Invoke(assets);

        return asset;
    }

    public void AddAsset(Asset asset)
    {
        ThematicLayer? layer = ThematicLayer.Deserialize(asset.json);
        if (layer == null) throw new ArgumentException("can not deserialize the asset");
        assets.Add(asset);
        OnAssetListUpdated?.Invoke(assets);
    }

    public void RemoveAsset(Asset asset)
    {
        if (!assets.Contains(asset)) throw new ArgumentException("can not find the asset: " + asset.name);
        assets.Remove(asset);
        OnAssetListUpdated?.Invoke(assets);
    }

    public void ApplyAsset(Asset asset, Coordinate center, float rotation)
    {
        if (!assets.Contains(asset)) throw new ArgumentException("can not find the asset");
        ThematicLayer? tempIndoorData = ThematicLayer.Deserialize(asset.json);
        if (tempIndoorData == null) throw new Exception("Oops! can not deserialize the asset");

        history.SessionStart();

        // layer.vertexPool.ForEach(v => AddVertexInternal(v));
        // layer.boundaryPool.ForEach(b => AddBoundaryInternal(b));
        // layer.spacePool.ForEach(s => AddSpaceInternal(s));

        // TODO(debt): apply rLine in asset

        history.SessionCommit();
    }

    private void RegisterInstructionExecutor()
    {
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Vertices, (ins) =>
        {
            List<CellVertex> vertices = new List<CellVertex>();
            foreach (var coor in ins.oldParam.coors())
            {
                CellVertex? vertex = indoorFeatures.FindVertexCoor(coor);
                if (vertex != null)
                    vertices.Add(vertex);
                else
                    throw new ArgumentException("one of vertex can not found: " + coor);
            }
            activeTiling.UpdateVertices(vertices, ins.newParam.coors());
        });

        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.Boundary, (ins) =>
        {
            Coordinate startCoor = ins.newParam.lineString().StartPoint.Coordinate;
            CellVertex? start = indoorFeatures.FindVertexCoor(startCoor);
            Coordinate endCoor = ins.newParam.lineString().EndPoint.Coordinate;
            CellVertex? end = indoorFeatures.FindVertexCoor(endCoor);

            CellBoundary? boundary = null;
            if (start == null && end == null)
                boundary = activeTiling.AddBoundary(startCoor, endCoor);
            else if (start != null && end == null)
                boundary = activeTiling.AddBoundary(start, endCoor);
            else if (start == null && end != null)
                boundary = activeTiling.AddBoundary(startCoor, end);
            else if (start != null && end != null)
                boundary = activeTiling.AddBoundary(start, end);
            if (boundary == null)
                throw new InvalidOperationException("add boundary failed:");
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            activeTiling.RemoveBoundary(boundary);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            boundary.UpdateGeom(ins.newParam.lineString());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryDirection, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            activeTiling.UpdateBoundaryNaviDirection(boundary, ins.newParam.naviInfo().direction);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryNavigable, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            activeTiling.UpdateBoundaryNavigable(boundary, ins.newParam.naviInfo().navigable);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.SpaceNavigable, (ins) =>
        {
            CellSpace? space = indoorFeatures.FindSpaceGeom(ins.oldParam.coor());
            if (space == null)
                throw new ArgumentException("can not find space contain point: " + ins.oldParam.coor().ToString());
            activeTiling.UpdateSpaceNavigable(space, ins.newParam.naviInfo().navigable);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.SpaceId, (ins) =>
        {
            CellSpace? space = indoorFeatures.FindSpaceGeom(ins.oldParam.coor());
            if (space == null)
                throw new ArgumentException("can not find space contain point: " + ins.oldParam.coor().ToString());
            space.containerId = ins.newParam.containerId();
            space.children.Clear();
            List<string> childrenId = new List<string>(ins.newParam.childrenId().Split(','));
            childrenId.ForEach(childId => space.children.Add(new Container(childId)));
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.RLine, (ins) =>
        {
            RepresentativeLine? rLine = indoorFeatures.FindRLine(ins.oldParam.lineString(), out var rLineGroup);
            if (rLine == null || rLineGroup == null)
                throw new ArgumentException("can not find representative line: " + ins.oldParam.lineString());
            activeTiling.UpdateRLinePassType(rLineGroup, rLine.fr, rLine.to, ins.newParam.naviInfo().passType);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.POI, (ins) =>
        {
            Container? layOn = indoorFeatures.FindSpaceGeom(ins.newParam.coor());
            ICollection<Container?> spaces = new List<Container?>(ins.newParam.coors().Select(coor => indoorFeatures.FindSpaceGeom(coor)).ToList());
            ICollection<Container?> queue = new List<Container?>(ins.newParam.lineString().Coordinates.Select(coor => indoorFeatures.FindSpaceGeom(coor)).ToList());
            if (ins.newParam.values().Contains(POICategory.Human.ToString()) || ins.newParam.values().Contains(POICategory.PaAmr.ToString()))
            {
                var poi = new IndoorPOI(new Point(ins.newParam.coor()), layOn, spaces, queue, ins.newParam.values().ToArray());
                ins.newParam.values2().ForEach(label => Debug.Log(label));
                ins.newParam.values2().ForEach(label => poi.AddLabel(label));
                activeTiling.AddPOI(poi);
            }
            else throw new Exception("unknow poi type: " + ins.newParam.values());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.POI, (ins) =>
        {
            IndoorPOI? poi = indoorFeatures.FindIndoorPOI(ins.oldParam.coor());
            if (poi == null) throw new Exception("can not find poi");
            activeTiling.RemovePOI(poi);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.POI, (ins) =>
        {
            IndoorPOI? poi = indoorFeatures.FindIndoorPOI(ins.oldParam.coor());
            if (poi == null) throw new Exception("can not find poi");
            poi.point = new Point(ins.newParam.coor());
        });
    }

    public bool AddGridMap(GridMap gridmap)
    {
        if (gridMaps.Any(map => map.id == gridmap.id)) return false;
        gridMaps.Insert(0, gridmap.Clone());
        OnGridMapListUpdated?.Invoke(gridMaps);
        OnGridMapCreated?.Invoke(gridMaps[0]);
        Debug.Log("Grid map added");
        return true;
    }

    public bool RemoveGridMap(int index)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        var tobeRemoved = gridMaps[index];
        gridMaps.RemoveAt(index);
        OnGridMapListUpdated?.Invoke(gridMaps);
        OnGridMapRemoved?.Invoke(tobeRemoved);
        return true;
    }

    public bool RemoveGridMap(string id)
    {
        int index = gridMaps.FindIndex(map => map.id == id);
        if (index < 0) return false;
        var tobeRemoved = gridMaps[index];

        gridMaps.RemoveAt(index);
        OnGridMapListUpdated?.Invoke(gridMaps);
        OnGridMapRemoved?.Invoke(tobeRemoved);
        return true;
    }

    public bool MoveGridMap(int index, MapOrigin newOrigin)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        gridMaps[index].globalOrigin = newOrigin.Clone();
        return true;
    }

    public bool MoveGridMap(string id, MapOrigin newOrigin)
    {
        int index = gridMaps.FindIndex(map => map.id == id);
        if (index < 0) return false;
        gridMaps[index].globalOrigin = newOrigin.Clone();
        return true;
    }

    public bool RenameGridMap(string oldName, string newName)
    {
        int index = gridMaps.FindIndex(map => map.id == oldName);
        if (index < 0) return false;

        gridMaps[index].id = newName;
        OnGridMapListUpdated?.Invoke(gridMaps);
        return true;
    }

    public bool RenameGridMap(int index, string newName)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        gridMaps[index].id = newName;
        OnGridMapListUpdated?.Invoke(gridMaps);
        return true;
    }

    public bool UseGridMap(int index)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        var temp = gridMaps[index];
        gridMaps.RemoveAt(index);
        gridMaps.Insert(0, temp);
        OnGridMapListUpdated?.Invoke(gridMaps);
        return true;
    }

    public void SelectMap()
    {
        if (simulating) return;
        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;
        if (currentSimData != null)
        {
            Debug.Log("going to remove agent");
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
            currentSimData.active = false;
            currentSimData = null;
        }
    }

    public void SelectSimulation(string simName)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(sim => sim.name == simName);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + simName);

        if (currentSimData != null)
        {
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
            currentSimData.active = false;
        }
        currentSimData = simDataList[index];
        currentSimData.active = true;

        activeHistory = currentSimData.history;
        activeInstructionInterpreter = currentSimData.instructionInterpreter;
        currentSimData.agents.ForEach(agent => OnAgentCreate?.Invoke(agent));

        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public SimData? AddSimulation(string name)
    {
        if (simulating) return null;
        if (simDataList.Any(sim => sim.name == name)) return null;
        if (currentSimData != null)
        {
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
            currentSimData.active = false;
        }

        SimData newSimData = new SimData(name);
        newSimData.OnAgentCreate = OnAgentCreate;
        newSimData.OnAgentRemoved = OnAgentRemoved;
        newSimData.active = true;
        simDataList.Add(newSimData);
        currentSimData = newSimData;
        activeHistory = currentSimData.history;
        activeInstructionInterpreter = currentSimData.instructionInterpreter;

        OnSimulationListUpdated?.Invoke(simDataList);

        return newSimData;
    }

    public void RemoveSimulation(int index)
    {
        if (simulating) return;
        if (index >= simDataList.Count) throw new ArgumentException("simulation index out of range");
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void RemoveSimulation(string name)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(simData => simData.name == name);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + name);
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void RenameSimulation(string oldName, string newName)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(simData => simData.name == oldName);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + oldName);
        simDataList[index].name = newName;
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public bool Undo()
    {
        if (simulating) return false;
        if (inSession) return false;
        var instructions = activeHistory.Undo(out var snapShot);
        if (instructions.Count > 0)
        {
            List<ReducedInstruction> reverseIns = ReducedInstruction.Reverse(instructions);
            reverseIns.ForEach(ins => Debug.Log(ins.ToString()));
            activeInstructionInterpreter.Execute(reverseIns);
            if (activeHistory == history)
                OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
            else
                OnSimulationListUpdated?.Invoke(simDataList);
            return true;
        }
        else
        {
            Debug.LogWarning("can not undo");
            return false;
        }
    }

    public bool Redo()
    {
        if (simulating) return false;
        if (inSession) return false;
        var instructions = activeHistory.Redo(out var snapShot);
        if (instructions.Count > 0)
        {
            instructions.ForEach(ins => Debug.Log(ins.ToString()));
            activeInstructionInterpreter.Execute(instructions);
            if (activeHistory == history)
                OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
            else
                OnSimulationListUpdated?.Invoke(simDataList);
            return true;
        }
        else
        {
            Debug.LogWarning("can not redo");
            return false;
        }
    }

    public void SessionStart()
    {
        activeHistory.SessionStart();
        inSession = true;
    }
    public void SessionCommit()
    {
        inSession = false;
        activeHistory.SessionCommit();
        OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
    }

    public bool IntersectionLessThan(LineString ls, int threshold, out List<CellBoundary> crossesBoundaries, out List<Coordinate> intersections)
    {
        return indoorFeatures.activeLayer.IntersectionLessThan(ls, threshold, out crossesBoundaries, out intersections);
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = activeTiling.AddBoundary(startCoor, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = activeTiling.AddBoundary(start, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return boundary;
    }
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = activeTiling.AddBoundary(start, end, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = activeTiling.AddBoundary(startCoor, end, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return boundary;
    }

    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = activeTiling.AddBoundaryAutoSnap(startCoor, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return boundary;
    }

    public CellVertex SplitBoundary(Coordinate middleCoor)
    {
        CellVertex vertex = activeTiling.SplitBoundary(middleCoor, out var oldBoundary, out var newBoundary1, out var newBoundary2);
        history.SessionStart();
        if (newBoundary1.leftSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.leftSpace.Geom!.Centroid.Coordinate, newBoundary1.leftSpace.navigable, newBoundary1.leftSpace.navigable));
        if (newBoundary1.rightSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.rightSpace.Geom!.Centroid.Coordinate, newBoundary1.rightSpace.navigable, newBoundary1.rightSpace.navigable));
        history.DoStep(ReducedInstruction.RemoveBoundary(oldBoundary));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary1));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary2));
        if (newBoundary1.leftSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.leftSpace.Geom!.Centroid.Coordinate, newBoundary1.leftSpace.navigable, newBoundary1.leftSpace.navigable));
        if (newBoundary1.rightSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.rightSpace.Geom!.Centroid.Coordinate, newBoundary1.rightSpace.navigable, newBoundary1.rightSpace.navigable));
        history.SessionCommit();
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return vertex;
    }

    public CellVertex SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        CellVertex vertex = activeTiling.SplitBoundary(middleCoor, boundary, out var newBoundary1, out var newBoundary2);
        history.SessionStart();
        if (newBoundary1.leftSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.leftSpace.Geom!.Centroid.Coordinate, newBoundary1.leftSpace.navigable, newBoundary1.leftSpace.navigable));
        if (newBoundary1.rightSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.rightSpace.Geom!.Centroid.Coordinate, newBoundary1.rightSpace.navigable, newBoundary1.rightSpace.navigable));
        history.DoStep(ReducedInstruction.RemoveBoundary(boundary));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary1));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary2));
        if (newBoundary1.leftSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.leftSpace.Geom!.Centroid.Coordinate, newBoundary1.leftSpace.navigable, newBoundary1.leftSpace.navigable));
        if (newBoundary1.rightSpace != null)
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(newBoundary1.rightSpace.Geom!.Centroid.Coordinate, newBoundary1.rightSpace.navigable, newBoundary1.rightSpace.navigable));
        history.SessionCommit();
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return vertex;
    }

    public bool UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();
        bool ret = activeTiling.UpdateVertices(vertices, newCoors);
        if (ret) history.DoCommit(ReducedInstruction.UpdateVertices(oldCoors, newCoors));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        return ret;
    }
    public void RemoveBoundary(CellBoundary boundary)
    {
        var spaces = boundary.Spaces();

        history.SessionStart();
        if (spaces.Count == 1)
        {
            history.DoStep(ReducedInstruction.UpdateSpaceNavigable(spaces[0].Geom!.Centroid.Coordinate, spaces[0].navigable, Navigable.Navigable));
        }
        else if (spaces.Count == 2)
        {
            if (spaces[0].navigable < spaces[1].navigable)
            {
                history.DoStep(ReducedInstruction.UpdateSpaceNavigable(spaces[1].Polygon.InteriorPoint.Coordinate, spaces[1].Navigable, spaces[0].navigable));
                activeTiling.UpdateSpaceNavigable(spaces[1], spaces[0].navigable);
            }
            else
            {
                history.DoStep(ReducedInstruction.UpdateSpaceNavigable(spaces[0].Polygon.InteriorPoint.Coordinate, spaces[0].Navigable, spaces[1].navigable));
                activeTiling.UpdateSpaceNavigable(spaces[0], spaces[1].navigable);
            }
        }
        history.DoStep(ReducedInstruction.RemoveBoundary(boundary));
        history.SessionCommit();

        activeTiling.RemoveBoundary(boundary);
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
    }
    public void RemoveBoundaries(List<CellBoundary> boundaries)
    {
        history.SessionStart();
        boundaries.ForEach(b => RemoveBoundary(b));
        history.SessionCommit();
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
    }
    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryDirection(boundary.geom, boundary.NaviDir, direction));
        activeTiling.UpdateBoundaryNaviDirection(boundary, direction);
    }
    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryNavigable(boundary.geom, boundary.Navigable, navigable));
        activeTiling.UpdateBoundaryNavigable(boundary, navigable);
    }
    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateSpaceNavigable(space.Polygon.InteriorPoint.Coordinate, space.Navigable, navigable));
        activeTiling.UpdateSpaceNavigable(space, navigable);
    }

    public void UpdateSpaceId(CellSpace space, string newContainerId, List<string> childrenId)
    {
        var oldContainerId = space.containerId;
        var oldChildrenId = string.Join(',', space.children.Select(child => child.containerId));
        var newChildrenId = string.Join(',', childrenId);
        try
        {
            activeTiling.UpdateSpaceId(space, newContainerId, childrenId);
            history.DoCommit(ReducedInstruction.UpdateSpaceId(space.Polygon.InteriorPoint.Coordinate, oldContainerId, oldChildrenId, newContainerId, newChildrenId));
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning(e.Message);
            Debug.LogWarning("Ignore the operation. Try another id please");
        }

    }
    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        history.DoCommit(ReducedInstruction.UpdateRLinePassType(rLines.Geom(fr, to), rLines.passType(fr, to), passType));
        activeTiling.UpdateRLinePassType(rLines, fr, to, passType);
    }

    public void AddAgent(AgentDescriptor agent, AgentTypeMeta meta)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.AddAgent(agent);
        currentSimData.history.DoCommit(ReducedInstruction.AddAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);

        if (!agentMetaList.ContainsKey(meta.typeName))
            agentMetaList.Add(meta.typeName, meta);
    }

    public void RemoveAgent(AgentDescriptor agent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.RemoveAgentEqualsTo(agent);
        currentSimData.history.DoCommit(ReducedInstruction.RemoveAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);

        if (!currentSimData.agents.Any(a => a.type == agent.type))
            agentMetaList.Remove(agent.type);
    }

    public void UpdateAgent(AgentDescriptor oldAgent, AgentDescriptor newAgent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");
        currentSimData.history.DoCommit(ReducedInstruction.UpdateAgent(oldAgent, newAgent));
        currentSimData.UpdateAgent(oldAgent, newAgent);
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void UpdateAgents(List<AgentDescriptor> oldAgents, List<AgentDescriptor> newAgents)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");
        if (oldAgents.Count != newAgents.Count) throw new ArgumentException("old agents count != new agents count");
        currentSimData.history.SessionStart();
        for (int i = 0; i < oldAgents.Count; i++)
        {
            currentSimData.history.DoStep(ReducedInstruction.UpdateAgent(oldAgents[i], newAgents[i]));
            currentSimData.UpdateAgent(oldAgents[i], newAgents[i]);
        }
        currentSimData.history.SessionCommit();
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void AddPOI(IndoorPOI poi)
    {
        activeTiling.AddPOI(poi);
        history.DoCommit(
                ReducedInstruction.AddIndoorPOI(poi.point.Coordinate,
                                                poi.foi.Select(space => space.Geom!.Centroid.Coordinate).ToList(),
                                                poi.queue.Select(space => space.Geom!.Centroid.Coordinate).ToArray(),
                                                poi.category.Select(c => c.term).ToList(),
                                                poi.label.Select(l => l.value).ToList()));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
    }

    public void UpdatePOI(IndoorPOI poi, Coordinate coor)
    {
        Coordinate oldCoordinate = poi.point.Coordinate;
        if (activeTiling.UpdatePOI(poi, coor))
            history.DoCommit(ReducedInstruction.UpdateIndoorPOI(oldCoordinate, coor));
    }

    public void RemovePOI(IndoorPOI poi)
    {
        activeTiling.RemovePOI(poi);

        history.DoCommit(
            ReducedInstruction.RemoveIndoorPOI(poi.point.Coordinate,
                                               poi.foi.Select(space => space.Geom!.Centroid.Coordinate).ToList(),
                                               poi.queue.Select(space => space.Geom!.Centroid.Coordinate).ToArray(),
                                               poi.category.Select(c => c.term).ToList(),
                                               poi.label.Select(l => l.value).ToList()));
        if (!inSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
    }
}
