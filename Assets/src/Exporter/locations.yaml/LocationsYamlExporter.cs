using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class LocationsYamlExporter : IExporter
{
    public static double PaAmrFunctionDirection = Math.PI;
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
        Dictionary<CellSpace, Node> space2Node = new Dictionary<CellSpace, Node>();

        graph = new Graph();
        int i = 0;
        indoorSimData.indoorFeatures.layers[0].cellSpaceMember.ForEach(space =>
        {
            var centroid = space.Geom.Centroid;
            string key = $"MAIN_" + i.ToString("D4");
            i++;
            Node newNode = new Node(key, new double[] { centroid.X, centroid.Y, 0.0 });
            graph.locations.Add(newNode);
            space2Node[space] = newNode;
        });

        indoorSimData.indoorFeatures.layers[0].cellBoundaryMember.ForEach(boundary =>
        {

            if (boundary.NaviDir != NaviDirection.NoneDirection &&
                boundary.leftSpace != null && boundary.leftSpace.navigable == Navigable.Navigable &&
                boundary.rightSpace != null && boundary.rightSpace.navigable == Navigable.Navigable)
            {
                if (boundary.NaviDir == NaviDirection.Left2Right || boundary.NaviDir == NaviDirection.BiDirection)
                    graph.routes.Add(new Edge(space2Node[boundary.leftSpace], space2Node[boundary.rightSpace]));

                if (boundary.NaviDir == NaviDirection.Right2Left || boundary.NaviDir == NaviDirection.BiDirection)
                    graph.routes.Add(new Edge(space2Node[boundary.rightSpace], space2Node[boundary.leftSpace]));
            }

        });

        indoorSimData.indoorFeatures.layers[0].poiMember.ForEach(poi =>
        {
            var coor = poi.point.Coordinate;
            var node = graph.ClosestNode(coor.X, coor.Y, 0.05);  // TODO haha, magic number

            if (node != null)  // close to node
            {
                node.name = poi.GetLabels()[0];

                HashSet<IndoorPOI> potentialHumanPois = indoorSimData.indoorFeatures.layers[0].Space2POIs(poi.foi[0]);
                IndoorPOI humanPoi = potentialHumanPois.FirstOrDefault((poi) => poi.CategoryContains(POICategory.Human.ToString()));
                if (humanPoi != null)
                {
                    double dy = humanPoi.point.Y - poi.point.Y;
                    double dx = humanPoi.point.X - poi.point.X;
                    double rotation = Math.Atan2(dy, dx) - PaAmrFunctionDirection;
                    while (rotation > Math.PI) rotation -= 2 * Math.PI;
                    while (rotation < -Math.PI) rotation += 2 * Math.PI;
                    node.pose[2] = rotation;
                }
                else
                {
                    throw new ArgumentException("can not find related human poi");
                }
            }
            else  // close to edge
            {

            }


        });
    }

    public string Export()
    {
        if (graph == null) throw new InvalidOperationException("Translate first");

        StringBuilder sb = new StringBuilder();

        sb.Append("Locations:\n");
        graph.locations.ForEach(node => sb.Append($"  {node.name}: [{node.pose[0]}, {node.pose[1]}, {node.pose[2]}]\n"));

        sb.Append("Route:\n");
        graph.routes.ForEach(edge => sb.Append($"  - [{edge.from.name}, {edge.to.name}]\n"));

        return sb.ToString();
    }

    public void Export(Stream stream)
    {
        throw new System.NotImplementedException();
    }

}
