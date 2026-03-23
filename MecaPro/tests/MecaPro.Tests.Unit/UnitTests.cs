using Xunit;
using Moq;
using FluentAssertions;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Inventory;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Operations;
using MecaPro.Application.Modules.Customers;
using MecaPro.Application.Modules.Inventory;

namespace MecaPro.Tests.Unit;

public class DomainTests
{
    [Fact]
    public void Vehicle_UpdateMileage_Should_Update_Valid_Mileage()
    {
        var customerId = Guid.NewGuid();
        var plate = LicensePlate.Create("AB-123-CD");
        var vehicle = Vehicle.Create(customerId, plate, "Peugeot", "208", 2020, 15000);
        vehicle.UpdateMileage(20000);
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
        var customerId = Guid.NewGuid();
        var command = new CreateVehicleCommand(customerId, "ZZ-111-AA", "BMW", "Serie 1", 2023, 1000);
        var uowMock = new Mock<IUnitOfWork>();
        var vehicleRepoMock = new Mock<IVehicleRepository>();
        var customerRepoMock = new Mock<ICustomerRepository>();
        var revisionRepoMock = new Mock<IRevisionRepository>();
        var partRepoMock = new Mock<IPartRepository>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        
        var handler = new OperationsHandlers(vehicleRepoMock.Object, revisionRepoMock.Object, customerRepoMock.Object, partRepoMock.Object, uowMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value?.Make.Should().Be("BMW");
        vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class MaintenanceTests
{
    [Theory]
    [InlineData("Huile Moteur", 15000, 0, 100)]
    [InlineData("Huile Moteur", 7500, 0, 50)]
    [InlineData("Huile Moteur", 20000, 0, 100)]
    [InlineData("Huile Moteur", 10000, 10000, 0)]
    [InlineData("Inconnu", 1000000, 0, 0)]
    [InlineData("Plaquettes Freins", 10000, 0, 25)]
    public void CalculateUrgency_Should_Return_Expected_Values(string part, int cur, int last, int expected)
    {
        var result = MaintenanceService.CalculateUrgency(part, cur, last);
        result.Should().Be(expected);
    }
}

public class LoyaltyTests
{
    [Theory]
    [InlineData(100, null, false, 10)]
    [InlineData(500, "Diagnostic_IA", false, 100)]
    [InlineData(100, "Checkup", true, 30)]
    [InlineData(50, null, false, 5)]
    [InlineData(0, null, false, 0)]
    public void CalculatePoints_Should_Return_Correct_Values(decimal amount, string? type, bool b2b, int expected)
    {
        var result = LoyaltyService.CalculatePoints(amount, type, b2b);
        result.Should().Be(expected);
    }
}
