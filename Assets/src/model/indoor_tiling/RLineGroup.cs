using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

using UnityEngine;

public struct BoundaryWithGeom
{
    public CellBoundary boundary;
    public LineString rLineGeom;
}


#nullable enable
public class RLineGroup
{
    [JsonPropertyAttribute] public CellSpace space { get; private set; }
    [JsonPropertyAttribute] public List<RepresentativeLine> udRL { get; private set; } = new List<RepresentativeLine>();  // un-default RLines
    [JsonIgnore] public List<RepresentativeLine> rLines { get; private set; } = new List<RepresentativeLine>();
    [JsonIgnore] public const PassType defaultPassType = PassType.AllowedToPass;
    [JsonIgnore] public Action OnUpdate = () => { };

#pragma warning disable CS8618
    public RLineGroup() { }  // for deserialize only
#pragma warning restore CS8618

    private void FillDefaultRLines()
    {
        int rLineCount = space.allBoundaries.Count * (space.allBoundaries.Count - 1);
        if (rLines.Count < rLineCount)
        {
            foreach (var fr in space.allBoundaries)
                foreach (var to in space.allBoundaries)
                    if (fr != to)
                        if (udRL.FirstOrDefault(rl => rl.fr == fr && rl.to == to) == null)
                            rLines.Add(new RepresentativeLine(fr, to, space, defaultPassType));
        }

        if (rLines.Count != rLineCount)
            throw new Exception($"rLines.Count({rLines.Count}) != space.allBoundaries.Count * (space.allBoundaries.Count - 1)({rLineCount})");
    }

    public RLineGroup(CellSpace space)
    {
        this.space = space;
        var inbound = space.InBound();
        var outbound = space.OutBound();
        FillDefaultRLines();
    }

    public PassType passType(CellBoundary fr, CellBoundary to)
    {
        FillDefaultRLines();
        RepresentativeLine? rl = rLines.FirstOrDefault(rl => rl.fr == fr && rl.to == to);
        if (rl == null)
            throw new Exception($"can not find the rline from \"fr\"({fr.Id}) to \"to\"({to.Id})");
        return rl.pass;
    }

    public LineString Geom(CellBoundary fr, CellBoundary to)
    {
        FillDefaultRLines();
        LineString? ls = rLines.FirstOrDefault(rl => rl.fr == fr && rl.to == to)?.geom;
        if (ls == null)
            throw new Exception($"can not find the geom of rline from \"fr\"({fr.Id}) to \"to\"({to.Id})");
        return ls;
    }

    public void SetPassType(CellBoundary fr, CellBoundary to, PassType passType)
    {
        FillDefaultRLines();
        RepresentativeLine? rl = rLines.FirstOrDefault(rl => rl.fr == fr && rl.to == to);
        if (rl == null)
            throw new Exception($"can not find the rline from \"fr\"({fr.Id}) to \"to\"({to.Id})");

        if (rl.pass == defaultPassType && passType != defaultPassType)
            udRL.Add(rl);
        if (rl.pass != defaultPassType && passType == defaultPassType)
            udRL.Remove(rl);

        rl.pass = passType;
    }

    public List<BoundaryWithGeom> next(CellBoundary fr)
        => rLines.Where(rLine => rLine.pass == PassType.AllowedToPass)
                 .Where(rLine => rLine.fr == fr)
                 .Select(rLine => new BoundaryWithGeom() { boundary = rLine.to, rLineGeom = rLine.geom} )
                 .ToList();

    public void UpdateGeom()
        => rLines.ForEach(rl => rl.UpdateGeom(space));
}
