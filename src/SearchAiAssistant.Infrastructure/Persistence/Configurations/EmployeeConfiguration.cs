using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(employee => employee.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(employee => employee.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(employee => employee.Department)
            .HasColumnName("department")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(employee => employee.JobTitle)
            .HasColumnName("job_title")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(employee => employee.Skills)
            .HasColumnName("skills")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(employee => employee.Location)
            .HasColumnName("location")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(employee => employee.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(employee => employee.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(employee => employee.Email)
            .IsUnique();

        builder.HasIndex(employee => employee.Department);

        builder.HasIndex(employee => employee.JobTitle);
    }
}