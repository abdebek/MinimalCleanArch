using MinimalCleanArch.Extensions.Models;

namespace MinimalCleanArch.Validation.Models;

/// <summary>
/// Problem details for validation errors
/// </summary>
public class ValidationProblemDetails : ProblemDetails
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationProblemDetails"/> class
    /// </summary>
    public ValidationProblemDetails()
    {
        Title = "Validation Failed";
        Status = 400;
        Type = "https://httpstatuses.com/400";
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Gets or sets the validation errors
    /// </summary>
    public IDictionary<string, string[]> Errors { get; set; }
}
