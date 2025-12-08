using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

/// <summary>
/// Sample Todo aggregate using MinimalCleanArch base entities.
/// </summary>
public class Todo : BaseSoftDeleteEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Priority { get; private set; }
    public DateTime? DueDate { get; private set; }

    private Todo() { } // EF Core

    public Todo(string title, string? description = null, int priority = 0, DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Update(string title, string? description, int priority, DateTime? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        LastModifiedAt = DateTime.UtcNow;
    }
}
