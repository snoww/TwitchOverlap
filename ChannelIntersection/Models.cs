using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ChannelIntersection
{
    public class ChannelModel : IComparable
    {
        [BsonId]
        public string Id { get; set; }

        [BsonIgnore]
        public string DisplayName { get; set; }

        [BsonIgnore]
        public string Avatar { get; set; }
        public DateTime Timestamp { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Overlaps { get; set; }
        public Dictionary<string, int> Data { get; set; }
        public List<Dictionary<string, Dictionary<string, int>>> History { get; set; }
        
        public int CompareTo(object obj)
        {
            var other = obj as ChannelModel;
            return other?.Id == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
        }
    }
}