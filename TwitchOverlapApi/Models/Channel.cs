using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchOverlapApi.Models
{
    public class Channel
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int Viewers { get; set; }
        public int Chatters { get; set; }
        public Dictionary<string, int> Data { get; set; }
    }
}