namespace MinimalCleanArch.Specifications;

/// <summary>
/// Extension methods for composing specifications.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Combines two specifications with a logical AND.
    /// </summary>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) =>
        new AndSpecification<T>(left, right);

    /// <summary>
    /// Combines two specifications with a logical OR.
    /// </summary>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) =>
        new OrSpecification<T>(left, right);

    /// <summary>
    /// Negates a specification.
    /// </summary>
    public static ISpecification<T> Not<T>(this ISpecification<T> specification) =>
        new NotSpecification<T>(specification);
}
