using Fixar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fixar.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.EntityName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.UserName)
            .HasMaxLength(256);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(64);

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}
