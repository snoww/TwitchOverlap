using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }
        public int Overlapped { get; set; }

        public Overlap(DateTime timestamp, int source, int target, int overlapped)
        {
            Timestamp = timestamp;
            Source = source;
            Target = target;
            Overlapped = overlapped;
        }
    }
}
