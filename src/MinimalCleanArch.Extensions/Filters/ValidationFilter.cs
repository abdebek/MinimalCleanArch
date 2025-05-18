using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using MinimalCleanArch.Extensions.Models;

namespace MinimalCleanArch.Extensions.Filters
{
    /// <summary>
    /// Filter for validating request parameters in Minimal API endpoints
    /// </summary>
    /// <typeparam name="T">The type of the parameter to validate</typeparam>
    public class ValidationFilter<T>
    {
        private readonly IValidator<T> _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationFilter{T}"/> class
        /// </summary>
        /// <param name="validator">The validator</param>
        public ValidationFilter(IValidator<T> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Invokes the validation filter
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="context">The HTTP context</param>
        /// <param name="next">The next delegate in the pipeline</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task<object?> InvokeAsync(T parameter, HttpContext context, Func<Task<object?>> next)
        {
            var validationResult = await _validator.ValidateAsync(parameter);
            
            if (!validationResult.IsValid)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/problem+json";
                
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Validation Failed",
                    Type = "https://httpstatuses.com/400",
                    Detail = "One or more validation errors occurred",
                    Instance = context.Request.Path
                };
                
                problemDetails.Extensions["errors"] = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
                return null;
            }
            
            return await next();
        }
    }
}
