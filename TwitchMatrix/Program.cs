using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using MathNet.Numerics.LinearAlgebra;

namespace TwitchMatrix
{
    public class Program
    {
        private static readonly HttpClient Http = new();
        private static readonly ConcurrentBag<string> Chatters = new();
        private static readonly string TwitchToken = Environment.GetEnvironmentVariable("TWITCH_TOKEN");
        private static readonly string TwitchClient = Environment.GetEnvironmentVariable("TWITCH_CLIENT");


        public static async Task Main(string[] args)
        {
            Console.WriteLine("starting twitch matrix");
            var sw = new Stopwatch();
            sw.Start();
            Dictionary<string, Channel> topChannels = await GetTopChannels();
            Console.WriteLine($"Retrieved {topChannels.Count} channels in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            var channelChatters = new Dictionary<string, HashSet<string>>();

            IEnumerable<Task> processTasks = topChannels.Select(async channel =>
            {
                (string name, Channel ch) = channel;
                HashSet<string> chChatters = await GetChatters(ch);
                if (chChatters == null)
                {
                    return;
                }

                channelChatters.TryAdd(name, chChatters);
            });

            await Task.WhenAll(processTasks);

            Console.WriteLine($"Retrieved {Chatters.Count} chatters in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            // list of ALL unique chatters retrieved
            string[] chatters = Chatters.ToHashSet().ToArray();
            // list of ALL channels retrieved
            string[] channels = channelChatters.Select(x => x.Key).ToArray();

            int rows = channelChatters.Count;
            int columns = chatters.Length;

            MatrixBuilder<float> builder = Matrix<float>.Build;
            // create empty matrix
            Matrix<float> matrix = builder.Sparse(rows, columns, 0);
            var rand = new Random();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    // if channel[i] contains chatter[j]
                    // set matrix[i, j] to 1
                    if (channelChatters[channels[i]].Contains(chatters[j]))
                    {
                        matrix[i, j] = 1 * (rand.Next(1,100) > 10 ? 1 : rand.Next(5));
                    }
                }
            }

            Console.WriteLine($"Built sparse matrix in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            // product = matrix * matrix.transpose()
            // product is n x n matrix where (c_0, c_1) contains the overlap between channel c_0 and c_1
            Matrix<float> product = matrix.TransposeAndMultiply(matrix);
            Console.WriteLine($"Transpose and multiply in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            
            
            Console.WriteLine(product.ToString());
        }

        private static async Task<Dictionary<string, Channel>> GetTopChannels()
        {
            var channels = new Dictionary<string, Channel>();
            var pageToken = string.Empty;
            do
            {
                using var request = new HttpRequestMessage();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TwitchToken);
                request.Headers.Add("Client-Id", TwitchClient);
                request.RequestUri = string.IsNullOrWhiteSpace(pageToken)
                    ? new Uri("https://api.twitch.tv/helix/streams?first=100")
                    : new Uri($"https://api.twitch.tv/helix/streams?first=100&after={pageToken}");

                using HttpResponseMessage response = await Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    pageToken = json.RootElement.GetProperty("pagination").GetProperty("cursor").GetString();
                    JsonElement.ArrayEnumerator channelEnumerator = json.RootElement.GetProperty("data").EnumerateArray();
                    foreach (JsonElement channel in channelEnumerator)
                    {
                        int viewerCount = channel.GetProperty("viewer_count").GetInt32();
                        if (viewerCount < 1500)
                        {
                            pageToken = null;
                            break;
                        }

                        string login = channel.GetProperty("user_login").GetString()?.ToLowerInvariant();
                        var dbChannel = new Channel(login, channel.GetProperty("user_name").GetString(), channel.GetProperty("game_name").GetString(), viewerCount, DateTime.Now);

                        channels.TryAdd(login, dbChannel);
                    }
                }
            } while (pageToken != null);

            return channels;
        }


        private static async Task<HashSet<string>> GetChatters(Channel channel)
        {
            Stream stream;
            try
            {
                stream = await Http.GetStreamAsync($"https://tmi.twitch.tv/group/user/{channel.LoginName}/chatters");
            }
            catch
            {
                Console.WriteLine($"Could not retrieve chatters for {channel.LoginName}");
                return null;
            }

            using JsonDocument response = await JsonDocument.ParseAsync(stream);
            await stream.DisposeAsync();

            int chatters = response.RootElement.GetProperty("chatter_count").GetInt32();
            if (chatters < 500)
            {
                return null;
            }

            channel.Chatters = chatters;
            var chatterList = new HashSet<string>(chatters);
            JsonElement.ObjectEnumerator viewerTypes = response.RootElement.GetProperty("chatters").EnumerateObject();
            foreach (JsonProperty viewerType in viewerTypes)
            {
                foreach (JsonElement viewer in viewerType.Value.EnumerateArray())
                {
                    string username = viewer.GetString()?.ToLower();
                    if (username == null || username.EndsWith("bot", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    chatterList.Add(username);
                    Chatters.Add(username);
                }
            }

            return chatterList;
        }
    }
}