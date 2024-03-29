using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using NetTopologySuite.Geometries;

using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

#nullable enable

public class IndoorSimData
{
    [JsonProperty] public string softwareVersion = "unknow";
    [JsonProperty] public string schemaHash = "unknow";

    [JsonProperty] private Guid? uuid = null;
    [JsonIgnore] public Guid? Uuid { get => uuid; }

    [JsonIgnore]
    static string? schemaStableString = null;

    [JsonProperty] public DateTime? latestUpdateTime = null;

    [JsonProperty] public string? description = null;

    [JsonProperty] public string? author = null;

    [JsonProperty] public List<GridMap> gridMaps = new();

    [JsonProperty] public IndoorFeatures? indoorFeatures;

    [JsonIgnore] public List<IndoorTiling> indoorTilings = new();

    [JsonIgnore] public IndoorTiling? activeTiling = null;
    [JsonIgnore]
    public IndoorTiling ActiveTiling
    {
        get => activeTiling ?? throw new Exception("activeTilling null"); private set => activeTiling = value;
    }

    [JsonProperty] public InstructionHistory<ReducedInstruction> history = new();

    [JsonProperty] private List<SimData> simDataList = new();
    [JsonProperty] public Dictionary<string, AgentTypeMeta> agentMetaList = new();
    [JsonIgnore] public SimData? currentSimData { get; private set; } = null;
    [JsonIgnore] public bool simulating = false;

    [JsonIgnore] public InstructionHistory<ReducedInstruction> activeHistory;
    [JsonIgnore] private readonly InstructionInterpreter instructionInterpreter = new();
    [JsonIgnore] private InstructionInterpreter activeInstructionInterpreter;

    [JsonProperty] public List<Asset> assets = new();
    [JsonProperty] public string digestCache = "";

    [JsonIgnore] public Action<List<Asset>> OnAssetListUpdated = (a) => { };
    [JsonIgnore] public Action<List<GridMap>> OnGridMapListUpdated = (gridMaps) => { };
    [JsonIgnore] public Action<GridMap> OnGridMapCreated = (gridmap) => { };
    [JsonIgnore] public Action<GridMap> OnGridMapRemoved = (gridmap) => { };
    [JsonIgnore] public Action<IndoorFeatures?> OnIndoorFeatureUpdated = (indoorFeatues) => { };
    [JsonIgnore] public Action<List<SimData>> OnSimulationListUpdated = (sims) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentCreate = (a) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentRemoved = (a) => { };

    [JsonIgnore] public Action PostAction = () => { };
    [JsonIgnore] public Action PostActionAfterException = () => { };


    public IndoorSimData()
    {
        RegisterInstructionExecutor();
        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;
    }

    static public JSchema JSchema()
        => new JSchemaGenerator() { ContractResolver = new IgnoreGeometryCoorContractResolver() }.Generate(typeof(IndoorSimData));

    static public string JSchemaStableString()
    {
        if (schemaStableString == null)
        {
            schemaStableString = JSchema().ToString();
            schemaStableString = schemaStableString.Replace("\r\n", "\n");  // for windows
        }
        return schemaStableString;
    }

    static public string JSchemaHash()
        => Hash.GetHashString(JSchemaStableString());

    [OnDeserialized]
    private void OnSerializedMethod(StreamingContext context)
    {
        indoorTilings.Clear();
        indoorFeatures?.layers.ForEach(layer =>
        {
            var indoorTiling = new IndoorTiling(layer, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));
            indoorTiling.AssignIndoorData(layer);
            indoorTilings.Add(indoorTiling);
        });
        ActiveTiling = indoorTilings[0];
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
        ActiveTiling = indoorTilings[0];

        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;

        latestUpdateTime = DateTime.Now;
        uuid = Guid.NewGuid();

