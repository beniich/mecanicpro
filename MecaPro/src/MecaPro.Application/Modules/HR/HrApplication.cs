using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.HR;
using MediatR;

namespace MecaPro.Application.Modules.HR;

// DTOs
public record AbsenceDto(Guid Id, Guid EmployeeId, DateTime StartDate, DateTime EndDate, string Type, string Status, string? Reason);
public record SkillDto(Guid Id, Guid EmployeeId, string SkillName, int ProficiencyLevel, DateTime LastCertified);

// Commands & Queries
public record RequestAbsenceCommand(Guid EmployeeId, DateTime StartDate, DateTime EndDate, string Type, string? Reason) : IRequest<Result<AbsenceDto>>;
public record ApproveAbsenceCommand(Guid AbsenceId, string ApprovedByUserId) : IRequest<Result<bool>>;
public record GetEmployeeAbsencesQuery(Guid EmployeeId) : IRequest<Result<List<AbsenceDto>>>;
public record AddSkillCommand(Guid EmployeeId, string SkillName, int ProficiencyLevel, DateTime Certified) : IRequest<Result<SkillDto>>;
public record GetEmployeeSkillsQuery(Guid EmployeeId) : IRequest<Result<List<SkillDto>>>;

// Mapping Extensions
public static class HrMappingExtensions
{
    public static AbsenceDto ToDto(this EmployeeAbsence a) => new(a.Id, a.EmployeeId, a.StartDate, a.EndDate, a.Type.ToString(), a.Status.ToString(), a.Reason);
    public static SkillDto ToDto(this EmployeeSkill s) => new(s.Id, s.EmployeeId, s.SkillName, s.ProficiencyLevel, s.LastCertified);
}

// Handlers
public class HrHandlers(
    IAbsenceRepository absences, 
    ISkillRepository skills, 
    IUnitOfWork uow) : 
    IRequestHandler<RequestAbsenceCommand, Result<AbsenceDto>>,
    IRequestHandler<ApproveAbsenceCommand, Result<bool>>,
    IRequestHandler<GetEmployeeAbsencesQuery, Result<List<AbsenceDto>>>,
    IRequestHandler<AddSkillCommand, Result<SkillDto>>,
    IRequestHandler<GetEmployeeSkillsQuery, Result<List<SkillDto>>>
{
    public async Task<Result<AbsenceDto>> Handle(RequestAbsenceCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<AbsenceType>(cmd.Type, out var type)) return Result<AbsenceDto>.Failure("Type d'absence invalide.");
        
        var absence = EmployeeAbsence.Request(cmd.EmployeeId, cmd.StartDate, cmd.EndDate, type, cmd.Reason);
        await absences.AddAsync(absence, ct);
        await uow.SaveChangesAsync(ct);
        return Result<AbsenceDto>.Success(absence.ToDto());
    }

    public async Task<Result<bool>> Handle(ApproveAbsenceCommand cmd, CancellationToken ct)
    {
        var absence = await absences.GetByIdAsync(cmd.AbsenceId, ct);
        if (absence == null) return Result<bool>.Failure("Absence introuvable.");
        
        absence.Approve(cmd.ApprovedByUserId);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<List<AbsenceDto>>> Handle(GetEmployeeAbsencesQuery query, CancellationToken ct)
    {
        var list = await absences.GetByEmployeeIdAsync(query.EmployeeId, ct);
        return Result<List<AbsenceDto>>.Success(list.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<SkillDto>> Handle(AddSkillCommand cmd, CancellationToken ct)
    {
        var skill = EmployeeSkill.Create(cmd.EmployeeId, cmd.SkillName, cmd.ProficiencyLevel, cmd.Certified);
        await skills.AddAsync(skill, ct);
        await uow.SaveChangesAsync(ct);
        return Result<SkillDto>.Success(skill.ToDto());
    }

    public async Task<Result<List<SkillDto>>> Handle(GetEmployeeSkillsQuery query, CancellationToken ct)
    {
        var list = await skills.GetByEmployeeIdAsync(query.EmployeeId, ct);
        return Result<List<SkillDto>>.Success(list.Select(s => s.ToDto()).ToList());
    }
}
