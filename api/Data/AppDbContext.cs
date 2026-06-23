using Microsoft.EntityFrameworkCore;
using ObservabilityApi.Models;

namespace ObservabilityApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AgentEvent> AgentEvents => Set<AgentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentEvent>(entity =>
        {
            entity.ToTable("AgentEvents");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AgentName)
                .HasColumnName("AgentName")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SessionId)
                .HasColumnName("SessionId")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Model)
                .HasColumnName("Model")
                .HasMaxLength(100);

            entity.Property(e => e.InputTokens)
                .HasColumnName("InputTokens")
                .HasDefaultValue(0);

            entity.Property(e => e.OutputTokens)
                .HasColumnName("OutputTokens")
                .HasDefaultValue(0);

            entity.Property(e => e.CacheReadTokens)
                .HasColumnName("CacheReadTokens")
                .HasDefaultValue(0);

            entity.Property(e => e.CacheWriteTokens)
                .HasColumnName("CacheWriteTokens")
                .HasDefaultValue(0);

            entity.Property(e => e.ReasoningTokens)
                .HasColumnName("ReasoningTokens")
                .HasDefaultValue(0);

            entity.Property(e => e.TotalCostUsd)
                .HasColumnName("TotalCostUsd")
                .HasColumnType("decimal(10,6)")
                .HasDefaultValue(0m);

            entity.Property(e => e.ActivityType)
                .HasColumnName("ActivityType")
                .HasMaxLength(50);

            entity.Property(e => e.ApiCalls)
                .HasColumnName("ApiCalls")
                .HasDefaultValue(1);

            entity.Property(e => e.HasAgentSpawn)
                .HasColumnName("HasAgentSpawn")
                .HasDefaultValue(false);

            entity.Property(e => e.SessionDurationMinutes)
                .HasColumnName("SessionDurationMinutes")
                .HasDefaultValue(0);

            entity.Property(e => e.EventTimestamp)
                .HasColumnName("EventTimestamp")
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(e => e.CollectedAt)
                .HasColumnName("CollectedAt")
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => new { e.SessionId, e.EventTimestamp, e.AgentName })
                .HasDatabaseName("UQ_AgentEvents")
                .IsUnique();

            entity.HasIndex(e => e.EventTimestamp)
                .HasDatabaseName("IX_AgentEvents_EventTimestamp")
                .IsDescending();

            entity.HasIndex(e => new { e.AgentName, e.EventTimestamp })
                .HasDatabaseName("IX_AgentEvents_AgentName_Timestamp")
                .IsDescending(false, true);

            entity.HasIndex(e => new { e.ActivityType, e.EventTimestamp })
                .HasDatabaseName("IX_AgentEvents_ActivityType")
                .IsDescending(false, true);
        });
    }
}
