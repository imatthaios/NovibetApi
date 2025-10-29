using System.ComponentModel.DataAnnotations;

namespace Novibet.Application.Common.Validation;

public class ValidCurrencyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Currency code is required");
        }

        var currencyCode = value.ToString();
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return new ValidationResult("Currency code is required");
        }

        return ValidateBasicFormat(currencyCode);
    }

    private static ValidationResult? ValidateBasicFormat(string currencyCode)
    {
        if (currencyCode.Length != 3)
        {
            return new ValidationResult("Currency code must be exactly 3 characters");
        }

        if (!currencyCode.All(char.IsLetter))
        {
            return new ValidationResult("Currency code must contain only letters");
        }

        var commonCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EUR", "USD", "JPY", "BGN", "CZK", "DKK", "GBP", "HUF", "PLN", "RON", "SEK",
            "CHF", "ISK", "NOK", "TRY", "AUD", "BRL", "CAD", "CNY", "HKD", "IDR",
            "ILS", "INR", "KRW", "MXN", "MYR", "NZD", "PHP", "SGD", "THB", "ZAR"
        };

        return !commonCurrencies.Contains(currencyCode) ?
            new ValidationResult($"Currency '{currencyCode}' is not supported")
            : ValidationResult.Success;
    }
}