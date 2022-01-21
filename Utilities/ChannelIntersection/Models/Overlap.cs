using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public int Channel { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }

    public class ChannelOverlap
    {
        public string Name { get; set; }
        public int Shared { get; set; }
    }
}
