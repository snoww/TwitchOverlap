using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChannelIntersection.Models;
using MathNet.Numerics.LinearAlgebra;
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
            // var channelIndexes = new Dictionary<string, int>();
            // var index = 0;

            IQueryable<Chatters> query = _context.Chatters.AsNoTracking().Where(x => x.Time >= DateTime.UtcNow.AddDays(-7));
            foreach (Chatters chatters in query)
            {
                foreach ((string _, List<string> channels) in chatters.Users)
                {
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
                            // channelIndexes[pair[0]] = index++;
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
                            // channelIndexes[pair[1]] = index++;
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
            }

            Console.WriteLine($"Calculated intersections in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            foreach (var (c1, intersects) in channelIntersections.Where(x => x.Key == "xqcow"))
            {
                foreach (var (c2, count) in intersects.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"{c1}:{c2}:{count}");
                }
                
                break;
            }

            // MatrixBuilder<float> builder = Matrix<float>.Build;
            // // create empty matrix
            // Matrix<float> matrix = builder.Sparse(channelIndexes.Count, channelIndexes.Count, 0);
            // foreach (var (c1, intersects) in channelIntersections)
            // {
            //     foreach (var (c2, count) in intersects.OrderByDescending(x => x.Value))
            //     {
            //         matrix[channelIndexes[c1], channelIndexes[c2]] = count;
            //     }
            // }
            //
            // Console.WriteLine($"Constructed sparse matrix in {sw.Elapsed.TotalSeconds}s");
            // sw.Restart();
            //
            // Console.WriteLine(matrix.ToString());
            // var aIndex = channelIndexes["xqcow"];
            // var bIndex = channelIndexes["ming"];
            // Console.WriteLine(matrix[aIndex, bIndex]);
            //
            // Matrix<float> norm = matrix.NormalizeColumns(2).NormalizeRows(2);
            // Matrix<float> sim = norm.TransposeAndMultiply(norm);
            // var a = (sim.ColumnSums() + sim.RowSums()).Sum();
            // Console.WriteLine(sim.ToString());
            // Console.WriteLine(sim[aIndex, bIndex]);
            // Console.WriteLine(sim[aIndex, bIndex] / ((a - sim.ColumnCount) / (sim.ColumnCount * (sim.ColumnCount - 1))));
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