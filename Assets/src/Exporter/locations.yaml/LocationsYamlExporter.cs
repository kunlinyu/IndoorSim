using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

public class LocationsYamlExporter : IExporter
{
    public string name => "locations.yaml";

    public string defaultStreamName => "locations.yaml";

    IndoorSimData indoorSimData = null;
    Graph graph = null;

    public void Load(IndoorSimData indoorSimData)
    {
        this.indoorSimData = indoorSimData;
    }

    public void Translate()
    {
        graph = new Graph();
        graph.locations.Add(new Node() { name = "MAIN_000", pose = new float[] { 0.0f, 0.0f, 0.0f} });
        graph.locations.Add(new Node() { name = "MAIN_001", pose = new float[] { 0.0f, 0.0f, 0.0f} });
        graph.locations.Add(new Node() { name = "MAIN_002", pose = new float[] { 0.0f, 0.0f, 0.0f} });
        graph.routes.Add(new Edge() { from = graph.locations[0], to = graph.locations[1]});
        graph.routes.Add(new Edge() { from = graph.locations[1], to = graph.locations[2]});
    }

    public string Export()
    {
        if (graph == null) throw new InvalidOperationException("Translate first");

        StringBuilder sb = new StringBuilder();

        sb.Append("Locations:\n");
        graph.locations.ForEach(node => sb.Append($"  {node.name}: [{node.pose[0]}, {node.pose[0]}, {node.pose[0]}]\n"));

        sb.Append("Route:\n");
        graph.routes.ForEach(edge => sb.Append($"  - [{edge.from.name}, {edge.to.name}]\n"));

        return sb.ToString();
    }

    public void Export(Stream stream)
    {
        throw new System.NotImplementedException();
    }

}
