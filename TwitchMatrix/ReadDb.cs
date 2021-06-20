using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChannelIntersection.Models;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TwitchMatrix
{
    public class ReadDb
    {
        private readonly TwitchContext _context;

        public ReadDb(TwitchContext context)
        {
            _context = context;
        }

        public void Read()
        {
            var sw = new Stopwatch();
            sw.Start();

            IQueryable<Chatters> query = _context.Chatters.AsNoTracking().Where(x => x.Time >= DateTime.UtcNow.AddDays(-7));

            var userData = new Dictionary<string, Dictionary<string, int>>();
            var userIndex = new Dictionary<string, int>();
            var index = 0;

            var allChannels = new Dictionary<string, int>();
            var channelIndex = 0;

            foreach (Chatters chatters in query)
            {
                foreach ((string username, List<string> channels) in chatters.Users)
                {
                    if (!userData.ContainsKey(username))
                    {
                        userData[username] = new Dictionary<string, int>();
                        userData[username] = channels.ToDictionary(x => x, _ => 1);
                        userIndex[username] = index++;
                    }
                    else
                    {
                        foreach (string channel in channels)
                        {
                            if (!userData[username].ContainsKey(channel))
                            {
                                userData[username][channel] = 1;
                            }
                            else
                            {
                                userData[username][channel] += 1;
                            }
                        }
                    }
                }

                foreach (string channel in chatters.Channels)
                {
                    if (!allChannels.ContainsKey(channel))
                    {
                        allChannels[channel] = channelIndex++;
                    }
                }
            }

            Console.WriteLine($"Loaded data in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            MatrixBuilder<float> builder = Matrix<float>.Build;
            // create empty matrix
            Matrix<float> matrix = builder.Sparse(allChannels.Count, userData.Count, 0);
            foreach ((string username, Dictionary<string, int> presentIn) in userData)
            {
                foreach ((string channel, int count) in presentIn)
                {
                    matrix[allChannels[channel], userIndex[username]] = count;
                }
            }

            Console.WriteLine($"Built sparse matrix in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            Matrix<float> product = matrix.TransposeAndMultiply(matrix);
            Console.WriteLine($"Transpose and multiply in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            Matrix<float> norm = product.NormalizeColumns(2).NormalizeRows(2);

            Console.WriteLine($"Normalized in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();

            Matrix<float> similarity = norm.TransposeAndMultiply(norm);

            float sum = (similarity.ColumnSums() + similarity.RowSums()).Sum();

            Console.WriteLine(similarity.ToString());
            Console.WriteLine("sum: " + sum);

            Console.WriteLine($"Calculated similarity in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
        }
    }
}