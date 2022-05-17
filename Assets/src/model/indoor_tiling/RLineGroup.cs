using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

using UnityEngine;


#nullable enable
public class RLineGroup
{
    [JsonPropertyAttribute] public CellSpace space { get; private set; }
    [JsonPropertyAttribute] public List<RepresentativeLine> undefaultRLines { get; private set; } = new List<RepresentativeLine>();
    [JsonIgnore] public List<RepresentativeLine> rLines { get; private set; } = new List<RepresentativeLine>();
    [JsonIgnore] public const PassType defaultPassType = PassType.AllowedToPass;
    [JsonIgnore] public Action OnUpdate = () => { };

    public RLineGroup() { }

    private void FillDefaultRLines()
    {
        int rLineCount = space.allBoundaries.Count * (space.allBoundaries.Count - 1);
        if (rLines.Count < rLineCount)
        {
            foreach (var fr in space.allBoundaries)
                foreach (var to in space.allBoundaries)
                    if (fr != to)
                        if (undefaultRLines.FirstOrDefault(rl => rl.fr == fr && rl.to == to) == null)
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
            undefaultRLines.Add(rl);
        if (rl.pass != defaultPassType && passType == defaultPassType)
            undefaultRLines.Remove(rl);

        rl.pass = passType;
    }

    public List<RepresentativeLine> next(CellBoundary from)
        => rLines.Where(rLine => rLine.fr == from).ToList();

    public void UpdateGeom()
        => rLines.ForEach(rl => rl.UpdateGeom(space));
}
