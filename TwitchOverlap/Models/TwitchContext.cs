using Microsoft.EntityFrameworkCore;

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
                entity.HasKey(e => new { e.Id, e.Timestamp })
                    .HasName("overlap_pkey");
                entity.ToTable("overlap");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Data)
                    .HasColumnType("jsonb")
                    .HasColumnName("data");
                entity.HasOne(d => d.Channel)
                    .WithMany(p => p.Histories)
                    .HasForeignKey(d => d.Id)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("overlap_id_fkey");
            });
        }
    }
}
