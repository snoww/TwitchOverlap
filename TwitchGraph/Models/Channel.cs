using System;

namespace TwitchGraph.Models
{
    public class Channel : IComparable
    {
        public Channel(string id, string displayName, int size)
        {
            Id = id;
            DisplayName = displayName;
            Size = size;
        }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Size { get; set; }

        public int CompareTo(object obj)
        {
            var other = obj as Channel;
            return other?.Id == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
        }
    }
}