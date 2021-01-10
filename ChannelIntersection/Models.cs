using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ChannelIntersection
{
    public class ChannelModel : IComparable
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Overlaps { get; set; }
        public Dictionary<string, int> Data { get; set; }
        
        public int CompareTo(object obj)
        {
            var other = obj as ChannelModel;
            if (other?.Id == null) return 1;
            
            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }
    }

    public class ChannelData
    {
        [BsonId]
        public string Id { get; set; }
        public string Avatar { get; set; }
    }
}