using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable disable

namespace TwitchOverlapApi.Models
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

        [JsonIgnore]
        public virtual ICollection<Overlap> OverlapChannelNavigations { get; set; } = new List<Overlap>();
        public virtual IList<ChannelHistory> History { get; set; } = new List<ChannelHistory>();
    }
    
    public class ChannelHistory
    {
        public DateTime Timestamp { get; set; }
        public int Id { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Shared { get; set; }

        [JsonIgnore]
        public virtual Channel Channel { get; set; }
    }

    public class ChannelIndex
    {
        public List<ChannelSummary> Channels { get; set; }
        public DateTime LastUpdate { get; set; }
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
