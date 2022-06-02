using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using UnityEngine;

#nullable enable

public class IndoorSimData
{
    [JsonPropertyAttribute] public List<GridMapInfo> gridMapInfos = new List<GridMapInfo>();
    [JsonIgnore] GridMapInfo currentGridMapInfo;

    [JsonPropertyAttribute] public IndoorData indoorData = new IndoorData();
    [JsonIgnore] public IndoorTiling indoorTiling;
    [JsonPropertyAttribute] public InstructionHistory<ReducedInstruction> history = new InstructionHistory<ReducedInstruction>();


    [JsonPropertyAttribute] public List<SimData> simDataList = new List<SimData>();
    [JsonIgnore] public SimData? currentSimData { get; private set;  }

    [JsonIgnore] public InstructionHistory<ReducedInstruction> activeHistory;
    [JsonIgnore] private InstructionInterpreter instructionInterpreter = new InstructionInterpreter();
    [JsonIgnore] private InstructionInterpreter activeInstructionInterpreter;

    [JsonIgnore] public Simulation? simulation = null;

    [JsonPropertyAttribute] public List<Asset> assets = new List<Asset>();
    [JsonPropertyAttribute] public string digestCache = "";

    [JsonIgnore] public Action<List<Asset>> OnAssetUpdated = (a) => { };
    [JsonIgnore] public Action<List<GridMapInfo>> OnGridMapUpdated = (grid) => { };
    [JsonIgnore] public Action<IndoorData> OnIndoorDataUpdated = (indoor) => { };
    [JsonIgnore] public Action<List<SimData>> OnSimulationListUpdated = (sims) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentCreate = (a) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentRemoved = (a) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentUpdated = (a) => { };

    public string Serialize(bool indent = false)
    {
        digestCache = indoorData.CalcDigest();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            Formatting = indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
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

        indoorData = indoorSimData.indoorData;
        simDataList = indoorSimData.simDataList;
        assets = indoorSimData.assets;
        history = indoorSimData.history;
        OnAssetUpdated?.Invoke(assets);
        OnIndoorDataUpdated?.Invoke(indoorData);

        indoorTiling.AssignIndoorData(indoorData);

        return true;
    }

    public static IndoorSimData? Deserialize(string json, bool historyOnly = false)
    {
        IndoorSimData? indoorSimData = JsonConvert.DeserializeObject<IndoorSimData>(json, new WKTConverter(), new CoorConverter(), new StackConverter());
        if (indoorSimData == null) return null;

        if (historyOnly)
        {
            indoorSimData.indoorData = new IndoorData();
            indoorSimData.simDataList = new List<SimData>();
        }
        else
        {
            if (indoorSimData.indoorData != null)
                indoorSimData.indoorData.UpdateIndices();
        }
        indoorSimData.indoorTiling.AssignIndoorData(indoorSimData.indoorData!);
        return indoorSimData;
    }

    public Asset ExtractAsset(string name,
                              List<CellVertex> vertices,
                              List<CellBoundary> boundaries,
                              List<CellSpace> spaces,
                              Func<float, float, float, float, string>? captureThumbnailBase64)
    {
        if (vertices.Any(v => !indoorData.Contains(v))) throw new ArgumentException("can not find some vertex");
        if (boundaries.Any(b => !indoorData.Contains(b))) throw new ArgumentException("can not find some boundary");
        if (spaces.Any(s => !indoorData.Contains(s))) throw new ArgumentException("can not find some space");

        IndoorData newIndoorData = new IndoorData();
        newIndoorData.vertexPool.AddRange(vertices);
        newIndoorData.boundaryPool.AddRange(boundaries);
        newIndoorData.spacePool.AddRange(spaces);
        newIndoorData.rLinePool.AddRange(spaces.Select(s => s.rLines!));
        string json = newIndoorData.Serialize(false);

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
        OnAssetUpdated?.Invoke(assets);

        return asset;
    }

    public void AddAsset(Asset asset)
    {
        IndoorData? indoorData = IndoorData.Deserialize(asset.json);
        if (indoorData == null) throw new ArgumentException("can not deserialize the asset");
        assets.Add(asset);
        OnAssetUpdated?.Invoke(assets);
    }

    public void RemoveAsset(Asset asset)
    {
        if (!assets.Contains(asset)) throw new ArgumentException("can not find the asset: " + asset.name);
        assets.Remove(asset);
        OnAssetUpdated?.Invoke(assets);
    }

