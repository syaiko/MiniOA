using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniOA.Core.Entities;
using MiniOA.Core.Enums;
using MiniOA.Infrastructure.Utils;

namespace MiniOA.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TodoTask> Tasks { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<TaskAuditLog> AuditLogs { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }

        public DbSet<WorkflowNode> WorkflowNodes { get; set; }

        public DbSet<ApprovalRecord> ApprovalRecords { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TodoTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).HasConversion<int>(); // 将枚举存为整型
                entity.Property(e => e.Priority).HasConversion<int>(); // 将优先级枚举存为整型
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Tasks)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.AssignedUser)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(e => e.AssignedUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Role).HasConversion<int>(); // 角色枚举存为整型
                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.Username).IsUnique(); // 用户名唯一
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasOne(e => e.Parent)
                      .WithMany(e => e.Children)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Manager)
                      .WithMany(e => e.ManagedDepartments)
                      .HasForeignKey(e => e.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => e.ManagerId);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<TaskAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperationType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.FromStatus).HasConversion<int>();
                entity.Property(e => e.ToStatus).HasConversion<int>();
                entity.HasOne(e => e.Task)
                      .WithMany()
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<WorkflowInstance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.WorkflowType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BusinessData);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasOne(e => e.Creator)
                      .WithMany(e => e.CreatedWorkflows)
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CurrentNode)
                      .WithMany()
                      .HasForeignKey(e => e.CurrentNodeId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.CreatorId);
                entity.HasIndex(e => e.CurrentNodeId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<WorkflowNode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WorkflowType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NodeType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ConditionExpression);
                entity.Property(e => e.NodeConfig);
                entity.HasOne(e => e.Approver)
                      .WithMany()
                      .HasForeignKey(e => e.ApproverId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Department)
                      .WithMany(e => e.WorkflowNodes)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.NextNode)
                      .WithMany()
                      .HasForeignKey(e => e.NextNodeId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.WorkflowType);
                entity.HasIndex(e => e.ApproverId);
                entity.HasIndex(e => e.DepartmentId);
                entity.HasIndex(e => e.NextNodeId);
            });

            modelBuilder.Entity<ApprovalRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.Property(e => e.Action).HasConversion<int>();
                entity.HasOne(e => e.WorkflowInstance)
                      .WithMany(e => e.ApprovalRecords)
                      .HasForeignKey(e => e.WorkflowInstanceId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Node)
                      .WithMany(e => e.ApprovalRecords)
                      .HasForeignKey(e => e.NodeId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Approver)
                      .WithMany(e => e.ApprovalRecords)
                      .HasForeignKey(e => e.ApproverId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.WorkflowInstanceId);
                entity.HasIndex(e => e.NodeId);
                entity.HasIndex(e => e.ApproverId);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.RelatedType).HasMaxLength(50);
                entity.Property(e => e.Type).HasConversion<int>();
                entity.HasOne(e => e.Receiver)
                      .WithMany(e => e.Notifications)
                      .HasForeignKey(e => e.ReceiverId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);
            });

            //PasswordHelper.CreatePWHash("admin", out byte[] pwHash, out byte[] pwSalt);
            ////初始化数据，测试专用
            //modelBuilder.Entity<User>().HasData(new User
            //{
            //    Id = 3,
            //    Username = "admin",
            //    Password = "admin",
            //    PasswordHash = pwHash,
            //    PasswordSalt = pwSalt,
            //    FullName = "系统管理员",
            //    Role = UserRole.SuperAdmin
            //});

        }
    }
}
