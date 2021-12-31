using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchGraph;

public class AggregateDays
{
    private readonly Dictionary<string, HashSet<int>> _channelViewers = new();
    private readonly Dictionary<int, List<string>> _chatterChannels = new();

    private static readonly object AggregateLock = new();
    private static readonly object ChannelOverlapLock = new();

    public async Task<(Dictionary<string, Dictionary<string, int>> channelOverlap, Dictionary<string, int> channelUniqueChatters)> Aggregate(string[] files)
    {
        Console.WriteLine("beginning aggregation");
        var sw = new Stopwatch();
        sw.Start();
        await AggregateChatters(files);
        Console.WriteLine($"finished aggregation in {sw.Elapsed.TotalSeconds}s");
        sw.Restart();
        Console.WriteLine("beginning transpose");
        var channelUniqueCount = TransposeChatters();
        Console.WriteLine($"transposed in {sw.Elapsed.TotalSeconds}s");
        sw.Restart();
        Console.WriteLine("calculating overlap");
        var ret = CalculateOverlap();
        Console.WriteLine($"overlap calculated in {sw.Elapsed.TotalSeconds}s");
        return (ret, channelUniqueCount);
    }

    private async Task AggregateChatters(string[] files)
    {
        await Parallel.ForEachAsync(files, async (file, token) =>
        {
            Dictionary<string, List<string>> data;
            await using (FileStream fs = File.OpenRead(file))
            {
                data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(fs, cancellationToken: token) ?? new Dictionary<string, List<string>>();
            }

            foreach (var (user, channels) in data)
            {
                foreach (string channel in channels)
                {
                    lock (AggregateLock)
                    {
                        if (!_channelViewers.ContainsKey(channel))
                        {
                            _channelViewers[channel] = new HashSet<int>();
                        }
                        else
                        {
                            _channelViewers[channel].Add(user.GetHashCode());
                        }
                    }
                }
            }
        });
        
        Console.WriteLine($"aggregated {_channelViewers.Count} channels");
    }

    private Dictionary<string, int> TransposeChatters()
    {
        var filter = _channelViewers.OrderByDescending(x => x.Value.Count).Take(2000).ToList();
        foreach (var (channel, user) in filter)
        {
            foreach (int hash in user)
            {
                if (!_chatterChannels.ContainsKey(hash))
                {
                    _chatterChannels[hash] = new List<string> { channel };
                }
                else
                {
                    _chatterChannels[hash].Add(channel);
                }
            }
        }
        Console.WriteLine($"transposed {_chatterChannels.Count} chatters");
        return filter.ToDictionary(x => x.Key, y => y.Value.Count);
    }

    private Dictionary<string, Dictionary<string, int>> CalculateOverlap()
    {
        var channelOverlap = new Dictionary<string, Dictionary<string, int>>();
        
        var sw = new Stopwatch();
        Parallel.ForEach(_chatterChannels, x =>
        {
            var (_, channels) = x;
            if (channels.Count < 2)
            {
                return;
            }
            
            foreach (IEnumerable<string> combs in GetKCombs(channels, 2))
            {
                string[] pair = combs.ToArray();
                lock (ChannelOverlapLock)
                {
                    if (!channelOverlap.ContainsKey(pair[0]))
                    {
                        channelOverlap[pair[0]] = new Dictionary<string, int> { { pair[1], 1 } };
                    }
                    else
                    {
                        if (!channelOverlap[pair[0]].ContainsKey(pair[1]))
                        {
                            channelOverlap[pair[0]][pair[1]] = 1;
                        }
                        else
                        {
                            channelOverlap[pair[0]][pair[1]]++;
                        }
                    }
                }
            }
        });

        Console.WriteLine($"calculated intersection in {sw.Elapsed.TotalSeconds}s");

        return channelOverlap;
    }
    
    private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
    {
        if (length == 1) return list.Select(t => new[] { t });
        return GetKCombs(list, length - 1)
            .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                (t1, t2) => t1.Concat(new[] { t2 }));
    }
}