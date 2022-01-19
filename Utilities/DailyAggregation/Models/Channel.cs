#nullable disable

namespace DailyAggregation.Models
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
        
        public Channel()
        {
        }

        public int CompareTo(object obj)
        {
            var other = obj as Channel;
            return other?.LoginName == null ? 1 : string.Compare(LoginName, other.LoginName, StringComparison.Ordinal);
        }
    }
}
