using Microsoft.EntityFrameworkCore;
using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Infrastructure.Data;

public class TaskSchedulerDbContext : DbContext
{
    public TaskSchedulerDbContext(DbContextOptions<TaskSchedulerDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<ConsumerInfo> Consumers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TaskItem
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Payload).HasColumnType("text");
            entity.Property(e => e.Result).HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.CronExpression).HasMaxLength(100);
            entity.Property(e => e.AssignedConsumer).HasMaxLength(100);

            // Configure enum properties
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Priority).HasConversion<int>();

            // Configure Metadata as JSON column
            entity.Property(e => e.Metadata).HasConversion(
                v => Newtonsoft.Json.JsonConvert.SerializeObject(v),
                v => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(v) ?? new Dictionary<string, string>()
            );

            // Indexes for better performance
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
        });

        // Configure ConsumerInfo
        modelBuilder.Entity<ConsumerInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CurrentTaskId).HasMaxLength(50);
            entity.Property(e => e.MachineName).HasMaxLength(100);

            // Indexes
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastHeartbeat);
        });
    }
}