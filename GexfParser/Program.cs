using System.Text.Json;
using System.Xml;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace GexfParser
{
    public static class Program
    {
        public static void Main()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("data/dec-fa-3.gexf");
            using var json = JsonDocument.Parse(JsonConvert.SerializeXmlNode(doc));
            var graph = json.RootElement.GetProperty("gexf").GetProperty("graph");
            var nodes = graph.GetProperty("nodes").GetProperty("node");
            var parsedGraph = new Graph();
            foreach (JsonElement node in nodes.EnumerateArray())
            {
                var rgb = node.GetProperty("viz:color");
                var hex = $"#{int.Parse(rgb.GetProperty("@r").GetString()!):X2}{int.Parse(rgb.GetProperty("@g").GetString()!):X2}{int.Parse(rgb.GetProperty("@b").GetString()!):X2}";
                
                parsedGraph.Nodes.Add(new Node
                {
                    Id = node.GetProperty("@id").GetString()!,
                    Name = node.GetProperty("@label").GetString()!,
                    Value = int.Parse(node.GetProperty("attvalues").GetProperty("attvalue")[0].GetProperty("@value").GetString()!),
                    Size = float.Parse(node.GetProperty("viz:size").GetProperty("@value").GetString()!),
                    Color = hex,
                    X = float.Parse(node.GetProperty("viz:position").GetProperty("@x").GetString()!),
                    Y = float.Parse(node.GetProperty("viz:position").GetProperty("@y").GetString()!)
                });
            }

            var edges = graph.GetProperty("edges").GetProperty("edge");
            foreach (JsonElement edge in edges.EnumerateArray())
            {
                parsedGraph.Edges.Add(new Edge
                {
                    Source = edge.GetProperty("@source").GetString()!,
                    Target = edge.GetProperty("@target").GetString()!
                });
            }
            
            File.WriteAllBytes("data/dec-fa-3.json", JsonSerializer.SerializeToUtf8Bytes(parsedGraph,new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase}));
        }
    }
}