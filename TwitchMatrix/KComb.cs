using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;

namespace TwitchMatrix
{
    public class KComb
    {
        private readonly TwitchContext _context;

        public KComb(TwitchContext context)
        {
            _context = context;
        }

        public void DoStuff()
        {
            var sw = new Stopwatch();
            sw.Start();

            var channelIntersections = new Dictionary<string, Dictionary<string, int>>();
            var mergedChatters = new Dictionary<string, HashSet<string>>();
            
            foreach (var chatters in _context.Chatters.AsNoTracking().Where(x => x.Time >= DateTime.UtcNow.AddDays(-7)).Select(x => x.Users))
            {
                foreach ((string username, HashSet<string> channels) in chatters)
                {
                    if (!mergedChatters.ContainsKey(username))
                    {
                        mergedChatters[username] = new HashSet<string>(channels);
                    }
                    else
                    {
                        mergedChatters[username].UnionWith(channels);
                    }
                }
            }
            
            Console.WriteLine($"merged chatters in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            var channelTotalOverlap = new Dictionary<string, int>();
            var channelUniqueChatters = new Dictionary<string, int>();

            var channelIndexes = new Dictionary<string, int>();
            var index = 0;

            foreach ((string _, HashSet<string> channels) in mergedChatters)
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
                    
                    if (!channelIndexes.ContainsKey(channel))
                    {
                        channelIndexes[channel] = index++;
                    }

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

                foreach (IEnumerable<string> combs in GetKCombs(channels, 2))
                {
                    string[] pair = combs.ToArray();
                    if (!channelIntersections.ContainsKey(pair[0]))
                    {
                        channelIntersections[pair[0]] = new Dictionary<string, int> {{pair[1], 1}};
                    }
                    else
                    {
                        if (!channelIntersections[pair[0]].ContainsKey(pair[1]))
                        {
                            channelIntersections[pair[0]][pair[1]] = 1;
                        }
                        else
                        {
                            channelIntersections[pair[0]][pair[1]]++;
                        }
                    }

                    if (!channelIntersections.ContainsKey(pair[1]))
                    {
                        channelIntersections[pair[1]] = new Dictionary<string, int> {{pair[0], 1}};
                    }
                    else
                    {
                        if (!channelIntersections[pair[1]].ContainsKey(pair[0]))
                        {
                            channelIntersections[pair[1]][pair[0]] = 1;
                        }
                        else
                        {
                            channelIntersections[pair[1]][pair[0]]++;
                        }
                    }
                }
            }
            
            Console.WriteLine($"calculated intersection in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            
            foreach (var (c1, intersects) in channelIntersections.Where(x => x.Key == "xqcow"))
            {
                Console.WriteLine($"total unique chatters: {channelUniqueChatters[c1]}");
                Console.WriteLine($"total overlap: {channelTotalOverlap[c1]}");
                foreach (var (c2, count) in intersects.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"{c1}:{c2}:{count}:{(double) count/channelTotalOverlap[c1]:P2}");
                }
                
                break;
            }

            // MatrixBuilder<float> builder = Matrix<float>.Build;
            // // create empty matrix
            // Matrix<float> matrix = builder.Dense(channelIndexes.Count, channelIndexes.Count, 0);
            // foreach (var (c1, intersects) in channelIntersections)
            // {
            //     // matrix[channelIndexes[c1], channelIndexes[c1]] = channelUniqueChatters[c1];
            //     foreach (var (c2, count) in intersects.OrderByDescending(x => x.Value))
            //     {
            //         matrix[channelIndexes[c1], channelIndexes[c2]] = count;
            //     }
            // }
            //
            // DelimitedWriter.Write("matrix.csv", matrix, ",");
            //
            // Console.WriteLine($"built dense matrix in {sw.Elapsed.TotalSeconds}s");
            // sw.Restart();
            //
            // // Console.WriteLine(matrix.ToString());
            // int aIndex = channelIndexes["xqcow"];
            // int bIndex = channelIndexes["mizkif"];
            // Console.WriteLine($"aIndex: {aIndex}, bIndex: {bIndex}");
            // // Console.WriteLine(matrix[aIndex, bIndex]);
            //
            // Matrix<float> norm = matrix.NormalizeColumns(2).NormalizeRows(2);
            // DelimitedWriter.Write("norm.csv", norm, ",");
            //
            // var sim = norm.TransposeAndMultiply(norm);
            // DelimitedWriter.Write("cosine.csv", sim, ",");
            //
            // float sum = sim.ColumnSums().Sum();
            // float average = (sum - sim.ColumnCount) / (sim.ColumnCount * (sim.ColumnCount - 1));
            // Console.WriteLine($"sum: {sum}, average: {average}");
            // var cSim = sim.Column(aIndex).Enumerate().Select(x => x / average).ToArray();
            // Console.WriteLine(matrix.Column(aIndex)[bIndex]);
            // Console.WriteLine($"matrix max raw: {matrix.Column(aIndex).Maximum()}");
            // Console.WriteLine(norm.Column(aIndex)[bIndex]);
            // Console.WriteLine($"norm max raw: {norm.Column(aIndex).Maximum()}");
            // Console.WriteLine(sim.Column(aIndex)[bIndex]);
            //
            // foreach (var (ch, i) in channelIndexes)
            // {
            //     Console.WriteLine($"{ch}:{sim[aIndex, i]/average}");
            // }
            // Console.WriteLine($"sim max raw: {sim.Column(aIndex).OrderByDescending(x => x).Skip(1).FirstOrDefault()}");
            // Console.WriteLine(cSim[bIndex]);
            // Console.WriteLine($"max averaged: {cSim.OrderByDescending(x => x).Skip(1).FirstOrDefault()}");
            // foreach (float f in cSim)
            // {
            //     Console.WriteLine(f);
            // }
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