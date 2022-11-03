using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

public class BinLocationCsvExporter : IExporter
{
    public string Name => "binLocation.csv";
    public string DefaultStreamName => "binLocation.csv";
    public bool CanIncludeFull => false;

    IndoorSimData? indoorSimData = null;

    List<string> containerIds = new List<string>();

    public void Load(IndoorSimData indoorSimData)
    {
        this.indoorSimData = indoorSimData;
    }

    public bool Translate(string layerName)
    {
        if (indoorSimData == null) throw new InvalidOperationException("call Load() first");
        ThematicLayer? layer = indoorSimData!.indoorFeatures!.layers.Find(layer => layer.level == layerName);
        if (layer == null) throw new ArgumentException("can not find layer with name: " + layerName);

        layer.cellSpaceMember.ForEach(space => space.AllNodeInContainerTree()
                             .ForEach(container => { if (container.containerId != "") containerIds.Add(container.containerId); } ));

        containerIds.Sort();

        return true;
    }

    public string Export(string softwareVersion, bool includeFull)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Bin Location (M),Picking Point (O)\n");
        containerIds.ForEach(id => sb.Append(id + ",\n"));
        return sb.ToString();
    }

    public void Reset()
    {
        indoorSimData = null;
        containerIds.Clear();
    }

    public void Export(Stream stream)
    {
        throw new System.NotImplementedException();
    }
}
