using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

class IndoorTiling
{
    public const double RemainSpaceSize = 10000.0d;
    [JsonPropertyAttribute] private List<CellVertex> vertexPool = new List<CellVertex>();
    [JsonPropertyAttribute] private List<CellBoundary> boundaryPool = new List<CellBoundary>();
    [JsonPropertyAttribute] private List<CellSpace> spacePool = new List<CellSpace>();
    [JsonPropertyAttribute] private List<RepresentativeLine> rLinePool = new List<RepresentativeLine>();
    [JsonIgnore] private CellSpace universalRemainSpace;  // URS


    [JsonIgnore] private Dictionary<CellVertex, List<CellBoundary>> vertex2Boundaries = new Dictionary<CellVertex, List<CellBoundary>>();
    [JsonIgnore] private Dictionary<CellVertex, List<CellSpace>> vertex2Spaces = new Dictionary<CellVertex, List<CellSpace>>();
    [JsonIgnore] private Dictionary<CellSpace, List<RepresentativeLine>> space2RLines = new Dictionary<CellSpace, List<RepresentativeLine>>();
    [JsonIgnore] private Dictionary<CellBoundary, List<RepresentativeLine>> boundary2RLines = new Dictionary<CellBoundary, List<RepresentativeLine>>();

    public IndoorTiling()
    {
        Coordinate[] cas = {
            new Coordinate( RemainSpaceSize,  RemainSpaceSize),
            new Coordinate(-RemainSpaceSize,  RemainSpaceSize),
            new Coordinate(-RemainSpaceSize, -RemainSpaceSize),
            new Coordinate( RemainSpaceSize, -RemainSpaceSize),
            new Coordinate( RemainSpaceSize,  RemainSpaceSize),
        };
        universalRemainSpace = new CellSpace(new GeometryFactory().CreatePolygon(cas), new List<CellVertex>(), true);
        spacePool.Add(universalRemainSpace);
    }

    public void AddBoundary(LineString ls)
        => AddBoundary(ls, new CellVertex(ls.StartPoint), new CellVertex(ls.EndPoint));

    private bool Reachable(CellVertex start, CellVertex end)
    {
        return true;  // TODO
    }

    private bool AnyPolygonContainNewBoundary(LineString ls)
    {
        return false;
    }

    private enum EditBoundaryMode
    {
        CreateNewHole,
        ConnectTwoBoundary,
        ExtendBoundary,
        CutNewCellSpace,


        RemoveHole,
        SplitBoundary,
        ShrinkBoundary,
        MergeCellSpace,
    };
    public void AddBoundary(LineString ls, CellVertex start, CellVertex end)
    {
        // TODO: Check ls start/end coordinate
        // TODO: Check intersection


        bool newStart = !vertexPool.Contains(start);
        bool newEnd = !vertexPool.Contains(end);

        // mode
        EditBoundaryMode mode;
        if (newStart && newEnd)                        // both are new
            mode = EditBoundaryMode.CreateNewHole;
        else if (newStart ^ newEnd)                    // one new one old
            mode = EditBoundaryMode.ExtendBoundary;
        else if (Reachable(start, end))                // no new vertex, reachable
            mode = EditBoundaryMode.CutNewCellSpace;
        else                                           // no new vertex, un-reachable
            mode = EditBoundaryMode.ConnectTwoBoundary;

        // target cellspace
        CellSpace? targetCellSpace;
        var RPoint = RepresentativePointOfLineString(ls);
        targetCellSpace = spacePool.FirstOrDefault(cellspace => cellspace.Geom.Contains(RPoint));
        if (targetCellSpace == null)
            throw new Exception("Oops! no cellspace contain the RPoint of new boundary");

        // Add Vertices
        if (newStart) vertexPool.Add(start);
        if (newEnd) vertexPool.Add(end);

        // Add Boundary
        CellBoundary boundary = new CellBoundary(ls, start, end);
        boundaryPool.Add(boundary);

        // Or Detect and add new polygon
        // Or split exist polygon
        if (mode == EditBoundaryMode.CreateNewHole)
        {
            targetCellSpace.AddNewHole(start, end);
        }
        else if (mode == EditBoundaryMode.ConnectTwoBoundary)
        {
            targetCellSpace.ConnectTwoBoundary(start, end);
        }
        else if (mode == EditBoundaryMode.ExtendBoundary)
        {
            if (newStart)  // first argument is vertex on boundary, second is new vertex
                targetCellSpace.ExtendBoundary(end, start);
            else
                targetCellSpace.ExtendBoundary(start, end);
        }
        else if (mode == EditBoundaryMode.CutNewCellSpace)
        {
            var newPolygon = targetCellSpace.CutNewCellSpace(start, end);
            // TODO: use this newPolygon add to pool
        }



        // update lookup tables
    }

    private static Point RepresentativePointOfLineString(LineString ls)
    {
        if (ls.NumPoints == 2) return ls.Centroid;
        if (ls.NumPoints > 2) return ls.GetPointN(1);
        throw new ArgumentException("LineString less than 2 points");
    }

    public void RemoveBoundary(CellBoundary cb)
    {
        if (!boundaryPool.Contains(cb)) throw new ArgumentException("can not find cell boundary");

        // Remove Boundary only
        // Or remove polygon
        // Or merge polygon
        // Remove Vertex

        // update lookup tables
    }

    public void RemoveSpace(CellSpace cs)
    {
        if (!spacePool.Contains(cs)) throw new ArgumentException("can not find cell space");

        // Remove cellspace
        // Remove representativeLine

        // update lookup tables
    }

    public void AddRepresentativeLine(LineString ls, CellBoundary from, CellBoundary to, CellSpace through)
    {
        // new RepresentativeLine(ls, from, to, through);
    }

    public void RemoveRepresentativeLine(RepresentativeLine rLine)
    {

    }




}
