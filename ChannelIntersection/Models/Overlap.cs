using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    // public class Overlap
    // {
    //     public string Id { get; set; }
    //     public DateTime Timestamp { get; set; }
    //     public Dictionary<string, int> Data { get; set; }
    //
    //     public virtual Channel Channel { get; set; }
    //
    //     public Overlap(string id, DateTime timestamp, Dictionary<string, int> data)
    //     {
    //         Id = id;
    //         Timestamp = timestamp;
    //         Data = data;
    //     }
    // }
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public int Overlapped { get; set; }

        public virtual Channel SourceNavigation { get; set; }
        public virtual Channel TargetNavigation { get; set; }

        public Overlap(DateTime timestamp, string source, string target, int overlapped)
        {
            Timestamp = timestamp;
            Source = source;
            Target = target;
            Overlapped = overlapped;
        }
    }
}
