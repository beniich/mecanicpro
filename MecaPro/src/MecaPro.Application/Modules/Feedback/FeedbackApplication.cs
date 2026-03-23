using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Feedback;
using MediatR;

namespace MecaPro.Application.Modules.Feedback;

// DTOs
public record SurveyDto(Guid Id, Guid CustomerId, DateTime SentAt, int? NpsScore, string? Comment, string Channel);
public record NpsStatsDto(double NpsScore, int TotalReponses, int Promoters, int Detractors);

// Commands & Queries
public record SendSurveyCommand(Guid RevisionId, Guid CustomerId, string Channel) : IRequest<Result<SurveyDto>>;
public record SubmitSurveyResponseCommand(string Token, int Score, string? Comment) : IRequest<Result<bool>>;
public record GetNpsStatsQuery() : IRequest<Result<NpsStatsDto>>;

// Mapping Extensions
public static class FeedbackMappingExtensions
{
    public static SurveyDto ToDto(this SurveyCampaign s) => new(s.Id, s.CustomerId, s.SentAt, s.NpsScore, s.Comment, s.Channel);
}

// Handlers
public class FeedbackHandlers(
    ISurveyRepository surveys, 
    IUnitOfWork uow) : 
    IRequestHandler<SendSurveyCommand, Result<SurveyDto>>,
    IRequestHandler<SubmitSurveyResponseCommand, Result<bool>>,
    IRequestHandler<GetNpsStatsQuery, Result<NpsStatsDto>>
{
    public async Task<Result<SurveyDto>> Handle(SendSurveyCommand cmd, CancellationToken ct)
    {
        var survey = SurveyCampaign.Create(cmd.RevisionId, cmd.CustomerId, cmd.Channel);
        await surveys.AddAsync(survey, ct);
        await uow.SaveChangesAsync(ct);
        return Result<SurveyDto>.Success(survey.ToDto());
    }

    public async Task<Result<bool>> Handle(SubmitSurveyResponseCommand cmd, CancellationToken ct)
    {
        var survey = await surveys.GetByTokenAsync(cmd.Token, ct);
        if (survey == null) return Result<bool>.Failure("Enquête introuvable ou jeton invalide.");
        
        survey.RegisterResponse(cmd.Score, cmd.Comment);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<NpsStatsDto>> Handle(GetNpsStatsQuery query, CancellationToken ct)
    {
        var reponses = (await surveys.GetAllAsync(Guid.Empty, ct)).Where(s => s.NpsScore.HasValue).ToList();
        var scores = reponses.Select(s => s.NpsScore!.Value).ToList();
        
        var nps = SatisfactionService.CalculateNps(scores);
        var promoters = scores.Count(s => s >= 9);
        var detractors = scores.Count(s => s <= 6);
        
        return Result<NpsStatsDto>.Success(new NpsStatsDto(nps, scores.Count, promoters, detractors));
    }
}
