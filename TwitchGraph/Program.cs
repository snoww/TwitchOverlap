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

        private const int MaxSearch = 1500;
        private const int MaxChannels = 1000;
        private const int MinOverlap = 1000;

        public static async Task Main()
        {
            using (JsonDocument json = JsonDocument.Parse(await File.ReadAllTextAsync("config.json")))
            {
                _twitchToken = json.RootElement.GetProperty("TWITCH_TOKEN").GetString();
                _twitchClient = json.RootElement.GetProperty("TWITCH_CLIENT").GetString();
            }

            DateTime timestamp = DateTime.UtcNow;
            string dirName = $"{timestamp.Month}-{timestamp.Year}";
            Console.WriteLine($"importing channel chatters at {timestamp:u}");
            
            var sw = new Stopwatch();
            var sw2 = new Stopwatch();
            sw.Start();
            sw2.Start();

            var channelChatters = new Dictionary<string, HashSet<string>>();
            var processed = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            var channels = new Dictionary<string, Channel>();
            
            var info = new DirectoryInfo($@"C:\Users\Snow\Documents\projects\TwitchGraphData\{dirName}");

            // only take the top `MaxSearch` # of channels, since its file size should some what correlate to the number of lines it contains
            foreach (FileInfo file in info.GetFiles().OrderByDescending(p => p.Length).Take(MaxSearch))
            {
                string ch = Path.GetFileNameWithoutExtension(file.Name);
                channelChatters.TryAdd(ch, new HashSet<string>(File.ReadLines(file.FullName)));
            }

            // then we sort the channels by unique chatter count, i.e. number of lines in the file
            // and take the top 1000
            Dictionary<string, HashSet<string>> filteredChannels = channelChatters.OrderByDescending(x => x.Value.Count).Take(MaxChannels).ToDictionary(x => x.Key, x => x.Value);
            foreach ((string ch, HashSet<string> chatters) in filteredChannels)
            {
                processed.TryAdd(ch, new ConcurrentDictionary<string, int>());
                channels.TryAdd(ch, new Channel(ch, string.Empty, chatters.Count));
            }

            Console.WriteLine($"imported {filteredChannels.Count} channels in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            Parallel.ForEach(GetKCombs(filteredChannels.Keys, 2), x =>
            {
                string[] pair = x.ToArray();
                int count = filteredChannels[pair[0]].Count(y => filteredChannels[pair[1]].Contains(y));
                if (count >= MinOverlap)
                {
                    processed[pair[0]].TryAdd(pair[1], count);
                }
            });

            Console.WriteLine($"processed {processed.Count} channels");
            Console.WriteLine($"calculated intersection in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            Directory.CreateDirectory($"./data/{dirName}/");
            await using (StreamWriter nodesStream = File.CreateText($"./data/{dirName}/nodes.csv"))
            {
                // await nodesStream.WriteLineAsync("id,label,size");

                await GetChannelDisplayName(channels);

                Console.WriteLine($"fetched display names in {sw.ElapsedMilliseconds}ms");
                sw.Restart();

                foreach ((string _, Channel channel) in channels)
                {
                    if (processed.ContainsKey(channel.Id))
                    {
                        await nodesStream.WriteLineAsync($"{channel.Id},{channel.DisplayName},{channel.Size}");
                    }
                }
            }
            
            Console.WriteLine($"saved nodes in {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            
            await using StreamWriter edgesStream = File.CreateText($"./data/{dirName}/edges.csv");
            // await edgesStream.WriteLineAsync("source,target,weight");
            
            foreach ((string ch1, ConcurrentDictionary<string, int> intersection) in processed)
            {
                foreach ((string ch2, int weight) in intersection)
                {
                    await edgesStream.WriteLineAsync($"{ch1},{ch2},{weight}");
                }
            }
            
            Console.WriteLine($"saved edges in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"total time taken: {sw2.Elapsed.TotalSeconds}s");
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
            var shards = (int) Math.Ceiling(channels.Count / 100.0);
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
            if (length == 1) return list.Select(t => new[] {t});
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new[] {t2}));
        }
    }
}