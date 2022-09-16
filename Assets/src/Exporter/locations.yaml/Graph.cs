using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

using UnityEngine;

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

    public Coordinate Coor()
        => new Coordinate(pose[0], pose[1]);
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
    private List<Node> locations = new List<Node>();
    private List<Edge> routes = new List<Edge>();

    public Dictionary<Node, List<Edge>> outEdgeIndex = new Dictionary<Node, List<Edge>>();

    public void ForEachNode(Action<Node> action) => locations.ForEach(node => action.Invoke(node));
    public void ForEachEdge(Action<Edge> action) => routes.ForEach(edge => action.Invoke(edge));

    public void AddNode(Node node)
    {
        locations.Add(node);
        outEdgeIndex[node] = new List<Edge>();
    }

    public void RemoveNode(Node node)
    {
        locations.Remove(node);
        outEdgeIndex.Remove(node);
    }

    public void AddEdge(Edge edge)
    {
        if (!outEdgeIndex.ContainsKey(edge.from)) throw new ArgumentException("Insert an edge before nodes");
        routes.Add(edge);
        outEdgeIndex[edge.from].Add(edge);
    }

    public void RemoveEdge(Edge edge)
    {
        outEdgeIndex[edge.from].Remove(edge);
        routes.Remove(edge);
    }

    public Edge? GetEdge(Node from, Node to)
    {
        var edges = outEdgeIndex[from];
        if (edges == null) return null;
        if (edges.Count == 0) return null;
        return edges.Find(edge => edge.to == to);
    }

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

    public List<Edge> ClosetEdges(double x, double y, double distance)
    {
        List<Edge> result = new List<Edge>();
        double minDistance = double.MaxValue;
        Edge? closestEdge = null;

        Point current = new Point(x, y);


        routes.ForEach(edge =>
        {
            LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { edge.from.Coor(), edge.to.Coor() });
            double distance = DistanceOp.Distance(ls, current);
            if (minDistance > distance)
            {
                minDistance = distance;
                closestEdge = edge;
            }
        });
        if (closestEdge == null)
            throw new InvalidOperationException("no edge found");

        if (minDistance > distance)
        {
            Debug.Log(minDistance);
            Debug.Log(distance);
            return result;
        }


        result.Add(closestEdge);
        Edge? reverseEdge = GetEdge(closestEdge.to, closestEdge.from);
        if (reverseEdge != null)
            result.Add(reverseEdge);

        return result;
    }


}
