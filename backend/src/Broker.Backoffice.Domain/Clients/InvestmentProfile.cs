using Broker.Backoffice.Domain.Common;

namespace Broker.Backoffice.Domain.Clients;

public sealed class InvestmentProfile : Entity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public InvestmentObjective? Objective { get; set; }
    public InvestmentRiskTolerance? RiskTolerance { get; set; }
    public LiquidityNeeds? LiquidityNeeds { get; set; }
    public InvestmentTimeHorizon? TimeHorizon { get; set; }
    public InvestmentKnowledge? Knowledge { get; set; }
    public InvestmentExperience? Experience { get; set; }
    public string? Notes { get; set; }
}
