using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

class IdPrefixGenerator
{
    private Dictionary<string, int> idMap = new Dictionary<string, int>();

    public string getId(string prefix)
    {
        string result;
        if (!idMap.ContainsKey(prefix)) idMap[prefix] = 0;
        result = prefix + "_" + idMap[prefix].ToString("D4");
        idMap[prefix]++;
        return result;
    }
}

public class LocationsYamlExporter : IExporter
{
    private static string idPrefix = "LacationYamlExporterId: ";
    public static double PaAmrFunctionDirection = Math.PI;
    public string Name => "locations.yaml";
    public string DefaultStreamName => "locations.yaml";
    public bool CanIncludeFull => true;

    IndoorSimData indoorSimData = null;
    Graph graph = null;
    IdPrefixGenerator id = new IdPrefixGenerator();

    public void Load(IndoorSimData indoorSimData)
    {
        this.indoorSimData = indoorSimData;
    }

    public bool Translate(string layerName)
    {
        ThematicLayer layer = indoorSimData.indoorFeatures.layers.Find(layer => layer.level == layerName);
        if (layer == null) throw new ArgumentException("can not find layer with level name: " + layerName);

        Dictionary<CellSpace, Node> space2Node = new Dictionary<CellSpace, Node>();

        graph = new Graph();


        // node
        layer.cellSpaceMember.ForEach(space =>
        {
            if (space.navigable != Navigable.Navigable) return;
            var centroid = space.Geom.Centroid;
            string key = id.getId("MAIN");
            Node newNode = new Node(key, new double[] { centroid.X, centroid.Y, 0.0 });
            graph.AddNode(newNode);
            space2Node[space] = newNode;
        });

        // edge
        layer.cellBoundaryMember.ForEach(boundary =>
        {
            if (boundary.NaviDir != NaviDirection.NoneDirection &&
                boundary.leftSpace != null && boundary.leftSpace.navigable == Navigable.Navigable &&
                boundary.rightSpace != null && boundary.rightSpace.navigable == Navigable.Navigable)
            {
                if (boundary.NaviDir == NaviDirection.Left2Right || boundary.NaviDir == NaviDirection.BiDirection)
                    graph.AddEdge(new Edge(space2Node[boundary.leftSpace], space2Node[boundary.rightSpace]));

                if (boundary.NaviDir == NaviDirection.Right2Left || boundary.NaviDir == NaviDirection.BiDirection)
                    graph.AddEdge(new Edge(space2Node[boundary.rightSpace], space2Node[boundary.leftSpace]));
            }
        });

        // poi
        layer.poiMember.ForEach(poi =>
        {
            if (poi.CategoryContains(POICategory.Human.ToString())) return;
            var coor = poi.point.Coordinate;
            var node = graph.ClosestNode(coor.X, coor.Y, 0.05);  // TODO haha, magic number

            string newNodeName;

            // close to node
            if (node != null)
            {
                // change name
                if (node.name.StartsWith("MAIN"))
                {
                    node.name = id.getId(poi.GetLabels()[0]);
                    newNodeName = node.name;
                    poi.AddLabel(idPrefix + node.name);
                }
                else
                {
                    throw new ArgumentException("Two differenct poi should not lay on same MAIN point");
                }

                // look for human poi and change direction
                node.pose[2] = Rotation(poi, layer);
            }
            // close to edge
            else
            {
                List<Edge> closestEdges = graph.ClosetEdges(coor.X, coor.Y, 0.10);
                if (closestEdges.Count == 0)
                    throw new InvalidOperationException("poi can not find an edge which close enough");

                // construct new node
                Node newNode = new Node(id.getId(poi.GetLabels()[0]), new double[] { coor.X, coor.Y, Rotation(poi, layer) });
                newNodeName = newNode.name;
                poi.AddLabel(idPrefix + newNode.name);

                // reconnect graph
                graph.AddNode(newNode);
                closestEdges.ForEach(closestEdge =>
                {
                    graph.RemoveEdge(closestEdge);
                    graph.AddEdge(new Edge(closestEdge.from, newNode));
                    graph.AddEdge(new Edge(newNode, closestEdge.to));
                });
            }

            // queue entry

            if (poi.queue != null && poi.queue.Count > 0)
            {
                Container lastContainer = poi.queue.Last();
                var centroid = lastContainer.Geom.Centroid;
                var qNode = graph.ClosestNode(centroid.X, centroid.Y, 0.05);  // TODO haha, magic number
                if (qNode != null)
                {
                    if (qNode.name.StartsWith("MAIN"))
                        qNode.name = 'Q' + newNodeName;
                    else
                        throw new ArgumentException("entry of a queue should not be a business point: " + qNode.name);
                }
                else
                {
                    throw new ArgumentException("can not find the entry of queue: " + newNodeName);
                }
            }
        });
        return true;
    }

    private double Rotation(IndoorPOI poi, ThematicLayer layer)
    {
        // look for human poi and change direction
        HashSet<IndoorPOI> potentialHumanPois = layer.Space2POIs(poi.foi[0]);
        IndoorPOI humanPoi = potentialHumanPois.FirstOrDefault((poi) => poi.CategoryContains(POICategory.Human.ToString()));
        if (humanPoi != null)
        {
            double dy = humanPoi.point.Y - poi.point.Y;
            double dx = humanPoi.point.X - poi.point.X;
            double rotation = Math.Atan2(dy, dx) - PaAmrFunctionDirection;
            while (rotation > Math.PI) rotation -= 2 * Math.PI;
            while (rotation < -Math.PI) rotation += 2 * Math.PI;
            return rotation;
        }
        else
        {
            throw new ArgumentException("can not find related human poi");
        }
    }

    public string Export(string softwareVersion, bool includeFull)
    {
        if (graph == null) throw new InvalidOperationException("Translate first");

        StringBuilder sb = new StringBuilder();

        sb.Append("Locations:\n");
        graph.ForEachNode(node => sb.Append($"  {node.name}: [{node.pose[0]:F3}, {node.pose[1]:F3}, {node.pose[2]:F3}]\n"));

        sb.Append("Route:\n");
        graph.ForEachEdge(edge => sb.Append($"  - [{edge.from.name}, {edge.to.name}]\n"));

        if (includeFull)
        {
            sb.Append("IndoorSim: ");
            sb.Append(indoorSimData.Serialize(softwareVersion, true));
            sb.Append("\n");
        }

        return sb.ToString();
    }

    public void Reset()
    {
        indoorSimData = null;
        graph = null;
        id = null;
    }

    public void Export(Stream stream)
    {
        throw new System.NotImplementedException();
    }

}