        RegisterInstructionExecutor();
    }

    public string Serialize(string softwareVersion, bool indent = false)
    {
        this.softwareVersion = softwareVersion;
        this.schemaHash = JSchemaHash();

        digestCache = indoorFeatures?.CalcDigest() ?? "";
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = indent ? Formatting.Indented : Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
            ContractResolver = ShouldSerializeContractResolver.Instance,
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK",
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

    public bool DeserializeInPlace(string json, Dictionary<string, string> schemaHashHistory, bool historyOnly = false)
    {
        assets.Clear();
        history.Clear();
        IndoorSimData? indoorSimData = Deserialize(json, historyOnly);

        if (indoorSimData == null) return false;

        if (indoorSimData.schemaHash == null || indoorSimData.schemaHash.Length == 0)
            Console.WriteLine("schemaHash is empty. This file is not official file format. Resave it to generate an official file format.");
        else if (indoorSimData.softwareVersion == null || indoorSimData.softwareVersion.Length == 0)
            Console.WriteLine("software version is empty. This file is not official file format. Resave it to generate an official file format.");
        else
        {
            var expectedSchemahash = JSchemaHash();


            if (indoorSimData.schemaHash != expectedSchemahash)
            {
                if (schemaHashHistory.ContainsKey(indoorSimData.softwareVersion))
                {
                    if (schemaHashHistory[indoorSimData.softwareVersion] == indoorSimData.schemaHash)
                    {
                        // translate
                    }
                    else
                    {
                        Console.WriteLine("schema hash history:");
                        foreach (var entry in schemaHashHistory)
                            Console.WriteLine(entry.Key + ": " + entry.Value);
#if !UNITY_EDITOR
                        throw new ArgumentException($"schemaHash({indoorSimData.schemaHash}) not correct for that software version({indoorSimData.softwareVersion})");
#endif
                    }
                }
                else
                {
#if !UNITY_EDITOR
                    throw new ArgumentException("Unknow software version of the map file: " + indoorSimData.softwareVersion);
#endif
                }
            }
        }

        if (indoorFeatures != null)
        {

            indoorFeatures.layers.ForEach(layer =>
            {
                layer.cellVertexMember.ForEach(v => layer.OnVertexRemoved?.Invoke(v));
                layer.cellBoundaryMember.ForEach(b => layer.OnBoundaryRemoved?.Invoke(b));
                layer.cellSpaceMember.ForEach(s => layer.OnSpaceRemoved?.Invoke(s));
                layer.rLineGroupMember.ForEach(r => layer.OnRLinesRemoved?.Invoke(r));
                layer.poiMember.ForEach(p => layer.OnPOIRemoved?.Invoke(p));
                indoorFeatures.OnLayerRemoved?.Invoke(layer);
                indoorFeatures.ClearActiveLayer();
            });

            indoorFeatures.layers.Clear();
            indoorFeatures.layerConnections?.Clear();
        }

        if (indoorSimData.indoorFeatures != null)
        {
            indoorFeatures ??= new();
            indoorFeatures.layers = indoorSimData.indoorFeatures.layers;
            indoorFeatures.layerConnections = indoorSimData.indoorFeatures.layerConnections;
            indoorFeatures.layers.ForEach(layer => indoorFeatures.OnLayerCreated?.Invoke(layer));
            if (indoorFeatures.layers.Count > 0)
                indoorFeatures.ActiveLayer = indoorFeatures.layers[0];

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
                ActiveTiling = indoorTilings[0];
            else
                activeTiling = null;
        }

        simDataList = indoorSimData.simDataList;

        assets = indoorSimData.assets;
        history = indoorSimData.history;

        if (simDataList.Count > 0) currentSimData = simDataList[0];
        else currentSimData = null;

        activeHistory = history;
        OnAssetListUpdated?.Invoke(assets);
        if (indoorFeatures != null) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        OnSimulationListUpdated?.Invoke(simDataList);

        gridMaps.ForEach(gridmap => OnGridMapRemoved?.Invoke(gridmap));
        gridMaps = indoorSimData.gridMaps;
        gridMaps.ForEach(gridmap => OnGridMapCreated?.Invoke(gridmap));

        latestUpdateTime = indoorSimData.latestUpdateTime;

        uuid = indoorSimData.uuid;
        if (uuid == null)
        {
            Console.WriteLine("loaded map don't have uuid");
            uuid = Guid.NewGuid();
            Console.WriteLine("Generate uuid for loaded map: " + uuid);
        }

        return true;
    }

    public static IndoorSimData? Deserialize(string json, bool historyOnly = false)
    {
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
            ContractResolver = ShouldSerializeContractResolver.Instance,
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
        indoorSimData.ActiveTiling = indoorSimData.indoorTilings[0];
        return indoorSimData;
    }

    public Asset ExtractAsset(string name,
                              List<CellVertex> vertices,
                              List<CellBoundary> boundaries,
                              List<CellSpace> spaces,
                              Func<float, float, float, float, string>? captureThumbnailBase64)
    {
        if (vertices.Any(v => !indoorFeatures!.Contains(v))) throw new ArgumentException("can not find some vertex");
        if (boundaries.Any(b => !indoorFeatures!.Contains(b))) throw new ArgumentException("can not find some boundary");
        if (spaces.Any(s => !indoorFeatures!.Contains(s))) throw new ArgumentException("can not find some space");

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
        latestUpdateTime = DateTime.Now;
    }

    public void RemoveAsset(Asset asset)
    {
        if (!assets.Contains(asset)) throw new ArgumentException("can not find the asset: " + asset.name);
        assets.Remove(asset);
        OnAssetListUpdated?.Invoke(assets);
        latestUpdateTime = DateTime.Now;
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
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    private void RegisterInstructionExecutor()
    {
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Vertices, (ins) =>
        {
            List<CellVertex> vertices = new List<CellVertex>();
            foreach (var coor in ins.oldParam.coors())
            {
                CellVertex? vertex = indoorFeatures!.FindVertexCoor(coor);
                if (vertex != null)
                    vertices.Add(vertex);
                else
                    throw new ArgumentException("one of vertex can not found: " + coor);
            }
            ActiveTiling.UpdateVertices(vertices, ins.newParam.coors());
        });

        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.Boundary, (ins) =>
        {
            Coordinate startCoor = ins.newParam.lineString().StartPoint.Coordinate;
            CellVertex? start = indoorFeatures!.FindVertexCoor(startCoor);
            Coordinate endCoor = ins.newParam.lineString().EndPoint.Coordinate;
            CellVertex? end = indoorFeatures.FindVertexCoor(endCoor);

            CellBoundary? boundary = null;
            if (start == null && end == null)
                boundary = ActiveTiling.AddBoundary(startCoor, endCoor);
            else if (start != null && end == null)
                boundary = ActiveTiling.AddBoundary(start, endCoor);
            else if (start == null && end != null)
                boundary = ActiveTiling.AddBoundary(startCoor, end);
            else if (start != null && end != null)
                boundary = ActiveTiling.AddBoundary(start, end);
            if (boundary == null)
                throw new InvalidOperationException("add boundary failed:");
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures!.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            ActiveTiling.RemoveBoundary(boundary);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures!.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            boundary.UpdateGeom(ins.newParam.lineString());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Split, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? oldBoundary = indoorFeatures!.FindBoundaryGeom(ins.oldParam.lineString());
            if (oldBoundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            ActiveTiling.SplitBoundary(ins.newParam.coor(), oldBoundary, out var newBoundary1, out var newBoundary2);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Merge, SubjectType.Boundary, (ins) =>
        {
            CellVertex? vertex = indoorFeatures!.FindVertexCoor(ins.oldParam.coor());
            if (vertex == null)
                throw new ArgumentException("can not find vertex: " + ins.newParam.coor());
            ActiveTiling.MergeBoundary(vertex);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryDirection, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures!.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            ActiveTiling.UpdateBoundaryNaviDirection(boundary, ins.newParam.direction());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryNavigable, (ins) =>
        {
            CellBoundary? boundary = indoorFeatures!.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            ActiveTiling.UpdateBoundaryNavigable(boundary, ins.newParam.navigable());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.SpaceNavigable, (ins) =>
        {
            CellSpace? space = indoorFeatures!.FindSpaceGeom(ins.oldParam.coor());
            if (space == null)
                throw new ArgumentException("can not find space contain point: " + ins.oldParam.coor().ToString());
            ActiveTiling.UpdateSpaceNavigable(space, ins.newParam.navigable());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.SpaceId, (ins) =>
        {
            CellSpace? space = indoorFeatures!.FindSpaceGeom(ins.oldParam.coor());
            if (space == null)
                throw new ArgumentException("can not find space contain point: " + ins.oldParam.coor().ToString());
            space.containerId = ins.newParam.containerId();
            space.children.Clear();
            List<string> childrenId = new(ins.newParam.childrenId().Split(','));
            childrenId.ForEach(childId => space.children.Add(new Container(childId)));
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.RLine, (ins) =>
        {
            RepresentativeLine? rLine = indoorFeatures!.FindRLine(ins.oldParam.lineString(), out var rLineGroup);
            if (rLine == null || rLineGroup == null)
                throw new ArgumentException("can not find representative line: " + ins.oldParam.lineString());
            ActiveTiling.UpdateRLinePassType(rLineGroup, rLine.fr, rLine.to, ins.newParam.passType());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.POI, (ins) =>
        {
            Container? layOn = indoorFeatures!.FindSpaceGeom(ins.newParam.coor());
            ICollection<Container?> spaces = new List<Container?>(ins.newParam.coors().Select(coor => indoorFeatures.FindSpaceGeom(coor)).ToList());
            ICollection<Container?> queue = new List<Container?>(ins.newParam.lineString().Coordinates.Select(coor => indoorFeatures.FindSpaceGeom(coor)).ToList());
            if (ins.newParam.values().Contains(POICategory.Human.ToString()) || ins.newParam.values().Contains(POICategory.PaAmr.ToString()))
            {
                var poi = new IndoorPOI(new Point(ins.newParam.coor()), layOn!, spaces!, queue!, ins.newParam.values().ToArray());
                ins.newParam.values2().ForEach(label => Console.WriteLine(label));
                ins.newParam.values2().ForEach(label => poi.AddLabel(label));
                ActiveTiling.AddPOI(poi);
            }
            else throw new Exception("unknow poi type: " + ins.newParam.values());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.POI, (ins) =>
        {
            IndoorPOI? poi = indoorFeatures!.FindIndoorPOI(ins.oldParam.coor());
            if (poi == null) throw new Exception("can not find poi");
            ActiveTiling.RemovePOI(poi);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.POI, (ins) =>
        {
            IndoorPOI? poi = indoorFeatures!.FindIndoorPOI(ins.oldParam.coor());
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
        Console.WriteLine("Grid map added");
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool RemoveGridMap(int index)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        var tobeRemoved = gridMaps[index];
        gridMaps.RemoveAt(index);
        OnGridMapListUpdated?.Invoke(gridMaps);
        OnGridMapRemoved?.Invoke(tobeRemoved);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
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
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool MoveGridMap(int index, MapOrigin newOrigin)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        gridMaps[index].globalOrigin = newOrigin.Clone();
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool MoveGridMap(string id, MapOrigin newOrigin)
    {
        int index = gridMaps.FindIndex(map => map.id == id);
        if (index < 0) return false;
        gridMaps[index].globalOrigin = newOrigin.Clone();
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool RenameGridMap(string oldName, string newName)
    {
        int index = gridMaps.FindIndex(map => map.id == oldName);
        if (index < 0) return false;

        gridMaps[index].id = newName;
        OnGridMapListUpdated?.Invoke(gridMaps);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool RenameGridMap(int index, string newName)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        gridMaps[index].id = newName;
        OnGridMapListUpdated?.Invoke(gridMaps);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public bool UseGridMap(int index)
    {
        if (index < 0 || index >= gridMaps.Count) return false;
        var temp = gridMaps[index];
        gridMaps.RemoveAt(index);
        gridMaps.Insert(0, temp);
        OnGridMapListUpdated?.Invoke(gridMaps);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return true;
    }

    public void SelectMap()
    {
        if (simulating) return;
        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;
        if (currentSimData != null)
        {
            Console.WriteLine("going to remove agent");
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
            currentSimData = null;
        }
    }

    public void SelectSimulation(string simName)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(sim => sim.name == simName);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + simName);

        if (currentSimData != null)
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
        currentSimData = simDataList[index];

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
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));

        SimData newSimData = new(name)
        {
            OnAgentCreate = OnAgentCreate,
            OnAgentRemoved = OnAgentRemoved
        };
        simDataList.Add(newSimData);
        currentSimData = newSimData;
        activeHistory = currentSimData.history;
        activeInstructionInterpreter = currentSimData.instructionInterpreter;

        OnSimulationListUpdated?.Invoke(simDataList);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();

        return newSimData;
    }

    public void RemoveSimulation(int index)
    {
        if (simulating) return;
        if (index >= simDataList.Count) throw new ArgumentException("simulation index out of range");
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void RemoveSimulation(string name)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(simData => simData.name == name);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + name);
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void RenameSimulation(string oldName, string newName)
    {
        if (simulating) return;
        int index = simDataList.FindIndex(simData => simData.name == oldName);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + oldName);
        simDataList[index].name = newName;
        OnSimulationListUpdated?.Invoke(simDataList);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public bool Undo()
    {
        if (simulating) return false;
        if (activeHistory.InSession) return false;
        var instructions = activeHistory.Undo(out var snapShot);
        if (instructions.Count > 0)
        {
            List<ReducedInstruction> reverseIns = ReducedInstruction.Reverse(instructions);
            reverseIns.ForEach(ins => Console.WriteLine(ins.ToString()));
            ActiveTiling.DisableResultValidate();
            activeInstructionInterpreter.Execute(reverseIns);
            ActiveTiling.EnableResultValidateAndDoOnce();
            if (activeHistory == history)
                OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
            else
                OnSimulationListUpdated?.Invoke(simDataList);
            latestUpdateTime = DateTime.Now;
            return true;
        }
        else
        {
            Console.WriteLine("can not undo");
            return false;
        }
    }

    public bool Redo()
    {
        if (simulating) return false;
        if (activeHistory.InSession) return false;
        var instructions = activeHistory.Redo(out var snapShot);
        if (instructions.Count > 0)
        {
            instructions.ForEach(ins => Console.WriteLine(ins.ToString()));
            ActiveTiling.DisableResultValidate();
            activeInstructionInterpreter.Execute(instructions);
            ActiveTiling.EnableResultValidateAndDoOnce();
            if (activeHistory == history)
                OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
            else
                OnSimulationListUpdated?.Invoke(simDataList);
            latestUpdateTime = DateTime.Now;
            return true;
        }
        else
        {
            Console.WriteLine("can not redo");
            return false;
        }
    }

    public void SessionStart()
    {
        activeHistory.SessionStart();
    }
    public void SessionCommit()
    {
        activeHistory.SessionCommit();
        OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public bool IntersectionLessThan(LineString ls, int threshold, out List<CellBoundary> crossesBoundaries, out List<Coordinate> intersections)
    {
        return indoorFeatures!.ActiveLayer.IntersectionLessThan(ls, threshold, out crossesBoundaries, out intersections);
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = ActiveTiling.AddBoundary(startCoor, endCoor, id);
        if (boundary != null)
        {
            history.DoCommit(ReducedInstruction.AddBoundary(boundary));
            latestUpdateTime = DateTime.Now;
        }
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        if (!activeHistory.InSession) PostAction?.Invoke();
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = ActiveTiling.AddBoundary(start, endCoor, id);
        if (boundary != null)
        {
            history.DoCommit(ReducedInstruction.AddBoundary(boundary));
            latestUpdateTime = DateTime.Now;
        }
        if (!activeHistory.InSession)
            OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        if (!activeHistory.InSession) PostAction?.Invoke();
        return boundary;
    }
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = ActiveTiling.AddBoundary(start, end, id);
        if (boundary != null)
        {
            history.DoCommit(ReducedInstruction.AddBoundary(boundary));
            latestUpdateTime = DateTime.Now;
        }
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        if (!activeHistory.InSession) PostAction?.Invoke();
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = ActiveTiling.AddBoundary(startCoor, end, id);
        if (boundary != null)
        {
            history.DoCommit(ReducedInstruction.AddBoundary(boundary));
            latestUpdateTime = DateTime.Now;
        }
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        if (!activeHistory.InSession) PostAction?.Invoke();
        return boundary;
    }

    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = ActiveTiling.AddBoundaryAutoSnap(startCoor, endCoor, id);
        if (boundary != null)
        {
            history.DoCommit(ReducedInstruction.AddBoundary(boundary));
            latestUpdateTime = DateTime.Now;
        }
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        if (!activeHistory.InSession) PostAction?.Invoke();
        return boundary;
    }

    public CellVertex? SplitBoundary(Coordinate middleCoor)
    {
        CellVertex? vertex = ActiveTiling.SplitBoundary(middleCoor, out var oldBoundary, out var newBoundary1, out var newBoundary2);
        history.DoCommit(ReducedInstruction.SplitBoundary(oldBoundary.geom, middleCoor));
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return vertex;
    }

    public CellVertex? SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        CellVertex? vertex = ActiveTiling.SplitBoundary(middleCoor, boundary, out var newBoundary1, out var newBoundary2);
        history.DoCommit(ReducedInstruction.SplitBoundary(boundary.geom, middleCoor));
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
        return vertex;
    }

    public bool UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();
        bool ret = ActiveTiling.UpdateVertices(vertices, newCoors);
        if (ret) history.DoCommit(ReducedInstruction.UpdateVertices(oldCoors, newCoors));
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
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
                if (spaces[0].Navigable != spaces[1].Navigable)
                    history.DoStep(ReducedInstruction.UpdateSpaceNavigable(spaces[1].Polygon.InteriorPoint.Coordinate, spaces[1].Navigable, spaces[0].navigable));
                ActiveTiling.UpdateSpaceNavigable(spaces[1], spaces[0].navigable);
            }
            else
            {
                if (spaces[0].Navigable != spaces[1].Navigable)
                    history.DoStep(ReducedInstruction.UpdateSpaceNavigable(spaces[0].Polygon.InteriorPoint.Coordinate, spaces[0].Navigable, spaces[1].navigable));
                ActiveTiling.UpdateSpaceNavigable(spaces[0], spaces[1].navigable);
            }
        }
        history.DoStep(ReducedInstruction.RemoveBoundary(boundary));
        history.SessionCommit();

        ActiveTiling.RemoveBoundary(boundary);
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
    public void RemoveBoundaries(List<CellBoundary> boundaries)
    {
        history.SessionStart();
        boundaries.ForEach(b => RemoveBoundary(b));
        history.SessionCommit();
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryDirection(boundary.geom, boundary.NaviDir, direction));
        ActiveTiling.UpdateBoundaryNaviDirection(boundary, direction);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryNavigable(boundary.geom, boundary.Navigable, navigable));
        ActiveTiling.UpdateBoundaryNavigable(boundary, navigable);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateSpaceNavigable(space.Polygon.InteriorPoint.Coordinate, space.Navigable, navigable));
        ActiveTiling.UpdateSpaceNavigable(space, navigable);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void UpdateSpaceId(CellSpace space, string newContainerId, List<string> childrenId)
    {
        var oldContainerId = space.containerId;
        var oldChildrenId = string.Join(',', space.children.Select(child => child.containerId));
        var newChildrenId = string.Join(',', childrenId);
        try
        {
            ActiveTiling.UpdateSpaceId(space, newContainerId, childrenId);
            history.DoCommit(ReducedInstruction.UpdateSpaceId(space.Polygon.InteriorPoint.Coordinate, oldContainerId, oldChildrenId, newContainerId, newChildrenId));
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("Ignore the operation. Try another id please");
        }
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        history.DoCommit(ReducedInstruction.UpdateRLinePassType(rLines.Geom(fr, to), rLines.passType(fr, to), passType));
        ActiveTiling.UpdateRLinePassType(rLines, fr, to, passType);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void AddAgent(AgentDescriptor agent, AgentTypeMeta meta)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.AddAgent(agent);
        currentSimData.history.DoCommit(ReducedInstruction.AddAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);

        if (!agentMetaList.ContainsKey(meta.typeName))
            agentMetaList.Add(meta.typeName, meta);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void RemoveAgent(AgentDescriptor agent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.RemoveAgentEqualsTo(agent);
        currentSimData.history.DoCommit(ReducedInstruction.RemoveAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);

        if (!currentSimData.agents.Any(a => a.type == agent.type))
            agentMetaList.Remove(agent.type);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void UpdateAgent(AgentDescriptor oldAgent, AgentDescriptor newAgent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");
        currentSimData.history.DoCommit(ReducedInstruction.UpdateAgent(oldAgent, newAgent));
        currentSimData.UpdateAgent(oldAgent, newAgent);
        OnSimulationListUpdated?.Invoke(simDataList);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
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
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void AddPOI(IndoorPOI poi)
    {
        ActiveTiling.AddPOI(poi);
        history.DoCommit(
                ReducedInstruction.AddIndoorPOI(poi.point.Coordinate,
                                                poi.foi.Select(space => space.Geom!.Centroid.Coordinate).ToList(),
                                                poi.queue.Select(space => space.Geom!.Centroid.Coordinate).ToArray(),
                                                poi.category.Select(c => c.term).ToList(),
                                                poi.label.Select(l => l.value).ToList()));
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void UpdatePOI(IndoorPOI poi, Coordinate coor)
    {
        Coordinate oldCoordinate = poi.point.Coordinate;
        if (ActiveTiling.UpdatePOI(poi, coor))
            history.DoCommit(ReducedInstruction.UpdateIndoorPOI(oldCoordinate, coor));
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }

    public void RemovePOI(IndoorPOI poi)
    {
        ActiveTiling.RemovePOI(poi);

        history.DoCommit(
            ReducedInstruction.RemoveIndoorPOI(poi.point.Coordinate,
                                               poi.foi.Select(space => space.Geom!.Centroid.Coordinate).ToList(),
                                               poi.queue.Select(space => space.Geom!.Centroid.Coordinate).ToArray(),
                                               poi.category.Select(c => c.term).ToList(),
                                               poi.label.Select(l => l.value).ToList()));
        if (!activeHistory.InSession) OnIndoorFeatureUpdated?.Invoke(indoorFeatures);
        latestUpdateTime = DateTime.Now;
        if (!activeHistory.InSession) PostAction?.Invoke();
    }
}
