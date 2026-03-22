using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Inventory;

namespace MecaPro.Infrastructure.Persistence;

public class DatabaseSeeder(AppDbContext db)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        // ── Données de démonstration ──────────────────────────
        if (await db.Customers.AnyAsync()) return;

        // Clients
        var client1 = Customer.Create(FullName.Create("Marc", "Dupont"), Email.Create("marc.dupont@email.com"), Phone.Create("+33612345678"));
        var client2 = Customer.Create(FullName.Create("Sophie", "Martin"), Email.Create("sophie.martin@email.com"), Phone.Create("+33698765432"));
        client1.AddLoyaltyPoints(1250, "Fidélité programme - bienvenue");
        client2.AddLoyaltyPoints(2800, "Fidélité programme - bienvenue");
        db.Customers.AddRange(client1, client2);

        // Véhicules
        var v1 = Vehicle.Create(client1.Id, LicensePlate.Create("AB-123-CD"), "Peugeot", "308", 2021, 42000);
        var v2 = Vehicle.Create(client1.Id, LicensePlate.Create("EF-456-GH"), "Renault", "Clio", 2019, 78000);
        var v3 = Vehicle.Create(client2.Id, LicensePlate.Create("IJ-789-KL"), "Volkswagen", "Golf", 2022, 15000);
        var v4 = Vehicle.Create(client2.Id, LicensePlate.Create("MN-012-OP"), "Toyota", "Yaris", 2020, 55000);
        v2.SetStatus(VehicleStatus.InRepair);
        v1.SetStatus(VehicleStatus.Active);
        db.Vehicles.AddRange(v1, v2, v3, v4);

        // Diagnostics
        var mechGuid = Guid.NewGuid(); // Random ID, as user is now isolated in Auth service


        // Révisions
        var rev1 = Revision.Create(v1.Id, "Vidange + Filtres", DateTime.UtcNow.AddDays(3), 90, Money.Create(180m), 42000);
        rev1.AddTask("Vidange huile moteur 5W30", 30);
        rev1.AddTask("Remplacement filtre à huile", 15);
        rev1.AddTask("Contrôle niveaux et freins", 45);
        rev1.AddPart(Guid.NewGuid(), "Huile Moteur 5W30", 5, Money.Create(15.5m));
        rev1.AddPart(Guid.NewGuid(), "Filtre à huile Peugeot", 1, Money.Create(12.9m));

        var rev2 = Revision.Create(v2.Id, "Freins avant", DateTime.UtcNow.AddDays(-1), 120, Money.Create(350m), 78000);
        rev2.AddTask("Dépose des plaquettes usées", 30);
        rev2.AddTask("Nettoyage étriers", 20);
        rev2.AddTask("Pose plaquettes neuves", 40);
        rev2.AddTask("Purge liquide de frein", 30);
        rev2.AddPart(Guid.NewGuid(), "Plaquettes Brembo Front", 1, Money.Create(85m));
        rev2.AddPart(Guid.NewGuid(), "Liquide de frein DOT4", 1, Money.Create(12m));
        
        rev2.Start(mechGuid);
        var rev3 = Revision.Create(v3.Id, "Distribution", DateTime.UtcNow.AddDays(7), 240, Money.Create(650m), 15000);
        db.Revisions.AddRange(rev1, rev2, rev3);

        var diag1 = Diagnostic.Create(v2.Id, mechGuid, "P0301", "Raté d'allumage cylindre 1", DiagnosticSeverity.Major, "OBD-III Pro", "Bougie défectueuse ou bobine d'allumage");
        var diag2 = Diagnostic.Create(v4.Id, mechGuid, "P0420", "Efficacité catalyseur insuffisante", DiagnosticSeverity.Minor, "OBD-III Pro", "Sonde lambda ou catalyseur usé");
        db.Diagnostics.AddRange(diag1, diag2);

        // Pièces en stock
        db.Parts.AddRange(
            Part.Create("FIL-001", "Filtre à huile Peugeot", "Filtres", Money.Create(12.90m), 45, "Bosch"),
            Part.Create("PLQ-001", "Plaquettes de frein avant", "Freinage", Money.Create(48.50m), 15, "Brembo"),
            Part.Create("BOU-001", "Bougie d'allumage NGK", "Allumage", Money.Create(8.90m), 60, "NGK"),
            Part.Create("BAT-001", "Batterie 70Ah", "Électrique", Money.Create(129.00m), 3, "Varta"),
            Part.Create("AMR-002", "Amortisseurs Avant", "Suspension", Money.Create(185.00m), 12, "Monroe"),
            Part.Create("EMB-003", "Kit d'embrayage", "Transmission", Money.Create(340.00m), 1, "Sachs"),
            Part.Create("PNE-004", "Pneu Pilot Sport 5", "Pneumatiques", Money.Create(155.00m), 24, "Michelin")
        );

        // Factures
        db.Invoices.AddRange(
            new MecaPro.Domain.Modules.Invoicing.Invoice { Id = Guid.NewGuid(), Number = "INV-2025-0001", CustomerId = client1.Id, GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TotalTTC = 216.00m, Status = "Paid", IssuedAt = DateTime.UtcNow.AddDays(-30) },
            new MecaPro.Domain.Modules.Invoicing.Invoice { Id = Guid.NewGuid(), Number = "INV-2025-0002", CustomerId = client2.Id, GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TotalTTC = 420.00m, Status = "Issued", IssuedAt = DateTime.UtcNow.AddDays(-5) }
        );

        await db.SaveChangesAsync();
    }
}
