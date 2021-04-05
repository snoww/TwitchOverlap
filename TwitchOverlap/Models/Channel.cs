using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable disable

namespace TwitchOverlap.Models
{
    public class Channel
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Avatar { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Shared { get; set; }
        public DateTime LastUpdate { get; set; }

        [JsonIgnore]
        public virtual ICollection<Overlap> Histories { get; set; } = new List<Overlap>();

        public Channel(string id, string game, int viewers, int chatters, int shared, DateTime lastUpdate)
        {
            Id = id;
            Game = game;
            Viewers = viewers;
            Chatters = chatters;
            Shared = shared;
            LastUpdate = lastUpdate;
        }
    }
    
    public class ChannelSummary
    {
        public string Id { get; }
        public string Avatar { get; }
        public string DisplayName { get; }
        public int Chatters { get; }

        public ChannelSummary(string id, string displayName, string avatar, int chatters)
        {
            Id = id;
            DisplayName = displayName;
            Avatar = avatar;
            Chatters = chatters;
        }
    }
    
    
}
