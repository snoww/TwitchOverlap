using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace TwitchGraph.Models
{
    public class TwitchContext : DbContext
    {
        private readonly string _connectionString;

        public TwitchContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public virtual DbSet<Edge> Edges { get; set; }
        public virtual DbSet<Node> Nodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Edge>(entity =>
            {
                entity.HasKey(e => new { e.Source, e.Target })
                    .HasName("edges_pkey");

                entity.ToTable("edges");

                entity.Property(e => e.Source).HasColumnName("source");

                entity.Property(e => e.Target).HasColumnName("target");

                entity.Property(e => e.Weight).HasColumnName("weight");
            });

            modelBuilder.Entity<Node>(entity =>
            {
                entity.ToTable("nodes");

                entity.Property(e => e.Id).HasColumnName("id");
                
                entity.Property(e => e.Label).HasColumnName("label");

                entity.Property(e => e.Size).HasColumnName("size");
            });
            
        }
    }
}
