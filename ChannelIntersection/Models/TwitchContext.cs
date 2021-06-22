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
        public virtual DbSet<Chatters> Chatters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString).EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("channel");
                
                entity.HasIndex(e => e.LoginName, "channel_login_name")
                    .IsUnique();
                entity.HasIndex(e => e.LastUpdate, "channel_timestamp_index")
                    .HasSortOrder(SortOrder.Descending);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LoginName).HasColumnName("login_name");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Avatar).HasColumnName("avatar");
                entity.Property(e => e.Chatters).HasColumnName("chatters");
                entity.Property(e => e.Game)
                    .IsRequired()
                    .HasColumnName("game");
                entity.Property(e => e.LastUpdate).HasColumnName("last_update");
                entity.Property(e => e.Shared).HasColumnName("shared");
                entity.Property(e => e.Viewers).HasColumnName("viewers");
            });

            modelBuilder.Entity<Overlap>(entity =>
            {
                entity.HasKey(e => new { e.Timestamp, e.Channel })
                    .HasName("overlap_pkey");

                entity.ToTable("overlap");
                
                entity.HasIndex(e => new {e.Timestamp, e.Channel}, "overlap_timestamp_desc_channel_index").HasSortOrder(SortOrder.Descending);
                
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.Shared).HasColumnType("jsonb").HasColumnName("shared");
            });

            // modelBuilder.Entity<Chatters>(entity =>
            // {
            //     entity.HasKey(e => e.Time).HasName("chatters_pk");
            //     entity.ToTable("chatters");
            //
            //     entity.Property(e => e.Time).HasColumnName("time");
            //     entity.Property(e => e.Users).HasColumnType("json").HasColumnName("users");
            //     entity.Property(e => e.Channels).HasColumnType("json").HasColumnName("channels");
            //     
            // });
            
            modelBuilder.Entity<Chatters>(entity =>
            {
                entity.HasKey(e => e.Time).HasName("chatters_pk");
                entity.ToTable("chatters");

                entity.Property(e => e.Time).HasColumnName("time");
                entity.Property(e => e.Users).HasColumnType("json").HasColumnName("users");
                entity.Property(e => e.Channels).HasColumnType("json").HasColumnName("channels");
                
            });
        }
    }
}
