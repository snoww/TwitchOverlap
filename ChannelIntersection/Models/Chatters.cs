using System;
using System.Collections.Generic;

namespace ChannelIntersection.Models
{
    public class Chatters
    {
        public DateTime Date { get; set; }
        public Dictionary<string, HashSet<string>> Users { get; set; }
    }
}