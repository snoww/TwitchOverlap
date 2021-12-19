using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using Microsoft.EntityFrameworkCore;

namespace ChannelIntersection
{
    public class HalfHourly
    {
        private readonly TwitchContext _context;
        private readonly Dictionary<string, List<string>> _chatters;
        private readonly Dictionary<string, Channel> _channels;
        private readonly DateTime _timestamp;

        private const int MinSharedViewers = 5;
        private const int MaxSharedChannels = 100;

        public HalfHourly(TwitchContext context, Dictionary<string, List<string>> chatters, Dictionary<string, Channel> channels, DateTime timestamp)
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
            
            // skip users that are present in more than 10 channels in the half hour aggregation
            // since these are most likely bots
            foreach ((string _, List<string> channels) in _chatters.Where(x => x.Value.Count < 10))
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
                if (!channelTotalOverlap.ContainsKey(ch.LoginName))
                {
                    return;
                }

                List<KeyValuePair<string, int>> filter = channelOverlap[ch.LoginName].OrderByDescending(y => y.Value)
                    .Where(y => y.Value >= MinSharedViewers)
                    .ToList();

                if (filter.Count < 1)
                {
                    return;
                }
                
                ch.Shared = channelTotalOverlap[ch.LoginName];
                overlapData.Add(new Overlap {
                    Timestamp = _timestamp, 
                    Channel = ch.Id, 
                    Shared = filter.Select(y => new ChannelOverlap 
                        {
                            Name = y.Key,
                            Shared = y.Value
                        })
                        .Take(MaxSharedChannels)
                        .ToList()
                });
            });
            
            _context.Channels.UpdateRange(_channels.Values);
            await _context.ChannelsHistories.AddRangeAsync(_channels.Values.Select(x => new ChannelHistory
            {
                Id = x.Id,
                Timestamp = x.LastUpdate,
                Viewers = x.Viewers,
                Chatters = x.Chatters,
                Shared = x.Shared,
            }));
            await _context.Overlaps.AddRangeAsync(overlapData);
            await _context.SaveChangesAsync();
            
            // remove old data
            DateTime thirtyDays = _timestamp.AddDays(-30);
            await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap where timestamp <= {thirtyDays}");
            await _context.Database.ExecuteSqlInterpolatedAsync($"delete from channel_history where timestamp <= {thirtyDays}");
            await _context.Database.ExecuteSqlRawAsync(@"
                delete
                from channel_history
                where exists(
                    select 1
                    from (
                        select r.timestamp, r.id
                        from (select h.timestamp,
                                     h.id,
                                     row_number() over (partition by h.id order by h.timestamp desc) as rank
                              from channel_history h) r
                        where rank > 2) b
                    where b.timestamp = channel_history.timestamp 
                      and b.id = channel_history.id)");
            
            Console.WriteLine($"inserted half hour data to database in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
        }
    }
}