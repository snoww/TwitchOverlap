using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchOverlapApi.Models
{
    public class Channel
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, int> Data { get; set; }
    }
}