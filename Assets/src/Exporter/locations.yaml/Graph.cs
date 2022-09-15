using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;

public class Node
{
    public string name;
    public float[] pose;
}

public class Edge
{
    public Node from;
    public Node to;
}


public class Graph
{
    public List<Node> locations = new List<Node>();
    public List<Edge> routes = new List<Edge>();

}
