using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChannelIntersection.Models
{
    public class TwitchContext : DbContext
    {
        private readonly string _connectionString;

        public TwitchContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public virtual DbSet<Channel> Channels { get; set; }
        public virtual DbSet<Overlap> Overlaps { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString).EnableSensitiveDataLogging().LogTo(Console.WriteLine);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("channel");

                entity.HasIndex(e => e.DisplayName, "channel_display_name_key")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Avatar).HasColumnName("avatar");
                entity.Property(e => e.Chatters).HasColumnName("chatters");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Game)
                    .IsRequired()
                    .HasColumnName("game");
                entity.Property(e => e.LastUpdate).HasColumnName("last_update");
                entity.Property(e => e.Shared).HasColumnName("shared");
                entity.Property(e => e.Viewers).HasColumnName("viewers");
            });

            modelBuilder.Entity<Overlap>(entity =>
            {
                entity.HasKey(e => new { e.Timestamp, e.Source, e.Target })
                    .HasName("overlap_pkey");

                entity.ToTable("overlap");
                
                entity.HasIndex(e => e.Timestamp, "overlap_timestamp_index").HasSortOrder(SortOrder.Descending);
                entity.HasIndex(e => new {e.Source, e.Overlapped}, "overlap_source_overlap_index").HasSortOrder(SortOrder.Ascending, SortOrder.Descending);
                entity.HasIndex(e => new {e.Target, e.Overlapped}, "overlap_target_overlap_index").HasSortOrder(SortOrder.Ascending, SortOrder.Descending);
                
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Source).HasColumnName("source");
                entity.Property(e => e.Target).HasColumnName("target");
                entity.Property(e => e.Overlapped).HasColumnName("overlap");
            });
        }
    }
}
