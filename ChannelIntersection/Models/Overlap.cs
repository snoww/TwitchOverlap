using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public int Overlapped { get; set; }

        public Overlap(DateTime timestamp, string source, string target, int overlapped)
        {
            Timestamp = timestamp;
            Source = source;
            Target = target;
            Overlapped = overlapped;
        }
    }
}
