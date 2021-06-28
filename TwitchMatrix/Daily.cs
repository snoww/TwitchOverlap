using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using Microsoft.EntityFrameworkCore;

namespace TwitchMatrix
{
    public class Daily
    {
        private readonly TwitchContext _context;
        private readonly Dictionary<string, HashSet<string>> _chatters = new();
        private readonly DateTime _timestamp;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _channelOverlap = new();
        private readonly ConcurrentDictionary<string, int> _channelTotalOverlap = new();
        private readonly ConcurrentDictionary<string, int> _channelUniqueChatters = new();

        private const int Limit = 500;
        private const string Dir = "chatters/";
        private const string Extension = ".json";

        public Daily(TwitchContext context, DateTime timestamp)
        {
            _context = context;
            _timestamp = timestamp;
        }

        public async Task Aggregate()
        {
            var sw = new Stopwatch();
            sw.Start();
            await Aggregate1Day();
            Console.WriteLine($"aggregated 1 day data in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            await Aggregate3Days();
            Console.WriteLine($"aggregated 3 day data in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            await Aggregate7Days();
            Console.WriteLine($"aggregated 7 day data in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            await Aggregate14Days();
            Console.WriteLine($"aggregated 14 day data in {sw.Elapsed.TotalSeconds}s");
            sw.Stop();
        }

        private async Task Aggregate1Day()
        {
            Console.WriteLine("loading 1 day data");
            await LoadData(GetFileNames(1));
            Console.WriteLine("aggregating 1 day data");
            Calculate(_chatters);
            await InsertDailyToDatabase();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
        }

        private async Task Aggregate3Days()
        {
            Console.WriteLine("loading 3 day data");
            await LoadData(GetFileNames(3));
            Console.WriteLine("aggregating 3 day data");
            Calculate(_chatters);
            await Insert3DaysToDatabase();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
        }

        private async Task Aggregate7Days()
        {
            Console.WriteLine("loading 7 day data");
            await LoadData(GetFileNames(7));
            Console.WriteLine("aggregating 7 day data");
            Calculate(_chatters);
            await Insert7DaysToDatabase();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
        }

        private async Task Aggregate14Days()
        {
            Console.WriteLine("loading 14 day data");
            await LoadData(GetFileNames(14));
            Console.WriteLine("aggregating 14 day data");
            Calculate(_chatters);
            await Insert14DaysToDatabase();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
        }

        private async Task<Dictionary<string, int>> FetchChannelIds()
        {
            return await _context.Channels.Where(x => _channelUniqueChatters.Keys.ToList().Contains(x.LoginName)).Select(x => new {x.LoginName, x.Id}).ToDictionaryAsync(x => x.LoginName, x => x.Id);
        }

        private IEnumerable<string> GetFileNames(int days)
        {
            var fileNames = new List<string>();
            if (days == 1)
            {
                fileNames.Add(Dir + _timestamp.AddDays(-1).ToShortDateString() + Extension);
            }
            else
            {
                int start;
                int end;
                switch (days)
                {
                    case 3:
                        start = 2;
                        end = 3;
                        break;
                    case 7:
                        start = 4;
                        end = 7;
                        break;
                    default:
                        start = 8;
                        end = 14;
                        break;
                }

                for (int i = start; i <= end; i++)
                {
                    fileNames.Add(Dir + _timestamp.AddDays(-i).ToShortDateString() + Extension);
                }
            }

            return fileNames;
        }

        private async Task LoadData(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                await using FileStream fs = File.OpenRead(file);
                foreach ((string username, HashSet<string> channels) in await JsonSerializer.DeserializeAsync<Dictionary<string, HashSet<string>>>(fs) ?? new Dictionary<string, HashSet<string>>())
                {
                    if (!_chatters.ContainsKey(username))
                    {
                        _chatters[username] = new HashSet<string>(channels);
                    }
                    else
                    {
                        _chatters[username].UnionWith(channels);
                    }
                }
            }
        }

        private void Calculate(Dictionary<string, HashSet<string>> chatters)
        {
            Console.WriteLine($"aggregating {chatters.Count:N0} chatters");
            Parallel.ForEach(chatters, x =>
            {
                (string _, HashSet<string> channels) = x;
                foreach (string channel in channels)
                {
                    if (!_channelUniqueChatters.ContainsKey(channel))
                    {
                        _channelUniqueChatters[channel] = 1;
                    }
                    else
                    {
                        _channelUniqueChatters[channel]++;
                    }

                    if (channels.Count < 2) break;

                    if (!_channelTotalOverlap.ContainsKey(channel))
                    {
                        _channelTotalOverlap[channel] = 1;
                    }
                    else
                    {
                        _channelTotalOverlap[channel]++;
                    }
                }

                if (channels.Count < 2)
                {
                    return;
                }

                foreach (IEnumerable<string> combs in Helper.GetKCombs(channels, 2))
                {
                    string[] pair = combs.ToArray();
                    if (!_channelOverlap.ContainsKey(pair[0]))
                    {
                        _channelOverlap[pair[0]] = new ConcurrentDictionary<string, int>();
                        _channelOverlap[pair[0]].TryAdd(pair[1], 1);
                    }
                    else
                    {
                        if (!_channelOverlap[pair[0]].ContainsKey(pair[1]))
                        {
                            _channelOverlap[pair[0]][pair[1]] = 1;
                        }
                        else
                        {
                            _channelOverlap[pair[0]][pair[1]]++;
                        }
                    }

                    if (!_channelOverlap.ContainsKey(pair[1]))
                    {
                        _channelOverlap[pair[1]] = new ConcurrentDictionary<string, int>();
                        _channelOverlap[pair[1]].TryAdd(pair[0], 1);
                    }
                    else
                    {
                        if (!_channelOverlap[pair[1]].ContainsKey(pair[0]))
                        {
                            _channelOverlap[pair[1]][pair[0]] = 1;
                        }
                        else
                        {
                            _channelOverlap[pair[1]][pair[0]]++;
                        }
                    }
                }
            });
        }

        private async Task InsertDailyToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapDaily>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapDaily
                {
                    Date = _timestamp.AddDays(-1),
                    Channel = channelId,
                    ChannelTotalOverlap = _channelTotalOverlap[channel],
                    ChannelTotalUnique = _channelUniqueChatters[channel],
                    Shared = _channelOverlap[channel]
                        .OrderByDescending(y => y.Value)
                        .Select(y => new ChannelOverlap
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .Take(Limit)
                        .ToList()
                });
            });

