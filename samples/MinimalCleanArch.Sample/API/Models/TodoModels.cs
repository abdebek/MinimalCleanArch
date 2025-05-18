namespace MinimalCleanArch.Sample.API.Models;

/// <summary>
/// DTO for creating a new todo
/// </summary>
public class CreateTodoRequest
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the due date
    /// </summary>
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// DTO for updating a todo
/// </summary>
public class UpdateTodoRequest
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this todo is completed
    /// </summary>
    public bool IsCompleted { get; set; }
}

/// <summary>
/// DTO for a todo
/// </summary>
public class TodoResponse
{
    /// <summary>
    /// Gets or sets the ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this todo is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this todo was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created this todo
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this todo was last modified
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last modified this todo
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
