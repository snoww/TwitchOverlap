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
    
    public class OverlapDaily
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }
    
    public class OverlapRolling3Days
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }
    
    public class OverlapRolling7Days
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }
    
    public class OverlapRolling14Days
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }

    public class ChannelOverlap
    {
        public string Name { get; set; }
        public int Shared { get; set; }
    }
}
