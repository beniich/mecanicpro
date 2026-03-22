using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Feedback;

public class SurveyCampaign : BaseEntity<Guid>
{
    public Guid RevisionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime SentAt { get; private set; }
    public string Token { get; private set; } = null!;
    public int? NpsScore { get; private set; } // 0 to 10
    public string? Comment { get; private set; }
    public string Channel { get; private set; } = "Email";

    private SurveyCampaign() { }

    public static SurveyCampaign Create(Guid revisionId, Guid customerId, string channel = "Email")
    {
        return new SurveyCampaign
        {
            Id = Guid.NewGuid(),
            RevisionId = revisionId,
            CustomerId = customerId,
            SentAt = DateTime.UtcNow,
            Token = Guid.NewGuid().ToString("N"),
            Channel = channel
        };
    }

    public void RegisterResponse(int score, string? comment)
    {
        if (score < 0 || score > 10) throw new BusinessRuleViolationException("NPS Score must be between 0 and 10.");
        NpsScore = score;
        Comment = comment;
    }
}

public interface ISurveyRepository
{
    Task<IEnumerable<SurveyCampaign>> GetAllAsync(Guid garageId, CancellationToken ct = default);
    Task<SurveyCampaign?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(SurveyCampaign survey, CancellationToken ct = default);
}

public static class SatisfactionService
{
    public static double CalculateNps(IEnumerable<int> scores)
    {
        var list = scores.ToList();
        if (!list.Any()) return 0;

        var promoters = list.Count(s => s >= 9);
        var detractors = list.Count(s => s <= 6);
        
        return Math.Round(((double)promoters - detractors) / list.Count * 100, 1);
    }
}
