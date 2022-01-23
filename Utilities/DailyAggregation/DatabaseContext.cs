using System.Collections.Concurrent;
using DailyAggregation.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DailyAggregation;

public class DatabaseContext : IDisposable
{
    private readonly TwitchContext _context;
    private readonly DateOnly _date;
    
    private const int MinSharedViewers = 5;
    private const int OneDayLimit = 100;
    private const int ThreeDayLimit = 200;
    private const int SevenDayLimit = 300;
    private const int DeleteAfter = -40;

    public DatabaseContext(string connectionString, DateTime timestamp)
    {
        _context = new TwitchContext(connectionString);
        _date = DateOnly.FromDateTime(timestamp);
    }

    public bool AlreadyAggregated()
    {
        return _context.OverlapsDaily.Any(x => x.Date == DateOnly.FromDateTime(DateTime.UtcNow));
    }
    
    private async Task<Dictionary<string, int>> FetchChannelIds(Dictionary<string, int> uniqueChatters)
    {
        return await _context.Channels.Where(x => uniqueChatters.Keys.ToList().Contains(x.LoginName)).Select(x => new {x.LoginName, x.Id}).ToDictionaryAsync(x => x.LoginName, x => x.Id);
    }
    
    
    public async Task InsertDailyToDatabase(Dictionary<string, int> uniqueChatters, IReadOnlyDictionary<string, int> totalOverlap, ConcurrentDictionary<string, ConcurrentDictionary<string, int>> overlap)
    {
        var overlapData = new ConcurrentBag<OverlapDaily>();

        Parallel.ForEach(await FetchChannelIds(uniqueChatters), x =>
        {
            (string channel, int channelId) = x;
            overlapData.Add(new OverlapDaily
            {
                Date = _date,
                Channel = channelId,
                ChannelTotalOverlap = totalOverlap[channel],
                ChannelTotalUnique = uniqueChatters[channel],
                Shared = overlap[channel]
                    .OrderByDescending(y => y.Value)
                    .Where(y => y.Value >= MinSharedViewers)
                    .Select(y => new ChannelOverlap
                    {
                        Name = y.Key,
                        Shared = y.Value
                    })
                    .Take(OneDayLimit)
                    .ToList()
            });
        });

        await _context.BulkInsertAsync(overlapData.ToList());
        await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_daily where date <= {_date.AddDays(DeleteAfter)}");
        await _context.SaveChangesAsync();
    }
    
    public async Task Insert3DayToDatabase(Dictionary<string, int> uniqueChatters, IReadOnlyDictionary<string, int> totalOverlap, ConcurrentDictionary<string, ConcurrentDictionary<string, int>> overlap)
    {
        var overlapData = new ConcurrentBag<OverlapRolling3Days>();

        Parallel.ForEach(await FetchChannelIds(uniqueChatters), x =>
        {
            (string channel, int channelId) = x;
            overlapData.Add(new OverlapRolling3Days
            {
                Date = _date,
                Channel = channelId,
                ChannelTotalOverlap = totalOverlap[channel],
                ChannelTotalUnique = uniqueChatters[channel],
                Shared = overlap[channel]
                    .OrderByDescending(y => y.Value)
                    .Where(y => y.Value >= MinSharedViewers)
                    .Select(y => new ChannelOverlap
                    {
                        Name = y.Key,
                        Shared = y.Value
                    })
                    .Take(ThreeDayLimit)
                    .ToList()
            });
        });

        await _context.BulkInsertAsync(overlapData.ToList());
        await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_daily where date <= {_date.AddDays(DeleteAfter)}");
        await _context.SaveChangesAsync();
    }
    
    public async Task Insert7DayToDatabase(Dictionary<string, int> uniqueChatters, IReadOnlyDictionary<string, int> totalOverlap, ConcurrentDictionary<string, ConcurrentDictionary<string, int>> overlap)
    {
        var overlapData = new ConcurrentBag<OverlapRolling7Days>();

        Parallel.ForEach(await FetchChannelIds(uniqueChatters), x =>
        {
            (string channel, int channelId) = x;
            overlapData.Add(new OverlapRolling7Days
            {
                Date = _date,
                Channel = channelId,
                ChannelTotalOverlap = totalOverlap[channel],
                ChannelTotalUnique = uniqueChatters[channel],
                Shared = overlap[channel]
                    .OrderByDescending(y => y.Value)
                    .Where(y => y.Value >= MinSharedViewers)
                    .Select(y => new ChannelOverlap
                    {
                        Name = y.Key,
                        Shared = y.Value
                    })
                    .Take(SevenDayLimit)
                    .ToList()
            });
        });

        await _context.BulkInsertAsync(overlapData.ToList());
        await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_daily where date <= {_date.AddDays(DeleteAfter)}");
        await _context.SaveChangesAsync();
    }
    
    public async Task Insert30DayToDatabase(Dictionary<string, int> uniqueChatters, IReadOnlyDictionary<string, int> totalOverlap, ConcurrentDictionary<string, ConcurrentDictionary<string, int>> overlap)
    {
        var overlapData = new ConcurrentBag<OverlapRolling30Days>();

        Parallel.ForEach(await FetchChannelIds(uniqueChatters), x =>
        {
            (string channel, int channelId) = x;
            overlapData.Add(new OverlapRolling30Days
            {
                Date = _date,
                Channel = channelId,
                ChannelTotalOverlap = totalOverlap[channel],
                ChannelTotalUnique = uniqueChatters[channel],
                Shared = overlap[channel]
                    .OrderByDescending(y => y.Value)
                    .Where(y => y.Value >= MinSharedViewers)
                    .Select(y => new ChannelOverlap
                    {
                        Name = y.Key,
                        Shared = y.Value
                    })
                    .Take(SevenDayLimit)
                    .ToList()
            });
        });

        await _context.BulkInsertAsync(overlapData.ToList());
        await _context.Database.ExecuteSqlInterpolatedAsync($"delete from overlap_daily where date <= {_date.AddDays(DeleteAfter)}");
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}