using System.Globalization;

namespace LibrarySystem.Application.Common;

public static class CurrencyFormatter
{
    private static readonly CultureInfo PhilippineCulture =
        CultureInfo.GetCultureInfo("en-PH");

    public static string Format(decimal amount) =>
        amount.ToString("C", PhilippineCulture);
}
