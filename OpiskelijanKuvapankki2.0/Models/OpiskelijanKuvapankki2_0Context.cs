using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace OpiskelijanKuvapankki2_0.Models;

public partial class OpiskelijanKuvapankki2_0Context : DbContext
{
    public OpiskelijanKuvapankki2_0Context()
    {
    }

    public OpiskelijanKuvapankki2_0Context(DbContextOptions<OpiskelijanKuvapankki2_0Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Login> Logins { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }
    public virtual DbSet<Organisation> Organisations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=local");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK_Categories");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(50);
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK_Images");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.LoginId).HasColumnName("LoginID");

            entity.HasOne(d => d.Category).WithMany(p => p.Images)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Categories");
        });

        modelBuilder.Entity<Login>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Logins_Email").IsUnique();

            entity.Property(e => e.LoginId).HasColumnName("LoginID");
            entity.Property(e => e.Contact).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Pword).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
