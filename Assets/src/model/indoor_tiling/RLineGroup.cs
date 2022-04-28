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
    [JsonPropertyAttribute] public List<RepresentativeLine> rLines { get; private set; } = new List<RepresentativeLine>();
    [JsonIgnore] public Action OnUpdate = () => { };

    public void UpdateOneRLine(CellBoundary b1, CellBoundary b2, PassType passType)
    {
        RepresentativeLine? rl = rLines.FirstOrDefault(rl => rl.from == b1 && rl.to == b2);
        if (rl == null)
            rLines.Add(new RepresentativeLine(b1, b2, space, passType));
        else
            rl.passType = passType;
    }

    public RLineGroup(CellSpace space)
    {
        this.space = space;
        var inbound = space.InBound();
        var outbound = space.OutBound();

        foreach (var b1 in space.allBoundaries)
            foreach (var b2 in space.allBoundaries)
                if (b1 != b2)
                    Add(new RepresentativeLine(b1, b2, space, PassType.AllowedToPass));
    }
    public RLineGroup(CellSpace space, List<RepresentativeLine> rLines)
    {
        this.space = space;
        this.rLines = rLines;
    }

    public void Add(RepresentativeLine rLine)
    {
        if (space != rLine.through)
            throw new ArgumentException($"The representative(from space {rLine.through.Id}) line don't belong to the space({space.Id})");
        rLines.Add(rLine);
        OnUpdate?.Invoke();
    }

    public void Add(CellBoundary from, CellBoundary to)
        => Add(new RepresentativeLine(from, to, space, PassType.AllowedToPass));

    public void Remove(RepresentativeLine rLine)
    {
        if (!rLines.Contains(rLine)) throw new ArgumentException("can not find the representative line to be remove");
        rLines.Remove(rLine);
        OnUpdate?.Invoke();
    }

    public void Remove(CellBoundary from, CellBoundary to)
    {
        RepresentativeLine? target = rLines.FirstOrDefault(rLine => rLine.from == from && rLine.to == to);
        if (target == null) throw new ArgumentException($"can not find representative line from {from.Id} to {to.Id}");
        rLines.Remove(target);
        OnUpdate?.Invoke();
    }

    public List<RepresentativeLine> next(CellBoundary from)
        => rLines.Where(rLine => rLine.from == from).ToList();

    public void UpdateGeom()
        => rLines.ForEach(rl => rl.UpdateGeom());
}
