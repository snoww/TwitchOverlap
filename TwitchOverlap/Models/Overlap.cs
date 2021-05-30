using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable disable

namespace TwitchOverlap.Models
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
