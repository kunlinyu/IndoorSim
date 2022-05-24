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

    [JsonPropertyAttribute] public IndoorData indoorData = new IndoorData();
    [JsonIgnore] public IndoorTiling indoorTiling;

    // TODO: put assets and instruction here

    [JsonPropertyAttribute] public List<SimData> simDataList = new List<SimData>();
    [JsonIgnore] public SimData currentSimData;

    [JsonIgnore] public Simulation? simulation = null;

    [JsonPropertyAttribute] public InstructionHistory<ReducedInstruction> instructionHistory = new InstructionHistory<ReducedInstruction>();
    [JsonPropertyAttribute] public List<Asset> assets = new List<Asset>();
    [JsonPropertyAttribute] public string digestCache = "";

    [JsonIgnore] public Action<List<Asset>> OnAssetUpdated = (a) => { };

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

        return sw.ToString();
        // return JsonConvert.SerializeObject(this);
    }

    public bool DeserializeInPlace(string json, bool historyOnly = false)
    {
        assets.Clear();
        instructionHistory.Clear();

        IndoorSimData? indoorSimData = Deserialize(json, historyOnly);
        if (indoorSimData == null) return false;

        indoorData = indoorSimData.indoorData;
        simDataList = indoorSimData.simDataList;
        assets = indoorSimData.assets;
        instructionHistory = indoorSimData.instructionHistory;
        OnAssetUpdated?.Invoke(assets);

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
            indoorSimData.instructionHistory.Uuundo();
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

        instructionHistory.SessionStart();

        // indoorData.vertexPool.ForEach(v => AddVertexInternal(v));
        // indoorData.boundaryPool.ForEach(b => AddBoundaryInternal(b));
        // indoorData.spacePool.ForEach(s => AddSpaceInternal(s));
        // TODO: apply rLine in asset

        instructionHistory.SessionCommit();
    }

    public IndoorSimData()
    {
        indoorTiling = new IndoorTiling(indoorData, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));

        SimData simData = new SimData();
        simDataList.Add(simData);
        currentSimData = simData;
    }

    public bool Undo()
    {
        var instructions = instructionHistory.Undo(out var snapShot);
        if (instructions.Count > 0)
        {
            InterpretInstruction(ReducedInstruction.Reverse(instructions));
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
        var instructions = instructionHistory.Redo(out var snapShot);
        if (instructions.Count > 0)
        {
            InterpretInstruction(instructions);
            return true;
        }
        else
        {
            Debug.LogWarning("can not redo");
            return false;
        }
    }

    public void SessionStart() => instructionHistory.SessionStart();
    public void SessionCommit() => instructionHistory.SessionCommit();

    private void InterpretInstruction(List<ReducedInstruction> instructions)
    {
        foreach (var instruction in instructions)
            InterpretInstruction(instruction);
    }

    private void InterpretInstruction(ReducedInstruction instruction)
    {
        Debug.Log("interpret instruction: " + instruction);
        instructionHistory.IgnoreDo = true;
        switch (instruction.subject)
        {
            case SubjectType.Vertices:
                switch (instruction.predicate)
                {
                    case Predicate.Update:
                        {
                            List<CellVertex> vertices = new List<CellVertex>();
                            foreach (var coor in instruction.oldParam.coors())
                            {
                                CellVertex? vertex = indoorData.FindVertexCoor(coor);
                                if (vertex != null)
                                    vertices.Add(vertex);
                                else
                                    throw new ArgumentException("one of vertex can not found: " + coor);
                            }
                            indoorTiling.UpdateVertices(vertices, instruction.newParam.coors());
                            break;
                        }
                    default:
                        throw new ArgumentException("Unknown predicate of subject type Vertices: " + instruction.predicate);
                }
                break;
            case SubjectType.Boundary:
                switch (instruction.predicate)
                {
                    case Predicate.Add:
                        {
                            Coordinate startCoor = instruction.newParam.lineString().StartPoint.Coordinate;
                            CellVertex? start = indoorData.FindVertexCoor(startCoor);
                            Coordinate endCoor = instruction.newParam.lineString().EndPoint.Coordinate;
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
                        }
                        break;
                    case Predicate.Remove:
                        {
                            CellBoundary? boundary = indoorData.FindBoundaryGeom(instruction.oldParam.lineString());
                            if (boundary == null)
                                throw new ArgumentException("can not find boundary: " + instruction.oldParam.lineString());
                            indoorTiling.RemoveBoundary(boundary);
                        }
                        break;
                    case Predicate.Update:
                        {
                            CellBoundary? boundary = indoorData.FindBoundaryGeom(instruction.oldParam.lineString());
                            if (boundary == null)
                                throw new ArgumentException("can not find boundary: " + instruction.oldParam.lineString());
                            boundary.UpdateGeom(instruction.newParam.lineString());
                        }
                        break;
                    default:
                        throw new ArgumentException("Unknown predicate: " + instruction.predicate);
                }
                break;
            case SubjectType.BoundaryDirection:
                if (instruction.predicate == Predicate.Update)
                {
                    CellBoundary? boundary = indoorData.FindBoundaryGeom(instruction.oldParam.lineString());
                    if (boundary == null)
                        throw new ArgumentException("can not find boundary: " + instruction.oldParam.lineString());
                    indoorTiling.UpdateBoundaryNaviDirection(boundary, instruction.newParam.naviInfo().direction);
                }
                else
                    throw new ArgumentException("boundary direction can only update.");
                break;
            case SubjectType.BoundaryNavigable:
                if (instruction.predicate == Predicate.Update)
                {
                    CellBoundary? boundary = indoorData.FindBoundaryGeom(instruction.oldParam.lineString());
                    if (boundary == null)
                        throw new ArgumentException("can not find boundary: " + instruction.oldParam.lineString());
                    indoorTiling.UpdateBoundaryNavigable(boundary, instruction.newParam.naviInfo().navigable);
                }
                else
                    throw new ArgumentException("boundary navigable can only update.");
                break;
            case SubjectType.SpaceNavigable:
                if (instruction.predicate == Predicate.Update)
                {
                    CellSpace? space = indoorData.FindSpaceGeom(instruction.oldParam.coor());
                    if (space == null)
                        throw new ArgumentException("can not find space contain point: " + instruction.oldParam.coor().ToString());
                    indoorTiling.UpdateSpaceNavigable(space, instruction.newParam.naviInfo().navigable);
                }
                else
                    throw new ArgumentException("space navigable can only update.");
                break;
            case SubjectType.RLine:
                if (instruction.predicate == Predicate.Update)
                {
                    RepresentativeLine? rLine = indoorData.FindRLine(instruction.oldParam.lineString(), out var rLineGroup);
                    if (rLine == null || rLineGroup == null)
                        throw new ArgumentException("can not find representative line: " + instruction.oldParam.lineString());
                    indoorTiling.UpdateRLinePassType(rLineGroup, rLine.fr, rLine.to, instruction.newParam.naviInfo().passType);
                }
                else
                    throw new ArgumentException("rLine pass type can only update.");
                break;
            default:
                throw new ArgumentException("Unknown subject type: " + instruction.subject);
        }
        instructionHistory.IgnoreDo = false;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(startCoor, endCoor, id);
        if (boundary != null) instructionHistory.DoCommit(ReducedInstruction.AddBoundary(boundary));
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(start, endCoor, id);
        if (boundary != null) instructionHistory.DoCommit(ReducedInstruction.AddBoundary(boundary));
        return boundary;
    }
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(start, end, id);
        if (boundary != null) instructionHistory.DoCommit(ReducedInstruction.AddBoundary(boundary));
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundary(startCoor, end, id);
        if (boundary != null) instructionHistory.DoCommit(ReducedInstruction.AddBoundary(boundary));
        return boundary;
    }

    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellBoundary? boundary = indoorTiling.AddBoundaryAutoSnap(startCoor, endCoor, id);
        if (boundary != null) instructionHistory.DoCommit(ReducedInstruction.AddBoundary(boundary));
        return boundary;
    }
    public CellVertex SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        CellVertex vertex = indoorTiling.SplitBoundary(boundary, middleCoor, out var newBoundary1, out var newBoundary2);
        instructionHistory.DoCommit(ReducedInstruction.RemoveBoundary(boundary));
        instructionHistory.DoCommit(ReducedInstruction.AddBoundary(newBoundary1));
        instructionHistory.DoCommit(ReducedInstruction.AddBoundary(newBoundary2));
        return vertex;
    }

    public bool UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();
        bool ret = indoorTiling.UpdateVertices(vertices, newCoors);
        if (ret) instructionHistory.DoCommit(ReducedInstruction.UpdateVertices(oldCoors, newCoors));
        return ret;
    }
    public void RemoveBoundary(CellBoundary boundary)
    {
        instructionHistory.DoCommit(ReducedInstruction.RemoveBoundary(boundary));
        indoorTiling.RemoveBoundary(boundary);
    }
    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        instructionHistory.DoCommit(ReducedInstruction.UpdateBoundaryDirection(boundary.geom, boundary.NaviDir, direction));
        indoorTiling.UpdateBoundaryNaviDirection(boundary, direction);
    }
    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        instructionHistory.DoCommit(ReducedInstruction.UpdateBoundaryNavigable(boundary.geom, boundary.Navigable, navigable));
        indoorTiling.UpdateBoundaryNavigable(boundary, navigable);
    }
    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        instructionHistory.DoCommit(ReducedInstruction.UpdateSpaceNavigable(space.Polygon.InteriorPoint.Coordinate, space.Navigable, navigable));
        indoorTiling.UpdateSpaceNavigable(space, navigable);
    }
    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        instructionHistory.DoCommit(ReducedInstruction.UpdateRLinePassType(rLines.Geom(fr, to), rLines.passType(fr, to), passType));
        indoorTiling.UpdateRLinePassType(rLines, fr, to, passType);
    }
}
