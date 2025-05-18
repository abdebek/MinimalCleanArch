using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinimalCleanArch.Extensions.Models
{
    /// <summary>
    /// Problem details for HTTP responses
    /// </summary>
    public class ProblemDetails
    {
        /// <summary>
        /// Gets or sets the URI that identifies the problem type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a short, human-readable summary of the problem type
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets a human-readable explanation specific to this occurrence of the problem
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a URI reference that identifies the specific occurrence of the problem
        /// </summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// Gets the extensions for this problem details instance
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> Extensions { get; } = new Dictionary<string, object>();
    }
}
