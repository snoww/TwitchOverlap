using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChannelIntersection
{
    public static class Program
    {
        public static async Task Main()
        {
            Dictionary<string, string> config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.OpenRead("config.json")) ?? new Dictionary<string, string>();

            using var processor = new ChannelProcessor(config["POSTGRES"], config["TWITCH_CLIENT"], config["TWITCH_TOKEN"], config["S3AccessKey"], config["S3SecretKey"]);
            await processor.Run();
        }
    }
}