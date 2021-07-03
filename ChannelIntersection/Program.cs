using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChannelIntersection
{
    public static class Program
    {
        public static async Task Main()
        {
            string twitchToken;
            string twitchClient;
            string psqlConnection;
            using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            {
                twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
                twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
                psqlConnection = json.RootElement.GetProperty("POSTGRES").GetString();
            }

            var processor = new ChannelProcessor(psqlConnection, twitchClient, twitchToken);
            await processor.Run();
        }
    }
}