using System.Globalization;
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
            doc.Load("data/jan-fa-2.gexf");
            
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
                    // scale size from by half, from 10-100 to 5-50
                    Size = float.Parse(node.GetProperty("viz:size").GetProperty("@value").GetString()!) / 2,
                    Color = hex,
                    X = float.Parse(node.GetProperty("viz:position").GetProperty("@x").GetString()!),
                    // un-flip y coordinate for echarts, since it flips it by default due to different coordinate directions
                    Y = float.Parse(node.GetProperty("viz:position").GetProperty("@y").GetString()!) * -1
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
            
            File.WriteAllBytes("data/2022-01-atlas-non-cc.json", JsonSerializer.SerializeToUtf8Bytes(parsedGraph,new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase}));
            
            // var nodes = doc.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes;
            // for (int i = 0; i < nodes.Count; i++)
            // {
            //     XmlNode node = nodes[i];
            //     var id = node.Attributes[0].Value;
            //     var label = node.Attributes[1].Value;
            //     if (label.Any(c => c > 255))
            //     {
            //         node.Attributes[1].Value = id;
            //     }
            // }
            //
            // doc.Save("data/jan-fa-2-ascii.gexf");
        }
    }
}
