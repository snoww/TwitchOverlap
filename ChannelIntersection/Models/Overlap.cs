using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    public class Overlap
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, int> Data { get; set; }

        public virtual Channel Channel { get; set; }

        public Overlap(string id, DateTime timestamp, Dictionary<string, int> data)
        {
            Id = id;
            Timestamp = timestamp;
            Data = data;
        }
    }
}
