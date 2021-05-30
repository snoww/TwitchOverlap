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
        public virtual DbSet<Overlap> Overlaps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("channel");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Avatar).HasColumnName("avatar");
                entity.Property(e => e.Chatters).HasColumnName("chatters");
                entity.Property(e => e.Game).HasColumnName("game");
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
