using System;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.HR;

public enum AbsenceType { PaidLeave, SickLeave, Training, Other }
public enum AbsenceStatus { Pending, Approved, Rejected }

public class EmployeeAbsence : AggregateRoot<Guid>
{
    public Guid EmployeeId { get; private set; } // Could map to User ID in Auth Service
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public AbsenceType Type { get; private set; }
    public AbsenceStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmployeeAbsence() { } // EF Core

    public static EmployeeAbsence Request(Guid employeeId, DateTime start, DateTime end, AbsenceType type, string? reason)
    {
        if (end <= start) throw new BusinessRuleViolationException("End date must be after start date.");
        
        return new EmployeeAbsence
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            StartDate = start,
            EndDate = end,
            Type = type,
            Status = AbsenceStatus.Pending,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(string approvedByUserId)
    {
        if (Status != AbsenceStatus.Pending) throw new BusinessRuleViolationException("Can only approve pending requests.");
        Status = AbsenceStatus.Approved;
        ApprovedBy = approvedByUserId;
        MarkUpdated();
    }

    public void Reject(string rejectedByUserId)
    {
        if (Status != AbsenceStatus.Pending) throw new BusinessRuleViolationException("Can only reject pending requests.");
        Status = AbsenceStatus.Rejected;
        ApprovedBy = rejectedByUserId;
        MarkUpdated();
    }
}

public interface IAbsenceRepository : IRepository<EmployeeAbsence, Guid>
{
    Task<IEnumerable<EmployeeAbsence>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default);
}

public interface ISkillRepository : IRepository<EmployeeSkill, Guid>
{
    Task<IEnumerable<EmployeeSkill>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default);
}

public class EmployeeSkill : AggregateRoot<Guid>
{
    public Guid EmployeeId { get; private set; }
    public string SkillName { get; private set; } = null!; // e.g., "Moteur Electrique", "Climatisation"
    public int ProficiencyLevel { get; private set; } // 1 to 5
    public DateTime LastCertified { get; private set; }

    private EmployeeSkill() { }

    public static EmployeeSkill Create(Guid employeeId, string skillName, int proficiencyLevel, DateTime certified)
    {
        return new EmployeeSkill
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SkillName = skillName,
            ProficiencyLevel = proficiencyLevel,
            LastCertified = certified
        };
    }
}
