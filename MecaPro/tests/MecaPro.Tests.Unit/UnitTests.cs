// ============================================================
//  PHASE 11 — TESTS UNITAIRES (Domain & Application CQRS)
// ============================================================
using Xunit;
using Moq;
using FluentAssertions;
using MecaPro.Domain.Common;
using MecaPro.Application;

namespace MecaPro.Tests.Unit;

public class DomainTests
{
    [Fact]
    public void Vehicle_UpdateMileage_Should_Update_Valid_Mileage()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var plate = LicensePlate.Create("AB-123-CD");
        var vehicle = Vehicle.Create(customerId, plate, "Peugeot", "208", 2020, 15000);

        // Act
        vehicle.UpdateMileage(20000);

        // Assert
        vehicle.Mileage.Should().Be(20000);
        vehicle.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Vehicle_UpdateMileage_Should_Throw_If_Mileage_Lower_Than_Current()
    {
        var plate = LicensePlate.Create("XX-999-YY");
        var vehicle = Vehicle.Create(Guid.NewGuid(), plate, "Renault", "Clio", 2019, 50000);
        
        Action act = () => vehicle.UpdateMileage(40000);
        
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("New mileage must be greater than current.");
    }

    [Fact]
    public void Part_DecrementStock_Should_Throw_If_Stock_Insufficient()
    {
        var part = Part.Create("REF-01", "Filtre", "Engine", Money.Create(10), 20, "BrandX");
        
        Action act = () => part.AdjustStock(-30); 
        
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("Insufficient stock.");
    }

}

public class CqrsTests
{
    [Fact]
    public async Task CreateVehicleHandler_Should_Return_VehicleDto_And_Add_To_Repo()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateVehicleCommand(customerId, "ZZ-111-AA", "BMW", "Serie 1", 2023, 1000);

        var uowMock = new Mock<IUnitOfWork>();
        var vehicleRepoMock = new Mock<IVehicleRepository>();
        
        var handler = new CreateVehicleHandler(vehicleRepoMock.Object, uowMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value?.Make.Should().Be("BMW");
        vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class SatisfactionTests
{
    [Fact]
    public void CalculateNps_Should_Return_Correct_Score()
    {
        // 10 scores: 5 promoters (9,10), 3 passives (7,8), 2 detractors (0-6)
        var scores = new List<int> { 10, 10, 9, 9, 9, 8, 8, 7, 5, 2 };
        
        // promoters: 5 (50%), detractors: 2 (20%). NPS = 50 - 20 = 30.
        var result = SatisfactionService.CalculateNps(scores);
        
        result.Should().Be(30.0);
    }

    [Fact]
    public void CalculateNps_Should_Return_Negative_100_If_All_Detractors()
    {
        var scores = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
        var result = SatisfactionService.CalculateNps(scores);
        result.Should().Be(-100.0);
    }

    [Fact]
    public void SurveyCampaign_RegisterResponse_Should_Throw_If_Score_Invalid()
    {
        var survey = SurveyCampaign.Create(Guid.NewGuid(), Guid.NewGuid());
        
        Action act = () => survey.RegisterResponse(11, "Too much!");
        
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("NPS Score must be between 0 and 10.");
    }

    [Fact]
    public void SurveyCampaign_RegisterResponse_Should_Update_Fields()
    {
        var survey = SurveyCampaign.Create(Guid.NewGuid(), Guid.NewGuid());
        survey.RegisterResponse(9, "Parfait!");
        
        survey.NpsScore.Should().Be(9);
        survey.Comment.Should().Be("Parfait!");
    }
}

public class MaintenanceTests
{
    [Theory]
    [InlineData("Huile Moteur", 15000, 0, 100)] // Exactly the threshold
    [InlineData("Huile Moteur", 7500, 0, 50)]   // Halfway
    [InlineData("Huile Moteur", 20000, 0, 100)] // Over threshold
    [InlineData("Huile Moteur", 10000, 10000, 0)] // Just changed
    [InlineData("Inconnu", 1000000, 0, 0)] // Unknown part
    [InlineData("Plaquettes Freins", 10000, 0, 25)] // 10k of 40k
    public void CalculateUrgency_Should_Return_Expected_Values(string part, int cur, int last, int expected)
    {
        var result = MaintenanceService.CalculateUrgency(part, cur, last);
        result.Should().Be(expected);
    }
}

public class LoyaltyTests
{
    [Theory]
    [InlineData(100, null, false, 10)] // 100€ = 10pts
    [InlineData(500, "Diagnostic_IA", false, 100)] // 50pts + 50 bonus
    [InlineData(100, "Checkup", true, 30)] // 10pts + 20 B2B bonus
    [InlineData(50, null, false, 5)] // 50€ = 5pts
    [InlineData(0, null, false, 0)]  // 0€ = 0pts
    public void CalculatePoints_Should_Return_Correct_Values(decimal amount, string? type, bool b2b, int expected)
    {
        var result = LoyaltyService.CalculatePoints(amount, type, b2b);
        result.Should().Be(expected);
    }
}
