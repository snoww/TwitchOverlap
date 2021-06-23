using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChannelIntersection.Models;

namespace TwitchMatrix
{
    public class HalfHourly
    {
        private readonly TwitchContext _context;
        private readonly Dictionary<string, HashSet<string>> _chatters;
        private readonly Dictionary<string, Channel> _channels;
        private readonly DateTime _timestamp;

        public HalfHourly(TwitchContext context, Dictionary<string, HashSet<string>> chatters, Dictionary<string, Channel> channels, DateTime timestamp)
        {
            _context = context;
            _chatters = chatters;
            _channels = channels;
            _timestamp = timestamp;
        }
        
        public async Task CalculateShared()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            var channelOverlap = new Dictionary<string, Dictionary<string, int>>();

            var channelTotalOverlap = new Dictionary<string, int>();
            var channelUniqueChatters = new Dictionary<string, int>();
            
            foreach ((string _, HashSet<string> channels) in _chatters)
            {
                foreach (string channel in channels)
                {
                    if (!channelUniqueChatters.ContainsKey(channel))
                    {
                        channelUniqueChatters[channel] = 1;
                    }
                    else
                    {
                        channelUniqueChatters[channel]++;
                    }

                    if (channels.Count < 2) break;

                    if (!channelTotalOverlap.ContainsKey(channel))
                    {
                        channelTotalOverlap[channel] = 1;
                    }
                    else
                    {
                        channelTotalOverlap[channel]++;
                    }
                }
                
                if (channels.Count < 2)
                {
                    continue;
                }

                foreach (IEnumerable<string> combs in Helper.GetKCombs(channels, 2))
                {
                    string[] pair = combs.ToArray();
                    if (!channelOverlap.ContainsKey(pair[0]))
                    {
                        channelOverlap[pair[0]] = new Dictionary<string, int> {{pair[1], 1}};
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

                    if (!channelOverlap.ContainsKey(pair[1]))
                    {
                        channelOverlap[pair[1]] = new Dictionary<string, int> {{pair[0], 1}};
                    }
                    else
                    {
                        if (!channelOverlap[pair[1]].ContainsKey(pair[0]))
                        {
                            channelOverlap[pair[1]][pair[0]] = 1;
                        }
                        else
                        {
                            channelOverlap[pair[1]][pair[0]]++;
                        }
                    }
                }
            }
            
            Console.WriteLine($"calculated intersection in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            
            var overlapData = new ConcurrentBag<Overlap>();

            Parallel.ForEach(_channels, x =>
            {
                (_, Channel ch) = x;
                ch.Shared = channelTotalOverlap[ch.LoginName];
                overlapData.Add(new Overlap {
                    Timestamp = _timestamp, 
                    Channel = ch.Id, 
                    Shared = channelOverlap[ch.LoginName]
                        .OrderByDescending(y => y.Value)
                        .Select(y => new ChannelOverlap
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .ToList()
                });
            });
            
            _context.Channels.UpdateRange(_channels.Values.ToList());
            await _context.Overlaps.AddRangeAsync(overlapData);
            
            await _context.SaveChangesAsync();
            Console.WriteLine($"added half-hourly intersection to database in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
        }
    }
}