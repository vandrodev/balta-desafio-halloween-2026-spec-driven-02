using LinkShortener.Application.Domain.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkShortener.Infrastructure.Persistence;

internal sealed class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("Links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(link => link.Code)
            .IsUnique();

        builder.Property(link => link.TargetUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(link => link.CreatedAtUtc)
            .IsRequired();

        builder.Property(link => link.AccessCount)
            .IsRequired()
            .HasDefaultValue(0L);
    }
}