    public void ApplyAsset(Asset asset, Coordinate center, float rotation)
    {
        if (!assets.Contains(asset)) throw new ArgumentException("can not find the asset");
        IndoorData? tempIndoorData = IndoorData.Deserialize(asset.json);
        if (tempIndoorData == null) throw new Exception("Oops! can not deserialize the asset");

        history.SessionStart();

        // indoorData.vertexPool.ForEach(v => AddVertexInternal(v));
        // indoorData.boundaryPool.ForEach(b => AddBoundaryInternal(b));
        // indoorData.spacePool.ForEach(s => AddSpaceInternal(s));
        // TODO: apply rLine in asset

        history.SessionCommit();
    }

    public IndoorSimData()
    {
        indoorTiling = new IndoorTiling(indoorData, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));


        activeHistory = history;
        activeInstructionInterpreter = instructionInterpreter;


        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Vertices, (ins) =>
        {
            List<CellVertex> vertices = new List<CellVertex>();
            foreach (var coor in ins.oldParam.coors())
            {
                CellVertex? vertex = indoorData.FindVertexCoor(coor);
                if (vertex != null)
                    vertices.Add(vertex);
                else
                    throw new ArgumentException("one of vertex can not found: " + coor);
            }
            indoorTiling.UpdateVertices(vertices, ins.newParam.coors());
        });

        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.Boundary, (ins) =>
        {
            Coordinate startCoor = ins.newParam.lineString().StartPoint.Coordinate;
            CellVertex? start = indoorData.FindVertexCoor(startCoor);
            Coordinate endCoor = ins.newParam.lineString().EndPoint.Coordinate;
            CellVertex? end = indoorData.FindVertexCoor(endCoor);

            CellBoundary? boundary = null;
            if (start == null && end == null)
                boundary = indoorTiling.AddBoundary(startCoor, endCoor);
            else if (start != null && end == null)
                boundary = indoorTiling.AddBoundary(start, endCoor);
            else if (start == null && end != null)
                boundary = indoorTiling.AddBoundary(startCoor, end);
            else if (start != null && end != null)
                boundary = indoorTiling.AddBoundary(start, end);
            if (boundary == null)
                throw new InvalidOperationException("add boundary failed:");
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorData.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            indoorTiling.RemoveBoundary(boundary);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Boundary, (ins) =>
        {
            CellBoundary? boundary = indoorData.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            boundary.UpdateGeom(ins.newParam.lineString());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryDirection, (ins) =>
        {
            CellBoundary? boundary = indoorData.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            indoorTiling.UpdateBoundaryNaviDirection(boundary, ins.newParam.naviInfo().direction);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.BoundaryNavigable, (ins) =>
        {
            CellBoundary? boundary = indoorData.FindBoundaryGeom(ins.oldParam.lineString());
            if (boundary == null)
                throw new ArgumentException("can not find boundary: " + ins.oldParam.lineString());
            indoorTiling.UpdateBoundaryNavigable(boundary, ins.newParam.naviInfo().navigable);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.SpaceNavigable, (ins) =>
        {
            CellSpace? space = indoorData.FindSpaceGeom(ins.oldParam.coor());
            if (space == null)
                throw new ArgumentException("can not find space contain point: " + ins.oldParam.coor().ToString());
            indoorTiling.UpdateSpaceNavigable(space, ins.newParam.naviInfo().navigable);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.RLine, (ins) =>
        {
            RepresentativeLine? rLine = indoorData.FindRLine(ins.oldParam.lineString(), out var rLineGroup);
            if (rLine == null || rLineGroup == null)
                throw new ArgumentException("can not find representative line: " + ins.oldParam.lineString());
            indoorTiling.UpdateRLinePassType(rLineGroup, rLine.fr, rLine.to, ins.newParam.naviInfo().passType);
        });

    }

    public void SelectMap()
    {
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
        if (simDataList.Any(sim => sim.name == name)) return null;
        if (currentSimData != null)
        {
            currentSimData.agents.ForEach(agent => OnAgentRemoved?.Invoke(agent));
            currentSimData.active = false;
        }

        SimData newSimData = new SimData(name);
        newSimData.OnAgentCreate = OnAgentCreate;
        newSimData.OnAgentRemoved = OnAgentRemoved;
        newSimData.OnAgentUpdated = OnAgentUpdated;
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
        if (index >= simDataList.Count) throw new ArgumentException("simulation index out of range");
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void RemoveSimulation(string name)
    {
        int index = simDataList.FindIndex(simData => simData.name == name);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + name);
        simDataList.RemoveAt(index);
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void RenameSimulation(string oldName, string newName)
    {
        int index = simDataList.FindIndex(simData => simData.name == oldName);
        if (index < 0) throw new ArgumentException("can not find simulation with name: " + oldName);
        simDataList[index].name = newName;
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public bool Undo()
    {
        var instructions = activeHistory.Undo(out var snapShot);
        if (instructions.Count > 0)
        {
            List<ReducedInstruction> reverseIns = ReducedInstruction.Reverse(instructions);
            Debug.Log("interpret instruction: " + reverseIns);
            activeInstructionInterpreter.Execute(reverseIns);
            if (activeHistory == history)
                OnIndoorDataUpdated?.Invoke(indoorData);
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
        var instructions = activeHistory.Redo(out var snapShot);
        if (instructions.Count > 0)
        {
            Debug.Log("interpret instruction: " + instructions);
            activeInstructionInterpreter.Execute(instructions);
            if (activeHistory == history)
                OnIndoorDataUpdated?.Invoke(indoorData);
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

    public void SessionStart() => activeHistory.SessionStart();
    public void SessionCommit() => activeHistory.SessionCommit();


    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(startCoor, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(start, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return boundary;
    }
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(start, end, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(startCoor, end, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return boundary;
    }

    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundaryAutoSnap(startCoor, endCoor, id);
        if (boundary != null) history.DoCommit(ReducedInstruction.AddBoundary(boundary));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return boundary;
    }
    public CellVertex SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        CellVertex vertex = indoorTiling.SplitBoundary(boundary, middleCoor, out var newBoundary1, out var newBoundary2);
        history.SessionStart();
        history.DoStep(ReducedInstruction.RemoveBoundary(boundary));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary1));
        history.DoStep(ReducedInstruction.AddBoundary(newBoundary2));
        history.SessionCommit();
        OnIndoorDataUpdated?.Invoke(indoorData);
        return vertex;
    }

    public bool UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();
        bool ret = indoorTiling.UpdateVertices(vertices, newCoors);
        if (ret) history.DoCommit(ReducedInstruction.UpdateVertices(oldCoors, newCoors));
        OnIndoorDataUpdated?.Invoke(indoorData);
        return ret;
    }
    public void RemoveBoundary(CellBoundary boundary)
    {
        history.DoCommit(ReducedInstruction.RemoveBoundary(boundary));
        indoorTiling.RemoveBoundary(boundary);
        OnIndoorDataUpdated?.Invoke(indoorData);
    }
    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryDirection(boundary.geom, boundary.NaviDir, direction));
        indoorTiling.UpdateBoundaryNaviDirection(boundary, direction);
    }
    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateBoundaryNavigable(boundary.geom, boundary.Navigable, navigable));
        indoorTiling.UpdateBoundaryNavigable(boundary, navigable);
    }
    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        history.DoCommit(ReducedInstruction.UpdateSpaceNavigable(space.Polygon.InteriorPoint.Coordinate, space.Navigable, navigable));
        indoorTiling.UpdateSpaceNavigable(space, navigable);
    }
    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        history.DoCommit(ReducedInstruction.UpdateRLinePassType(rLines.Geom(fr, to), rLines.passType(fr, to), passType));
        indoorTiling.UpdateRLinePassType(rLines, fr, to, passType);
    }

    public void AddAgent(AgentDescriptor agent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.AddAgent(agent);
        currentSimData.history.DoCommit(ReducedInstruction.AddAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void RemoveAgent(AgentDescriptor agent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");

        currentSimData.RemoveAgentEqualsTo(agent);
        currentSimData.history.DoCommit(ReducedInstruction.RemoveAgent(agent));
        OnSimulationListUpdated?.Invoke(simDataList);
    }

    public void UpdateAgent(AgentDescriptor oldAgent, AgentDescriptor newAgent)
    {
        if (currentSimData == null) throw new InvalidOperationException("switch to one of simulation first");
        currentSimData.UpdateAgent(oldAgent, newAgent);
        currentSimData.history.DoCommit(ReducedInstruction.UpdateAgent(oldAgent, newAgent));
    }
}