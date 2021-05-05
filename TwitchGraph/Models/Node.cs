namespace TwitchGraph.Models
{
    public class Node
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public int Size { get; set; }

        public Node(string id, string label, int size)
        {
            Id = id;
            Label = label;
            Size = size;
        }
    }
}
