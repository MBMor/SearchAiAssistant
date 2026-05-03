using SearchAiAssistant.Domain.Common;

namespace SearchAiAssistant.Domain.Entities;

public sealed class Employee : Entity
{
    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Department { get; private set; } = string.Empty;

    public string JobTitle { get; private set; } = string.Empty;

    public List<string> Skills { get; private set; } = [];

    public string Location { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Employee()
    {
    }

    public Employee(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string department,
        string jobTitle,
        IEnumerable<string>? skills,
        string location,
        DateTimeOffset createdAt)
        : base(id)
    {
        FirstName = Guard.Required(firstName, nameof(firstName), maxLength: 100);
        LastName = Guard.Required(lastName, nameof(lastName), maxLength: 100);
        Email = Guard.RequiredEmail(email, nameof(email));
        Department = Guard.Required(department, nameof(department), maxLength: 150);
        JobTitle = Guard.Required(jobTitle, nameof(jobTitle), maxLength: 150);
        Skills = Guard.NormalizeStringList(skills, nameof(skills), maxItemLength: 100);
        Location = Guard.Required(location, nameof(location), maxLength: 150);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Update(
        string firstName,
        string lastName,
        string email,
        string department,
        string jobTitle,
        IEnumerable<string>? skills,
        string location,
        DateTimeOffset updatedAt)
    {
        FirstName = Guard.Required(firstName, nameof(firstName), maxLength: 100);
        LastName = Guard.Required(lastName, nameof(lastName), maxLength: 100);
        Email = Guard.RequiredEmail(email, nameof(email));
        Department = Guard.Required(department, nameof(department), maxLength: 150);
        JobTitle = Guard.Required(jobTitle, nameof(jobTitle), maxLength: 150);
        Skills = Guard.NormalizeStringList(skills, nameof(skills), maxItemLength: 100);
        Location = Guard.Required(location, nameof(location), maxLength: 150);
        UpdatedAt = updatedAt;
    }
}