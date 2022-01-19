using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using DailyAggregation.Models;
// ReSharper disable InconsistentlySynchronizedField

namespace DailyAggregation;

public class Aggregate : IDisposable
{
    private const string S3BucketName = "twitch-overlap";
    private const string S3KeyFormat = "chatters/{0}.json.gz";
    private static readonly string TempDir = Path.GetTempPath();
    private static readonly string FilenameFormat = TempDir + "{0}.json";

    private readonly Dictionary<string, HashSet<int>> _channelViewers = new();
    private readonly Dictionary<int, List<string>> _chatterChannels = new();
    private readonly Dictionary<string, int> _channelTotalOverlap = new();

    private static readonly object AggregateLock = new();
    private static readonly object ChannelOverlapLock = new();
    private static readonly object ChannelTotalOverlapCountLock = new();

    private const int MaxChannels = 4000;
    
    private static readonly DateTime Timestamp = DateTime.UtcNow.AddDays(-1);

    private readonly IAmazonS3 _client;
    private readonly DatabaseContext _database;

    public Aggregate()
    {
        Dictionary<string, string> config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.OpenRead("config.json")) ?? new Dictionary<string, string>();
        if (config.Count == 0)
        {
            Console.WriteLine("empty config, exiting");
            Environment.Exit(-1);
        }
        
        _client = new AmazonS3Client(config["S3AccessKey"], config["S3SecretKey"], RegionEndpoint.USEast2);
        _database = new DatabaseContext(config["POSTGRES"], Timestamp);
    }

    public async Task BeginAggregation()
    {
        var sw = new Stopwatch();
        sw.Start();

        var dates = new List<string>();
        var map = new[] { 1, 3, 7, 30 };
        var types = new AggregateOverlap[]
            { new OverlapDaily(), new OverlapRolling3Days(), new OverlapRolling7Days(), new OverlapRolling30Days() };
        
        for (int i = 0; i < 4; i++)
        {
            dates = GetFilenames(map[i]);
            Console.WriteLine($"retrieving {map[i]} day(s) of chatters ...");
            await RetrieveChatters(dates);
            Console.WriteLine($"finished retrieving chatters, took {sw.Elapsed.TotalSeconds:N4}s");
            sw.Restart();
        
            Console.WriteLine($"aggregating {map[i]} day(s) of chatters ...");
            await AggregateChatters(dates);
            Console.WriteLine($"finished aggregation in {sw.Elapsed:mm\\:ss}");
            sw.Restart();
        
            Console.WriteLine("beginning transposition ...");
            var totalUnique = TransposeChatters();
            _channelViewers.Clear();
            Console.WriteLine($"finished transposition in {sw.Elapsed:mm\\:ss}");
            sw.Restart();
        
            Console.WriteLine("beginning overlap calculation ...");
            var overlap = CalculateOverlap();
            Console.WriteLine($"finished overlap calculation in {sw.Elapsed:mm\\:ss}");
            sw.Restart();
            
            switch (i)
            {
                case 0:
                    await _database.InsertDailyToDatabase(totalUnique, _channelTotalOverlap, overlap);
                    break;
                case 1:
                    await _database.Insert3DayToDatabase(totalUnique, _channelTotalOverlap, overlap);
                    break;
                case 2:
                    await _database.Insert7DayToDatabase(totalUnique, _channelTotalOverlap, overlap);
                    break;
                case 3:
                    await _database.Insert30DayToDatabase(totalUnique, _channelTotalOverlap, overlap);
                    break;
            }
            
            _channelTotalOverlap.Clear();
            _chatterChannels.Clear();
        }

        string? oldest = dates.OrderBy(x => x).FirstOrDefault();
        try
        {
            File.Delete(string.Format(FilenameFormat, oldest));
        }
        catch (Exception)
        {
            //
        }
    }

    private async Task AggregateChatters(List<string> files)
    {
        await Parallel.ForEachAsync(files, async (file, token) =>
        {
            Dictionary<string, List<string>> data;

            if (!File.Exists(string.Format(FilenameFormat, file)))
            {
                Console.WriteLine("not found " + file);
                // file not found
                return;
            }
            await using (FileStream fs = File.OpenRead(string.Format(FilenameFormat, file)))
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
        
        Console.WriteLine($"aggregated {_channelViewers.Count:N0} channels");
    }
    
    private Dictionary<string, int> TransposeChatters()
    {
        var filter = _channelViewers.OrderByDescending(x => x.Value.Count).Take(MaxChannels).ToList();
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
        Console.WriteLine($"transposed {_chatterChannels.Count:N0} chatters");
        return filter.ToDictionary(x => x.Key, y => y.Value.Count);
    }

    private Dictionary<string, Dictionary<string, int>> CalculateOverlap()
    {
        var channelOverlap = new Dictionary<string, Dictionary<string, int>>();
        
        Parallel.ForEach(_chatterChannels, x =>
        {
            var (_, channels) = x;
            if (channels.Count < 2)
            {
                return;
            }
            
            foreach (string channel in channels)
            {
                // appears in multiple channels
                // increment overlap chatter count
                
                lock (ChannelTotalOverlapCountLock)
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
                
                lock (ChannelOverlapLock)
                {
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
        });
        
        return channelOverlap;
    }

    private async Task RetrieveChatters(List<string> dates)
    {
        foreach (string date in dates)
        {
            if (!File.Exists(string.Format(FilenameFormat, date)))
            {
                try
                {
                    await DownloadChatters(date);
                }
                catch (Exception)
                {
                    // file not found, ignored
                    Console.WriteLine("data for " + date + " not found. skipping.");
                    continue;
                }
                
                DecompressFile(date);
            }
        }
    }
    
    private async Task DownloadChatters(string date)
    {
        using var transfer = new TransferUtility(_client);
        await transfer.DownloadAsync(string.Format(FilenameFormat, date) + ".gz", S3BucketName, string.Format(S3KeyFormat, date));
    }
    
    private static void DecompressFile(string date)
    {
        var compressedFileName = string.Format(FilenameFormat, date) + ".gz";
        using FileStream compressedFileStream = File.Open(compressedFileName, FileMode.Open);
        using FileStream outputFileStream = File.Create(string.Format(FilenameFormat, date));
        using (var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress))
        {
            decompressor.CopyTo(outputFileStream);
        }
        
        File.Delete(compressedFileName);
    }

    private static List<string> GetFilenames(int days)
    {
        var dates = new List<string>();

        if (days == 1)
        {
            dates.Add(Timestamp.ToString("yyyy-MM-dd"));
        }
        else
        {
            int end;
            switch (days)
            {
                case 3:
                    end = 3;
                    break;
                case 7:
                    end = 7;
                    break;
                case 14:
                    end = 14;
                    break;
                default: // case 30:
                    end = 30;
                    break;
            }

            for (int i = 0; i < end; i++)
            {
                dates.Add(Timestamp.AddDays(-i).ToString("yyyy-MM-dd"));
            }
        }

        return dates;
    }
    
    private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
    {
        if (length == 1) return list.Select(t => new[] { t });
        return GetKCombs(list, length - 1)
            .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                (t1, t2) => t1.Concat(new[] { t2 }));
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}