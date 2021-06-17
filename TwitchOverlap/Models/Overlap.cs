using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchOverlap.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public int Channel { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
        
        [JsonIgnore]
        public virtual Channel ChannelNavigation { get; set; }
    }
    
    public class ChannelOverlap
    {
        public string Name { get; set; }
        public int Shared { get; set; }
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
        public string DisplayName { get; set; }
        public string Game { get; }
        public int Shared { get; }

        public Data(string game, int shared, string displayName)
        {
            Game = game;
            Shared = shared;
            DisplayName = displayName;
        }
    }
}
