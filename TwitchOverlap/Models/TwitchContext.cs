using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TwitchOverlap.Models
{
    public class TwitchContext : DbContext
    {
        public TwitchContext(DbContextOptions options) : base(options)
        {
        }

        public virtual DbSet<Channel> Channels { get; set; }
        public virtual DbSet<ChannelHistory> ChannelsHistories { get; set; }
        public virtual DbSet<Overlap> Overlaps { get; set; }
        public virtual DbSet<OverlapDaily> OverlapsDaily { get; set; }
        public virtual DbSet<OverlapRolling3Days> OverlapRolling3Days { get; set; }
        public virtual DbSet<OverlapRolling7Days> OverlapRolling7Days { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("channel");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LoginName).HasColumnName("login_name");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Avatar).HasColumnName("avatar");
                entity.Property(e => e.Chatters).HasColumnName("chatters");
                entity.Property(e => e.Game).HasColumnName("game");
                entity.Property(e => e.LastUpdate).HasColumnName("last_update");
                entity.Property(e => e.Shared).HasColumnName("shared");
                entity.Property(e => e.Viewers).HasColumnName("viewers");
            });
            
            modelBuilder.Entity<ChannelHistory>(entity =>
            {
                entity.HasKey(e => new { e.Timestamp, e.Id })
                    .HasName("channel_history_pkey");

                entity.ToTable("channel_history");
                
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Chatters).HasColumnName("chatters");
                entity.Property(e => e.Shared).HasColumnName("shared");
                entity.Property(e => e.Viewers).HasColumnName("viewers");
                entity.HasOne(d => d.Channel)
                    .WithMany(p => p.History)
                    .HasForeignKey(d => d.Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("channel_history_id_fkey");
            });

            modelBuilder.Entity<Overlap>(entity =>
            {
                entity.HasKey(e => new {e.Timestamp, e.Channel})
                    .HasName("overlap_pkey");

                entity.ToTable("overlap");

                entity.HasIndex(e => new {e.Timestamp, e.Channel}, "overlap_timestamp_desc_channel_index").HasSortOrder(SortOrder.Descending);

                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.Shared).HasColumnType("jsonb").HasColumnName("shared");

                entity.HasOne(d => d.ChannelNavigation)
                    .WithMany(p => p.OverlapChannelNavigations)
                    .HasForeignKey(d => d.Channel)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("overlap_source_fkey");
            });

            modelBuilder.Entity<OverlapDaily>(entity =>
            {
                entity.HasKey(e => new {e.Date, e.Channel}).HasName("overlap_daily_pkey");
                entity.ToTable("overlap_daily");

                entity.Property(e => e.Date).HasColumnName("date");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.ChannelTotalOverlap).HasColumnName("channel_total_overlap");
                entity.Property(e => e.ChannelTotalUnique).HasColumnName("channel_total_unique");
                entity.Property(e => e.Shared).HasColumnType("jsonb").HasColumnName("shared");
            });

            modelBuilder.Entity<OverlapRolling3Days>(entity =>
            {
                entity.HasKey(e => new {e.Date, e.Channel}).HasName("overlap_rolling_3_days_pkey");
                entity.ToTable("overlap_rolling_3_days");

                entity.Property(e => e.Date).HasColumnName("date");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.ChannelTotalOverlap).HasColumnName("channel_total_overlap");
                entity.Property(e => e.ChannelTotalUnique).HasColumnName("channel_total_unique");
                entity.Property(e => e.Shared).HasColumnType("jsonb").HasColumnName("shared");
            });

            modelBuilder.Entity<OverlapRolling7Days>(entity =>
            {
                entity.HasKey(e => new {e.Date, e.Channel}).HasName("overlap_rolling_7_days_pkey");
                entity.ToTable("overlap_rolling_7_days");

                entity.Property(e => e.Date).HasColumnName("date");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.ChannelTotalOverlap).HasColumnName("channel_total_overlap");
                entity.Property(e => e.ChannelTotalUnique).HasColumnName("channel_total_unique");
                entity.Property(e => e.Shared).HasColumnType("jsonb").HasColumnName("shared");
            });
        }
    }
}