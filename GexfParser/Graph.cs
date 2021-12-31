namespace GexfParser;

public class Graph
{
    public List<Node> Nodes { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();
}

public class Node
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public int Value { get; set; }
    public float Size { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public class Edge
{
    public string Source { get; set; }
    public string Target { get; set; }
    // public int Weight { get; set; }
}