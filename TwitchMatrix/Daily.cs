using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using Microsoft.EntityFrameworkCore;

namespace TwitchMatrix
{
    public class Daily
    {
        private readonly TwitchContext _context;
        private readonly Dictionary<string, HashSet<string>> _chatters;
        private readonly DateTime _timestamp;

        private readonly Dictionary<string, Dictionary<string, int>> _channelOverlap = new();
        private readonly Dictionary<string, int> _channelTotalOverlap = new();
        private readonly Dictionary<string, int> _channelUniqueChatters = new();

        public Daily(TwitchContext context, Dictionary<string, HashSet<string>> chatters, DateTime timestamp)
        {
            _context = context;
            _chatters = chatters;
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
            sw.Restart();
            await Aggregate30Days();
            Console.WriteLine($"aggregated 30 day data in {sw.Elapsed.TotalSeconds}s");
            sw.Stop();
        }

        private async Task Aggregate1Day()
        {
            Calculate(_chatters);
            // insert to db
            await InsertDailyToDatabase();
        }

        private async Task Aggregate3Days()
        {
            Merge(_context.Chatters.AsNoTracking().OrderByDescending(x => x.Date).Skip(1).Take(2).Select(x => x.Users));
            Calculate(_chatters);
            await Insert3DaysToDatabase();
        }

        private async Task Aggregate7Days()
        {
            Merge(_context.Chatters.AsNoTracking().OrderByDescending(x => x.Date).Skip(3).Take(4).Select(x => x.Users));
            Calculate(_chatters);
            await Insert7DaysToDatabase();
        }

        private async Task Aggregate14Days()
        {
            Merge(_context.Chatters.AsNoTracking().OrderByDescending(x => x.Date).Skip(7).Take(7).Select(x => x.Users));
            Calculate(_chatters);
            await Insert14DaysToDatabase();
        }

        private async Task Aggregate30Days()
        {
            Merge(_context.Chatters.AsNoTracking().OrderByDescending(x => x.Date).Skip(14).Take(16).Select(x => x.Users));
            Calculate(_chatters);
            await Insert30DaysToDatabase();
        }

        private async Task<Dictionary<string, int>> FetchChannelIds()
        {
            return await _context.Channels.Where(x => _channelUniqueChatters.Keys.ToList().Contains(x.LoginName)).Select(x => new {x.LoginName, x.Id}).ToDictionaryAsync(x => x.LoginName, x => x.Id);
        }

        private void Merge(IEnumerable<Dictionary<string, HashSet<string>>> query)
        {
            foreach (Dictionary<string, HashSet<string>> chatters in query)
            {
                foreach ((string username, HashSet<string> channels) in chatters)
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
            foreach ((string _, HashSet<string> channels) in chatters)
            {
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
                    continue;
                }

                foreach (IEnumerable<string> combs in Helper.GetKCombs(channels, 2))
                {
                    string[] pair = combs.ToArray();
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
        }
        
        private async Task InsertDailyToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapDaily>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapDaily {
                    Date = _timestamp, 
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
                overlapData.Add(new OverlapRolling3Days {
                    Date = _timestamp, 
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
                overlapData.Add(new OverlapRolling7Days {
                    Date = _timestamp, 
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
                overlapData.Add(new OverlapRolling14Days {
                    Date = _timestamp, 
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
                        .ToList()
                });
            });
            
            await _context.OverlapRolling14Days.AddRangeAsync(overlapData);
            
            await _context.SaveChangesAsync();
        }
        
        private async Task Insert30DaysToDatabase()
        {
            var overlapData = new ConcurrentBag<OverlapRolling30Days>();

            Parallel.ForEach(await FetchChannelIds(), x =>
            {
                (string channel, int channelId) = x;
                overlapData.Add(new OverlapRolling30Days {
                    Date = _timestamp, 
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
                        .ToList()
                });
            });
            
            await _context.OverlapRolling30Days.AddRangeAsync(overlapData);
            
            await _context.SaveChangesAsync();
        }
    }
}