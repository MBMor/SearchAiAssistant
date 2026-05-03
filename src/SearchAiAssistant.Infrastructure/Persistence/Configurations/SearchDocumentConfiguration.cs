using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Infrastructure.Persistence.Configurations;

public sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("search_documents");

        builder.HasKey(searchDocument => searchDocument.Id);

        builder.Property(searchDocument => searchDocument.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(searchDocument => searchDocument.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(searchDocument => searchDocument.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(searchDocument => searchDocument.Title)
            .HasColumnName("title")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(searchDocument => searchDocument.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(searchDocument => searchDocument.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(searchDocument => searchDocument.Category)
            .HasColumnName("category")
            .HasMaxLength(150);

        builder.Property(searchDocument => searchDocument.Department)
            .HasColumnName("department")
            .HasMaxLength(150);

        builder.Property(searchDocument => searchDocument.IndexedAt)
            .HasColumnName("indexed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(searchDocument => new
        {
            searchDocument.SourceType,
            searchDocument.SourceId
        }).IsUnique();

        builder.HasIndex(searchDocument => searchDocument.Category);

        builder.HasIndex(searchDocument => searchDocument.Department);
    }
}