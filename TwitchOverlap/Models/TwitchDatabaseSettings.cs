namespace TwitchOverlapApi.Models
{
    public interface ITwitchDatabaseSettings
    {
        string DatabaseName { get; set; }
        string CollectionName { get; set; }
        string ConnectionString { get; set; }
    }
    
    public class TwitchDatabaseSettings : ITwitchDatabaseSettings
    {
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string ConnectionString { get; set; }
    }
}