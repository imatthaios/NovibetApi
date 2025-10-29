using System.ComponentModel.DataAnnotations;

namespace Novibet.Application.Common.Validation;

public class ValidDecimalAttribute: ValidationAttribute
{
    public string FieldName { get; set; } = "Value";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return new ValidationResult($"{FieldName} is required");

        if (value is decimal and < 0)
        {
            var errorMessage = ErrorMessage ?? $"{FieldName} must be positive";
            return new ValidationResult(errorMessage);
        }

        return ValidationResult.Success;
    }
}