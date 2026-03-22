using System;
using System.Collections.Generic;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Customers;

public enum CustomerSegment { Standard, Silver, Gold, Platinum, VIP }
public enum SubscriptionStatus { Active, Trialing, PastDue, Cancelled }
public enum ContactChannel { Email, SMS, Phone, WhatsApp }

public record CustomerCreatedEvent(Guid Id, string Email);
public record LoyaltyPointsAwardedEvent(Guid CustomerId, int PointsAwarded, int NewTotal);

public class Customer : AggregateRoot<Guid>
{
    public FullName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone? Phone { get; private set; }
    public Address? Address { get; private set; }
    public CustomerSegment Segment { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? Notes { get; private set; }
    public string? Tags { get; private set; }
    public ContactChannel PreferredContact { get; private set; } = ContactChannel.Email;
    public LoyaltyAccount Loyalty { get; private set; } = new();

    public bool IsBusiness { get; private set; }
    public string? CompanyName { get; private set; }
    public string? TaxId { get; private set; }

    private Customer() { } // EF Core

    public static Customer Create(FullName name, Email email, Phone? phone = null)
    {
        var c = new Customer { Id = Guid.NewGuid(), Name = name, Email = email, Phone = phone, Segment = CustomerSegment.Standard };
        c.AddDomainEvent(new CustomerCreatedEvent(c.Id, email.Value));
        return c;
    }

    public static Customer CreateBusiness(string companyName, string taxId, Email email, Phone? phone = null)
    {
        var c = new Customer { 
            Id = Guid.NewGuid(), 
            Name = FullName.Create("Company", companyName), 
            Email = email, 
            Phone = phone, 
            Segment = CustomerSegment.Gold,
            IsBusiness = true,
            CompanyName = companyName,
            TaxId = taxId
        };
        c.AddDomainEvent(new CustomerCreatedEvent(c.Id, email.Value));
        return c;
    }

    public void UpdateContact(FullName name, Email email, Phone? phone, Address? address, string? notes, string? tags, ContactChannel preferredContact, string? companyName = null, string? taxId = null)
    {
        Name = name; Email = email; Phone = phone; Address = address;
        Notes = notes; Tags = tags; PreferredContact = preferredContact;
        CompanyName = companyName; TaxId = taxId;
        MarkUpdated();
    }

    public void AddLoyaltyPoints(int points, string reason)
    {
        Loyalty.AddPoints(points, reason);
        AddDomainEvent(new LoyaltyPointsAwardedEvent(Id, points, Loyalty.Points));
    }

    public void SetStripeId(string id) => StripeCustomerId = id;
}

public class LoyaltyAccount
{
    public int Points { get; set; }
    public CustomerSegment Level { get; set; }
    public List<LoyaltyTransaction> Transactions { get; set; } = new();

    public void AddPoints(int p, string reason)
    {
        Points += p;
        Transactions.Add(new LoyaltyTransaction(p, reason, DateTime.UtcNow));
        Level = ComputeSegment();
    }

    public CustomerSegment ComputeSegment() => Points switch
    {
        > 5000 => CustomerSegment.Platinum,
        > 2000 => CustomerSegment.Gold,
        > 500  => CustomerSegment.Silver,
        _      => CustomerSegment.Standard
    };
}

public record LoyaltyTransaction(int Points, string Reason, DateTime Date);

public class Subscription : AggregateRoot<Guid>
{
    public Guid GarageId { get; private set; }
    public Guid PlanId { get; private set; }
    public string StripeSubscriptionId { get; private set; } = null!;
    public string PlanTier { get; private set; } = null!;
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }

    // Features config
    public int MaxMechanics { get; private set; }
    public bool HasEcommerce { get; private set; }
    public bool HasApiAccess { get; private set; }
    public bool IsWhiteLabel { get; private set; }

    public static Subscription Create(Guid garageId, Guid planId, string stripeId, string tier, int maxMech, bool ecommerce, bool api, bool whitelabel)
    {
        return new Subscription { Id = Guid.NewGuid(), GarageId = garageId, PlanId = planId, StripeSubscriptionId = stripeId, PlanTier = tier, Status = SubscriptionStatus.Active, MaxMechanics = maxMech, HasEcommerce = ecommerce, HasApiAccess = api, IsWhiteLabel = whitelabel, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1) };
    }

    public void Cancel() { CancelAtPeriodEnd = true; Status = SubscriptionStatus.Cancelled; MarkUpdated(); }
    public void Activate() { Status = SubscriptionStatus.Active; MarkUpdated(); }
    public void SetPastDue() { Status = SubscriptionStatus.PastDue; MarkUpdated(); }
}
