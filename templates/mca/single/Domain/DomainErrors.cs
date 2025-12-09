using MinimalCleanArch.Domain.Common;

namespace MCA.Domain;

/// <summary>
/// Application-specific error definitions.
/// </summary>
public static class DomainErrors
{
    public static class General
    {
        public static Error NotFound(string resource, object id) =>
            Error.NotFound($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");
    }

    public static class Pagination
    {
        public static Error InvalidPageSize(int pageSize) =>
            Error.Validation("Pagination.InvalidPageSize", $"Page size must be greater than 0 (received {pageSize}).");

        public static Error InvalidPageIndex(int pageIndex) =>
            Error.Validation("Pagination.InvalidPageIndex", $"Page index must be greater than 0 (received {pageIndex}).");

        public static Error PageSizeTooLarge(int pageSize, int max) =>
            Error.Validation("Pagination.PageSizeTooLarge", $"Page size {pageSize} exceeds the allowed maximum of {max}.");
    }
}
