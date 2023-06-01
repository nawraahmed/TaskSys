using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TaskManagementSystem.Models
{
    public partial class TaskAllocationDBContext : DbContext
    {
        public TaskAllocationDBContext()
        {
        }

        public TaskAllocationDBContext(DbContextOptions<TaskAllocationDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
        public virtual DbSet<Document> Documents { get; set; } = null!;
        public virtual DbSet<Log> Logs { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<Project> Projects { get; set; } = null!;
        public virtual DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
        public virtual DbSet<Task> Tasks { get; set; } = null!;
        public virtual DbSet<TaskComment> TaskComments { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AspNetRole>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedName] IS NOT NULL)");
            });

            modelBuilder.Entity<AspNetUser>(entity =>
            {
                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedUserName] IS NOT NULL)");

                entity.Property(e => e.Discriminator).HasDefaultValueSql("(N'')");

                entity.HasMany(d => d.Roles)
                    .WithMany(p => p.Users)
                    .UsingEntity<Dictionary<string, object>>(
                        "AspNetUserRole",
                        l => l.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                        r => r.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                        j =>
                        {
                            j.HasKey("UserId", "RoleId");

                            j.ToTable("AspNetUserRoles");

                            j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                        });
            });

            modelBuilder.Entity<AspNetUserLogin>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            });

            modelBuilder.Entity<AspNetUserToken>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasOne(d => d.UsernameNavigation)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(d => d.Username)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__documents__usern__47DBAE45");
            });

            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasOne(d => d.UsernameNavigation)
                    .WithMany(p => p.Logs)
                    .HasForeignKey(d => d.Username)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__logs__username__48CFD27E");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(d => d.UsernameNavigation)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.Username)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__notificat__usern__49C3F6B7");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasOne(d => d.CreatedByUsernameNavigation)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.CreatedByUsername)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__projects__create__4CA06362");
            });

            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(e => e.ProjectMembersId)
                    .HasName("PK__project___8708928C124CB42D");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectMembers)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__project_m__proje__4AB81AF0");

                entity.HasOne(d => d.UsernameNavigation)
                    .WithMany(p => p.ProjectMembers)
                    .HasForeignKey(d => d.Username)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__project_m__usern__4BAC3F29");
            });

            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasOne(d => d.AssignedToUsernameNavigation)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(d => d.AssignedToUsername)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__tasks__assigned___4F7CD00D");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__tasks__project_i__5070F446");

                entity.HasOne(d => d.TaskDocumentNavigation)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(d => d.TaskDocument)
                    .HasConstraintName("FK_tasks_documents");
            });

            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasKey(e => e.CommentId)
                    .HasName("PK__task_com__E7957687EA374025");

                entity.HasOne(d => d.Task)
                    .WithMany(p => p.TaskComments)
                    .HasForeignKey(d => d.TaskId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__task_comm__task___4D94879B");

                entity.HasOne(d => d.UsernameNavigation)
                    .WithMany(p => p.TaskComments)
                    .HasForeignKey(d => d.Username)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__task_comm__usern__4E88ABD4");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Username)
                    .HasName("PK__users__F3DBC573660B7E71");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
