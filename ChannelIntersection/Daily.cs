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

namespace ChannelIntersection
{
    public class Daily
    {
        private readonly TwitchContext _context;
        private readonly Dictionary<string, HashSet<string>> _chatters = new();
        private readonly DateTime _timestamp;

        private readonly Dictionary<string, Dictionary<string, int>> _channelOverlap = new();
        private readonly Dictionary<string, int> _channelTotalOverlap = new();
        private readonly Dictionary<string, int> _channelUniqueChatters = new();

        private readonly object _channelOverlapLock = new();
        private readonly object _channelTotalOverlapCountLock = new();
        private readonly object _channelUniqueChatterCountLock = new();

        private const int OneDayLimit = 100;
        private const int ThreeDayLimit = 200;
        private const int SevenDayLimit = 300;
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
            sw.Stop();
        }

        private async Task Aggregate1Day()
        {
            Console.WriteLine("loading 1 day data");
            await LoadData(GetFileNames(1));
            Console.WriteLine("aggregating 1 day data");
            Calculate(_chatters);
            await InsertDailyToDatabase();
            _channelOverlap.Clear();
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
            _channelOverlap.Clear();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
        }

        private async Task Aggregate7Days()
        {
            Console.WriteLine("loading 7 day data");
            string[] fileNames = GetFileNames(7).ToArray();
            await LoadData(fileNames);
            Console.WriteLine("aggregating 7 day data");
            Calculate(_chatters);
            await Insert7DaysToDatabase();
            _channelOverlap.Clear();
            _channelTotalOverlap.Clear();
            _channelUniqueChatters.Clear();
            await ArchiveOldest(fileNames[^1]);
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
                    default: // case 14:
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

                Dictionary<string, HashSet<string>> data;
                await using (FileStream fs = File.OpenRead(file))
                {
                    data = await JsonSerializer.DeserializeAsync<Dictionary<string, HashSet<string>>>(fs) ?? new Dictionary<string, HashSet<string>>();
                }

                foreach ((string username, HashSet<string> channels) in data)
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
                if (channels.Count < 2)
                {
                    // only appear in one channel
                    // increment unique chatter count in channel
                    foreach (string channel in channels)
                    {
                        lock (_channelUniqueChatterCountLock)
                        {
                            if (!_channelUniqueChatters.ContainsKey(channel))
                            {
                                _channelUniqueChatters[channel] = 1;
                            }
                            else
                            {
                                _channelUniqueChatters[channel]++;
                            }
                        }
                    }

                    return;
                }

                foreach (string channel in channels)
                {
                    // appears in multiple channels
                    // increment unique chatter count
                    // increment overlap chatter count
                    lock (_channelUniqueChatterCountLock)
                    {
                        if (!_channelUniqueChatters.ContainsKey(channel))
                        {
                            _channelUniqueChatters[channel] = 1;
                        }
                        else
                        {
                            _channelUniqueChatters[channel]++;
                        }
                    }

                    lock (_channelTotalOverlapCountLock)
                    {
                        if (!_channelTotalOverlap.ContainsKey(channel))
                        {
                            _channelTotalOverlap[channel] = 1;
                        }
                        else
                        {
                            _channelTotalOverlap[channel]++;
                        }
                    }
                }

                // loop over each combination of channels, then for each channel pair, increment their overlaps
                // e.g. channels = ["xqcow", "mizkif", "summit1g"]
                // combinations = [["xqcow", "mizkif"], ["xqcow", "summit1g"], ["mizkif", "summit1g"]]
                // and for each pair, we increment the overlap count
                foreach (IEnumerable<string> combs in Helper.GetKCombs(channels, 2))
                {
                    string[] pair = combs.ToArray();
                    lock (_channelOverlapLock)
                    {
                        if (!_channelOverlap.ContainsKey(pair[0]))
                        {
                            _channelOverlap[pair[0]] = new Dictionary<string, int> {{pair[1], 1}};
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
                            _channelOverlap[pair[1]] = new Dictionary<string, int> {{pair[0], 1}};
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
                        .Take(OneDayLimit)
                        .ToList()
                });
            });

            await _context.OverlapsDaily.AddRangeAsync(overlapData);
            await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_daily where timestamp <= {_timestamp.AddDays(-14)}");
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
                        .Take(ThreeDayLimit)
                        .ToList()
                });
            });

            await _context.OverlapRolling3Days.AddRangeAsync(overlapData);
            await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_rolling_3_days where timestamp <= {_timestamp.AddDays(-14)}");
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
                        .Take(SevenDayLimit)
                        .ToList()
                });
            });

            await _context.OverlapRolling7Days.AddRangeAsync(overlapData);
            await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_rolling_7_days where timestamp <= {_timestamp.AddDays(-14)}");
            await _context.SaveChangesAsync();
        }

        private static async Task ArchiveOldest(string oldestFile)
        {
            if (!File.Exists(oldestFile))
            {
                return;
            }

            using var proc = new Process {StartInfo = {UseShellExecute = false, FileName = "gzip", Arguments = oldestFile}};
            proc.Start();
            await proc.WaitForExitAsync();
        }
    }
}