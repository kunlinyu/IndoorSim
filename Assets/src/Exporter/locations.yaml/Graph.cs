using System.Collections;
using System.Collections.Generic;

#nullable enable

public class Node
{
    public string name;
    public double[] pose;

    public Node(string name, double[] pose)
    {
        this.name = name;
        this.pose = pose;
    }
}

public class Edge
{
    public Node from;
    public Node to;

    public Edge(Node from, Node to)
    {
        this.from = from;
        this.to = to;
    }
}


public class Graph
{
    public List<Node> locations = new List<Node>();
    public List<Edge> routes = new List<Edge>();

    public Node? ClosestNode(double x, double y, double distance)
    {
        double minDistance2 = double.MaxValue;
        Node? closestNode = null;
        locations.ForEach(node =>
        {
            double dx = node.pose[0] - x;
            double dx2 = dx * dx;
            double dy = node.pose[1] - y;
            double dy2 = dy * dy;
            if (minDistance2 > dx2 + dy2)
            {
                minDistance2 = dx2 + dy2;
                closestNode = node;
            }
        });

        if (minDistance2 > distance)
            closestNode = null;

        return closestNode;
    }

}
