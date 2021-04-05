using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable disable

namespace TwitchOverlap.Models
{
    public class Overlap
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, int> Data { get; set; }

        [JsonIgnore]
        public virtual Channel Channel { get; set; }

        public Overlap(string id, DateTime timestamp, Dictionary<string, int> data)
        {
            Id = id;
            Timestamp = timestamp;
            Data = data;
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
