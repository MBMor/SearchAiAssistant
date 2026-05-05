using SearchAiAssistant.Domain.Entities;
using Xunit;

namespace SearchAiAssistant.Tests.Unit.Domain;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateEmployee()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-05T10:00:00Z");

        var employee = new Employee(
            id: Guid.NewGuid(),
            firstName: " Anna ",
            lastName: " Novak ",
            email: " ANNA.NOVAK@EXAMPLE.COM ",
            department: " Engineering ",
            jobTitle: " Backend Developer ",
            skills: ["C#", ".NET", "C#", " PostgreSQL "],
            location: " Prague ",
            createdAt: createdAt);

        Assert.Equal("Anna", employee.FirstName);
        Assert.Equal("Novak", employee.LastName);
        Assert.Equal("anna.novak@example.com", employee.Email);
        Assert.Equal("Engineering", employee.Department);
        Assert.Equal("Backend Developer", employee.JobTitle);
        Assert.Equal(["C#", ".NET", "PostgreSQL"], employee.Skills);
        Assert.Equal("Prague", employee.Location);
        Assert.Equal(createdAt, employee.CreatedAt);
        Assert.Equal(createdAt, employee.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Employee(
                id: Guid.NewGuid(),
                firstName: "",
                lastName: "Novak",
                email: "anna.novak@example.com",
                department: "Engineering",
                jobTitle: "Backend Developer",
                skills: ["C#"],
                location: "Prague",
                createdAt: DateTimeOffset.UtcNow));

        Assert.Contains("firstName is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Employee(
                id: Guid.NewGuid(),
                firstName: "Anna",
                lastName: "Novak",
                email: "not-an-email",
                department: "Engineering",
                jobTitle: "Backend Developer",
                skills: ["C#"],
                location: "Prague",
                createdAt: DateTimeOffset.UtcNow));

        Assert.Contains("email must be a valid email address", exception.Message);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateEmployeeAndUpdatedAt()
    {
        var createdAt = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var updatedAt = DateTimeOffset.Parse("2026-05-06T10:00:00Z");

        var employee = new Employee(
            id: Guid.NewGuid(),
            firstName: "Anna",
            lastName: "Novak",
            email: "anna.novak@example.com",
            department: "Engineering",
            jobTitle: "Backend Developer",
            skills: ["C#"],
            location: "Prague",
            createdAt: createdAt);

        employee.Update(
            firstName: "Anna Maria",
            lastName: "Novak",
            email: "anna.maria@example.com",
            department: "Platform",
            jobTitle: "Senior Backend Developer",
            skills: [".NET", "PostgreSQL"],
            location: "Brno",
            updatedAt: updatedAt);

        Assert.Equal("Anna Maria", employee.FirstName);
        Assert.Equal("anna.maria@example.com", employee.Email);
        Assert.Equal("Platform", employee.Department);
        Assert.Equal("Senior Backend Developer", employee.JobTitle);
        Assert.Equal([".NET", "PostgreSQL"], employee.Skills);
        Assert.Equal("Brno", employee.Location);
        Assert.Equal(createdAt, employee.CreatedAt);
        Assert.Equal(updatedAt, employee.UpdatedAt);
    }
}