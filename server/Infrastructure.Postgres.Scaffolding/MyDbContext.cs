using System;
using System.Collections.Generic;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres.Scaffolding;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_pkay");

            entity.ToTable("group", "chat");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasMany(d => d.Users).WithMany(p => p.Groups)
                .UsingEntity<Dictionary<string, object>>(
                    "Groupmember",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("Userid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("groupmember_user_fk"),
                    l => l.HasOne<Group>().WithMany()
                        .HasForeignKey("Groupid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("groupmember_group_fk"),
                    j =>
                    {
                        j.HasKey("Groupid", "Userid").HasName("groupmember_pk");
                        j.ToTable("groupmember", "chat");
                        j.IndexerProperty<string>("Groupid").HasColumnName("groupid");
                        j.IndexerProperty<string>("Userid").HasColumnName("userid");
                    });
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("message", "chat");

            entity.HasIndex(e => e.Userid, "IX_message_userid");

            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Messagetext).HasColumnName("messagetext");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Group).WithMany()
                .HasForeignKey(d => d.Groupid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("message_group_id_fk");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("message_user_id_fk");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pkey");

            entity.ToTable("user", "chat");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Hash).HasColumnName("hash");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Salt).HasColumnName("salt");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
