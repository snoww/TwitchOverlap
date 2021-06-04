using System;
using System.Collections.Generic;

namespace TwitchOverlap.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }
        public int Overlapped { get; set; }
        
        public virtual Channel SourceNavigation { get; set; }
        public virtual Channel TargetNavigation { get; set; }

        public Overlap(DateTime timestamp, int source, int target, int overlapped)
        {
            Timestamp = timestamp;
            Source = source;
            Target = target;
            Overlapped = overlapped;
        }
    }

    public class ChannelData
    {
        public Channel Channel { get; }
        public Dictionary<string, Data> Data { get; set; } = new();
        
        public ChannelData(Channel channel)
        {
            Channel = channel;
        }
    }

    public class Data
    {
        public string Game { get; }
        public int Shared { get; }

        public Data(string game, int shared)
        {
            Game = game;
            Shared = shared;
        }
    }
}
