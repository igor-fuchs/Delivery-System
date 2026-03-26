using DeliverySystem.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = DeliverySystem.Application.Exceptions.ValidationException;

namespace DeliverySystem.Presentation.Filters;

/// <summary>
/// Action filter that automatically validates request models using FluentValidation.
/// Throws a <see cref="ValidationException"/> when validation fails,
/// which is then handled by the <see cref="DeliverySystem.Presentation.Middlewares.ExceptionHandlingMiddleware"/>.
/// Each field error carries a machine-readable <see cref="ValidationFieldError.Code"/> for i18n
/// sourced from <c>.WithErrorCode(...)</c> on each validator rule.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFilter"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve validators at runtime.</param>
    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator is null)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => new ValidationFieldError(e.ErrorCode, e.ErrorMessage)).ToArray()
                    );

                throw new ValidationException(errors);
            }
        }

        await next();
    }
}
