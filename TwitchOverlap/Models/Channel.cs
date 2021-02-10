using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchOverlap.Models
{
    public class Channel
    {
        [BsonId]
        public string Id { get; set; }
        public string Avatar { get; set; }
        public DateTime Timestamp { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Overlaps { get; set; }
        public Dictionary<string, int> Data { get; set; }
        public List<Dictionary<string, Dictionary<string, int>>> History { get; set; }
    }

    public class ChannelGames
    {
        [BsonId]
        public string Id { get; set; }
        public string Game { get; set; }
    }

    public class ChannelProjection
    {
        public string Id { get; set; }
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        public string Avatar { get; set; }
        public DateTime Timestamp { get; set; }
        public string Game { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public int Overlaps { get; set; }
        public Dictionary<string, Data> Data { get; set; }
        public List<string> History { get; set; }
        public Dictionary<string, List<int?>> OverlapPoints { get; set; }
    }

    public class Data
    {
        public int Shared { get; set; }
        public string Game { get; set; }
    }

    public class ChannelSummary
    {
        public string Id { get; set; }
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        public int Chatters { get; set; }
        public string Avatar { get; set; }
    }
}