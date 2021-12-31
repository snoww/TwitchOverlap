using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchGraph.Models;

namespace TwitchGraph
{
    public static class Program
    {
        private static readonly HttpClient Http = new();
        private static string _twitchToken;
        private static string _twitchClient;

        private const int MinOverlap = 10000;
        private const int MaxChannels = 1500;

        public static async Task Main()
        {
            // using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            // {
            //     _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
            //     _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
            // }

            DateTime timestamp = DateTime.UtcNow;
            // string dirName = $"{timestamp.Month}-{timestamp.Year}";
            Console.WriteLine($"importing channel chatters at {timestamp:u}");

            var sw = new Stopwatch();
            sw.Start();
            
            var files = Directory.GetFiles("/Users/snow/Documents/projects/twitch-graph-data", "*.json");
            var agg = new AggregateDays();
            var (channelOverlap, channelUniqueChatters) = await agg.Aggregate(files);
            
            Console.WriteLine($"aggregate took {sw.Elapsed.TotalSeconds}s");
            
            await File.WriteAllBytesAsync("channelOverlap.json", JsonSerializer.SerializeToUtf8Bytes(channelOverlap));
            await File.WriteAllBytesAsync("channelUnique.json", JsonSerializer.SerializeToUtf8Bytes(channelUniqueChatters));

            // var channelOverlap = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(await File.ReadAllTextAsync("channelOverlap.json"));
            // var channelUniqueChatters = JsonSerializer.Deserialize<Dictionary<string, int>>(await File.ReadAllTextAsync("channelUnique.json"));
            Debug.Assert(channelUniqueChatters != null, nameof(channelUniqueChatters) + " != null");
            Debug.Assert(channelOverlap != null, nameof(channelOverlap) + " != null");
            
            var nodeSet = new HashSet<string>(MaxChannels);
            foreach (var (channel, viewers) in channelUniqueChatters.OrderByDescending(x => x.Value).Take(MaxChannels))
            {
                if (!channelOverlap.ContainsKey(channel))
                {
                    continue;
                }

                if (!channelOverlap[channel].OrderByDescending(x => x.Value).Any(x => x.Value > MinOverlap))
                {
                    continue;
                }

                nodeSet.Add(channel);
            }

            await using StreamWriter nodeStream = File.CreateText("nodes.csv");
            await nodeStream.WriteLineAsync("id,label,size");
            await using StreamWriter edgeStream = File.CreateText("edges.csv");
            await edgeStream.WriteLineAsync("source,target,weight");
            
            foreach (var channel in nodeSet)
            {
                foreach (var (ch, overlap) in channelOverlap[channel].OrderByDescending(x => x.Value).Where(x => x.Value > MinOverlap))
                {
                    if (!nodeSet.Contains(ch))
                    {
                        continue;
                    }

                    await edgeStream.WriteLineAsync($"{channel},{ch},{overlap}");
                }
                
                await nodeStream.WriteLineAsync($"{channel},{channel},{channelUniqueChatters[channel]}");
            }
        }

        private static async Task GetChannelDisplayName(Dictionary<string, Channel> channels)
        {
            foreach (string reqString in RequestBuilder(channels.Keys.ToList()))
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchToken);
                request.Headers.Add("Client-Id", _twitchClient);
                request.RequestUri = new Uri($"https://api.twitch.tv/helix/users?{reqString}");
                using HttpResponseMessage response = await Http.SendAsync(request);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                JsonElement.ArrayEnumerator data = json.RootElement.GetProperty("data").EnumerateArray();
                foreach (JsonElement channel in data)
                {
                    string login = channel.GetProperty("login").GetString();
                    channels[login!].DisplayName = channel.GetProperty("display_name").GetString();
                }
            }
        }

        private static IEnumerable<string> RequestBuilder(IReadOnlyCollection<string> channels)
        {
            var shards = (int)Math.Ceiling(channels.Count / 100.0);
            var list = new List<string>(shards);
            for (int i = 0; i < shards; i++)
            {
                var request = new StringBuilder();
                foreach (string channel in channels.Skip(i * 100).Take(100))
                {
                    request.Append("&login=").Append(channel);
                }

                list.Add(request.ToString()[1..]);
            }

            return list;
        }

        private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new[] { t2 }));
        }
    }
}
