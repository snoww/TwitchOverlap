#nullable disable

namespace DailyAggregation.Models
{
    public class Overlap
    {
        public DateTime Timestamp { get; set; }
        public int Channel { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }

    public class AggregateOverlap
    {
        public DateOnly Date { get; set; }
        public int Channel { get; set; }
        public int ChannelTotalOverlap { get; set; }
        public int ChannelTotalUnique { get; set; }
        public List<ChannelOverlap> Shared { get; set; }
    }
    
    public class OverlapDaily : AggregateOverlap
    {
    }
    
    public class OverlapRolling3Days : AggregateOverlap
    {
    }
    
    public class OverlapRolling7Days : AggregateOverlap
    {
    }    
    
    public class OverlapRolling30Days : AggregateOverlap
    {
    }

    public class ChannelOverlap
    {
        public string Name { get; set; }
        public int Shared { get; set; }
    }
}
