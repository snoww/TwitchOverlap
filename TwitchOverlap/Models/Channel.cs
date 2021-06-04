using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable disable

namespace TwitchOverlap.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string LoginName { get; set; }
        public string DisplayName { get; set; }
        public string Avatar { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Shared { get; set; }
        public DateTime LastUpdate { get; set; }

        public virtual ICollection<Overlap> OverlapSourceNavigations { get; set; } = new HashSet<Overlap>();
        public virtual ICollection<Overlap> OverlapTargetNavigations { get; set; } = new HashSet<Overlap>();
        
        public Channel(int id, string loginName, string game, int viewers, int chatters, int shared, DateTime lastUpdate, string avatar, string displayName)
        {
            Id = id;
            Game = game;
            Viewers = viewers;
            Chatters = chatters;
            Shared = shared;
            LastUpdate = lastUpdate;
            Avatar = avatar;
            DisplayName = displayName;
        }

        public Channel(string loginName, string displayName, string game, int viewers, DateTime lastUpdate)
        {
            LoginName = loginName;
            DisplayName = displayName;
            Game = game;
            Viewers = viewers;
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
