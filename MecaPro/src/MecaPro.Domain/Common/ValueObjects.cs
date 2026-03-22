using System;

namespace MecaPro.Domain.Common;

public record FullName
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Full => $"{FirstName} {LastName}";

    private FullName() { }

    private FullName(string first, string last)
    {
        FirstName = first;
        LastName = last;
    }

    public static FullName Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first)) throw new ArgumentException("Prénom requis.");
        if (string.IsNullOrWhiteSpace(last)) throw new ArgumentException("Nom requis.");
        return new FullName(first.Trim(), last.Trim());
    }
}

public record Email
{
    public string Value { get; private set; } = null!;
    private Email() { }
    private Email(string value) => Value = value;
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains("@"))
            throw new ArgumentException("Email invalide.");
        return new Email(value.ToLowerInvariant().Trim());
    }
}

public record Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    private Money() { }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static Money Create(decimal amount, string currency = "EUR") => new(amount, currency);
    public int InCents() => (int)(Amount * 100);
}

public record Address
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string Country { get; private set; } = null!;

    private Address() { }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street; City = city; PostalCode = postalCode; Country = country;
    }

    public static Address Create(string street, string city, string postalCode, string country = "FR")
        => new(street, city, postalCode, country);
}

public record Phone
{
    public string Value { get; private set; } = null!;
    private Phone() { }
    private Phone(string value) => Value = value;
    public static Phone Create(string value) => new(value.Replace(" ", ""));
}
