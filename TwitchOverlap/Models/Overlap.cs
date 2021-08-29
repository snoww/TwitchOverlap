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

    public class OverlapDaily
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
        
        [JsonIgnore]
        public virtual Channel ChannelNavigation { get; set; }
    }
    
    public class OverlapRolling3Days
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
        
        [JsonIgnore]
        public virtual Channel ChannelNavigation { get; set; }
    }
    
    public class OverlapRolling7Days
    {
        public DateTime Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
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
        public static AggregateDays Type => AggregateDays.Default;
        public Channel Channel { get; }
        public List<Data> Data { get; set; } = new();
        
        public ChannelData(Channel channel)
        {
            Channel = channel;
        }
    }
    
    public class OverlapAggregate
    {
        public DateTime Date { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }
    
    public class ChannelAggregateData
    {
        public AggregateDays Type { get; set; }
        public Channel Channel { get; set; }
        public ChannelAggregateChange Change { get; set; }
        public int ChannelTotalUnique { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public DateTime Date { get; set; }
        public List<Data> Data { get; set; } = new();

        public ChannelAggregateData()
        {
        }

        public ChannelAggregateData(Channel channel)
        {
            Channel = channel;
        }
    }

    public class ChannelAggregateChange
    {
        public int? TotalChatterChange { get; set; }
        public double? TotalChatterPercentageChange { get; set; }
        public double? OverlapPercentChange { get; set; }
        public int? TotalOverlapChange { get; set; }
        public double? TotalOverlapPercentageChange { get; set; }
    }

    public class Data
    {
        public string LoginName { get; set; }
        public string DisplayName { get; set; }
        public string Game { get; set; }
        public int Shared { get; set; }
        public int? Change { get; set; }
    }
    
    public class OverlapHistory
    {
        public DateTime Timestamp { get; }
        public IEnumerable<ChannelOverlap> Shared { get; }

        public OverlapHistory(DateTime timestamp, IEnumerable<ChannelOverlap> shared)
        {
            Timestamp = timestamp;
            Shared = shared;
        }
    }

    public enum AggregateDays
    {
        Default = 0,
        OneDay = 1,
        ThreeDays = 3,
        SevenDays = 7,
    }
}
