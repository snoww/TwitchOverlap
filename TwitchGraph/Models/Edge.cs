namespace TwitchGraph.Models
{
    public class Edge
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public int Weight { get; set; }

        public Edge(string source, string target, int weight)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }
    }
}
