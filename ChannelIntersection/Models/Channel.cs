using System;
using System.Collections.Generic;

#nullable disable

namespace ChannelIntersection.Models
{
    public class Channel : IComparable
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
        
        public virtual ICollection<ChannelHistory> ChannelHistories { get; set; }
        
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

        public int CompareTo(object obj)
        {
            var other = obj as Channel;
            return other?.LoginName == null ? 1 : string.Compare(LoginName, other.LoginName, StringComparison.Ordinal);
        }
    }
    
    public class ChannelHistory
    {
        public DateTime Timestamp { get; set; }
        public int Id { get; set; }
        public int? Viewers { get; set; }
        public int? Chatters { get; set; }
        public int? Shared { get; set; }

        public virtual Channel Channel { get; set; }
    }
}