            await _context.OverlapsDaily.AddRangeAsync(overlapData);

            await _context.SaveChangesAsync();
        }

        private async Task Insert3DaysToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapRolling3Days>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapRolling3Days
                {
                    Date = _timestamp.AddDays(-1),
                    Channel = channelId,
                    ChannelTotalOverlap = _channelTotalOverlap[channel],
                    ChannelTotalUnique = _channelUniqueChatters[channel],
                    Shared = _channelOverlap[channel]
                        .OrderByDescending(y => y.Value)
                        .Select(y => new ChannelOverlap
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .Take(Limit)
                        .ToList()
                });
            });

            await _context.OverlapRolling3Days.AddRangeAsync(overlapData);

            await _context.SaveChangesAsync();
        }

        private async Task Insert7DaysToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapRolling7Days>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapRolling7Days
                {
                    Date = _timestamp.AddDays(-1),
                    Channel = channelId,
                    ChannelTotalOverlap = _channelTotalOverlap[channel],
                    ChannelTotalUnique = _channelUniqueChatters[channel],
                    Shared = _channelOverlap[channel]
                        .OrderByDescending(y => y.Value)
                        .Select(y => new ChannelOverlap
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .Take(Limit)
                        .ToList()
                });
            });

            await _context.OverlapRolling7Days.AddRangeAsync(overlapData);

            await _context.SaveChangesAsync();
        }


        private async Task Insert14DaysToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapRolling14Days>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapRolling14Days
                {
                    Date = _timestamp.AddDays(-1),
                    Channel = channelId,
                    ChannelTotalOverlap = _channelTotalOverlap[channel],
                    ChannelTotalUnique = _channelUniqueChatters[channel],
                    Shared = _channelOverlap[channel]
                        .OrderByDescending(y => y.Value)
                        .Select(y => new ChannelOverlap
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .Take(Limit)
                        .ToList()
                });
            });

            await _context.OverlapRolling14Days.AddRangeAsync(overlapData);

            await _context.SaveChangesAsync();
        }
    }
}