// ============================================================
// BACKEND ENDPOINTS RESTANTS + CART SERVICE + CSS COMPLÉMENTS
// ============================================================

// ─────────────────────────────────────────────────────────────
// ENDPOINTS — Véhicules, Diagnostics, Révisions, Tâches, Pièces
// ─────────────────────────────────────────────────────────────

namespace MecaPro.API.Endpoints;

public class VehicleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/vehicles")
            .RequireAuthorization()
            .WithTags("Vehicles");

        grp.MapGet("/", async (
            [FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] string? search, [FromQuery] string? status,
            [FromQuery] string? make, [FromQuery] string? sortBy,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehiclesPagedQuery(page, pageSize, search, null));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapGet("/by-qr/{token}", async (string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByQrQuery(token));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapPut("/{id:guid}", async (Guid id, UpdateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd with { VehicleId = id });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");

        grp.MapGet("/{id:guid}/qr", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetQrCodeQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapPost("/{id:guid}/qr/regenerate", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GenerateQrCodeCommand(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");

        grp.MapPost("/{id:guid}/images", async (
            Guid id, IFormFile file, IBlobStorageService blob, AppDbContext db) =>
        {
            if (file.Length > 10_000_000)
                return Results.BadRequest("Fichier trop volumineux (max 10 MB).");

            using var stream = file.OpenReadStream();
            var bytes = new byte[file.Length];
            await stream.ReadAsync(bytes);

            var blobUrl = await blob.UploadAsync(
                $"vehicles/{id}/{Guid.NewGuid()}-{file.FileName}",
                bytes, file.ContentType);

            var image = VehicleImage.Create(file.FileName, blobUrl, file.Length);
            var vehicle = await db.Vehicles.Include(v => v.Images).FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null) return Results.NotFound();
            vehicle.AddImage(image);
            await db.SaveChangesAsync();

            return Results.Ok(new { url = blobUrl, id = image.Id });
        }).RequireAuthorization("RequireMechanic").DisableAntiforgery();

        grp.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteVehicleCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("RequireGarageOwner");
    }
}

public class DiagnosticModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/diagnostics")
            .RequireAuthorization("RequireMechanic")
            .WithTags("Diagnostics");

        grp.MapGet("/", async (
            [FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] string? search, [FromQuery] string? status,
            [FromQuery] string? severity, AppDbContext db) =>
        {
            var query = db.Diagnostics
                .Include("Vehicle").Include("Mechanic")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(d => d.FaultCode.Contains(search) || d.Description.Contains(search));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status.ToString() == status);
            if (!string.IsNullOrEmpty(severity) && severity != "Resolved")
                query = query.Where(d => d.Severity.ToString() == severity);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(d => d.DiagnosedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Results.Ok(new
            {
                items = items.Select(d => d.ToDto()),
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                criticalCount = await db.Diagnostics.CountAsync(d => d.Severity == DiagnosticSeverity.Critical && d.Status != DiagnosticStatus.Resolved),
                majorCount = await db.Diagnostics.CountAsync(d => d.Severity == DiagnosticSeverity.Major && d.Status != DiagnosticStatus.Resolved),
                minorCount = await db.Diagnostics.CountAsync(d => d.Severity == DiagnosticSeverity.Minor && d.Status != DiagnosticStatus.Resolved),
                infoCount = await db.Diagnostics.CountAsync(d => d.Severity == DiagnosticSeverity.Info && d.Status != DiagnosticStatus.Resolved),
                resolvedCount = await db.Diagnostics.CountAsync(d => d.Status == DiagnosticStatus.Resolved)
            });
        });

        grp.MapPost("/", async (AddDiagnosticCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created("", result.Value) : Results.BadRequest(result.Errors);
        });

        grp.MapPost("/{id:guid}/resolve", async (
            Guid id, ResolveDiagnosticDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new ResolveDiagnosticCommand(id, dto.Resolution));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var diag = await db.Diagnostics.FirstOrDefaultAsync(d => d.Id == id);
            return diag != null ? Results.Ok(diag.ToDto()) : Results.NotFound();
        });
    }
}

public class RevisionModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/revisions")
            .RequireAuthorization("RequireMechanic")
            .WithTags("Revisions");

        grp.MapGet("/", async (
            [FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] string? search, [FromQuery] string? status,
            [FromQuery] string? dateFilter, AppDbContext db) =>
        {
            var query = db.Revisions.AsQueryable();

            if (dateFilter == "today")
                query = query.Where(r => r.ScheduledDate.Date == DateTime.UtcNow.Date);
            else if (dateFilter == "week")
            {
                var weekEnd = DateTime.UtcNow.AddDays(7);
                query = query.Where(r => r.ScheduledDate >= DateTime.UtcNow && r.ScheduledDate <= weekEnd);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status.ToString() == status);

            var total = await query.CountAsync();
            var items = await query.OrderBy(r => r.ScheduledDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Results.Ok(new
            {
                items = items.Select(r => r.ToDto()),
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                todayCount = await db.Revisions.CountAsync(r => r.ScheduledDate.Date == DateTime.UtcNow.Date),
                weekCount = await db.Revisions.CountAsync(r => r.ScheduledDate >= DateTime.UtcNow && r.ScheduledDate <= DateTime.UtcNow.AddDays(7)),
                inProgressCount = await db.Revisions.CountAsync(r => r.Status == RevisionStatus.InProgress),
                completedCount = await db.Revisions.CountAsync(r => r.Status == RevisionStatus.Completed)
            });
        });

        grp.MapPost("/", async (ScheduleRevisionCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created("", result.Value) : Results.BadRequest(result.Errors);
        });

        grp.MapPost("/{id:guid}/start", async (Guid id, AppDbContext db) =>
        {
            var rev = await db.Revisions.FindAsync(id);
            if (rev == null) return Results.NotFound();
            rev.Start();
            await db.SaveChangesAsync();
            return Results.Ok(rev.ToDto());
        });

        grp.MapPost("/{id:guid}/complete", async (
            Guid id, CompleteRevisionDto dto, AppDbContext db,
            IInvoiceService invoices, ICurrentUserService user) =>
        {
            var rev = await db.Revisions.FindAsync(id);
            if (rev == null) return Results.NotFound();
            rev.Complete(dto.ActualMinutes, Money.Create(dto.ActualCost), dto.Notes);
            await db.SaveChangesAsync();

            // Auto-generate invoice
            var vehicle = await db.Vehicles.FindAsync(rev.VehicleId);
            if (vehicle != null)
            {
                var garageId = Guid.Parse(user.UserId ?? Guid.Empty.ToString());
                await invoices.GenerateAsync(new GenerateInvoiceCommand(
                    vehicle.CustomerId, garageId,
                    new List<InvoiceLine> { new(rev.Type, 1, dto.ActualCost) }));
            }

            return Results.Ok(rev.ToDto());
        });
    }
}

public class DashboardModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/dashboard")
            .RequireAuthorization()
            .WithTags("Dashboard");

        grp.MapGet("/stats", async (AppDbContext db, ICurrentUserService user) =>
        {
            var garageId = Guid.Parse(user.UserId ?? Guid.Empty.ToString());
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            return Results.Ok(new
            {
                vehiclesInProgress = await db.Revisions.CountAsync(r =>
                    r.Status == RevisionStatus.InProgress),
                activeDiagnostics = await db.Diagnostics.CountAsync(d =>
                    d.Status != DiagnosticStatus.Resolved),
                completedRevisions = await db.Revisions.CountAsync(r =>
                    r.Status == RevisionStatus.Completed && r.CompletedDate >= monthStart),
                monthlyRevenue = await db.Invoices.Where(i =>
                    i.Status == "paid" && i.PaidAt >= monthStart)
                    .SumAsync(i => i.TotalTTC),
                totalClients = await db.Customers.CountAsync(),
                lowStockParts = await db.Parts.CountAsync(p =>
                    p.StockQuantity <= p.MinStockAlert),
                todayRevisions = await db.Revisions.CountAsync(r =>
                    r.ScheduledDate.Date == now.Date),
                unreadMessages = await db.ChatMessages.CountAsync(m =>
                    !m.IsRead && m.RecipientId == user.UserId)
            });
        });

        grp.MapGet("/alerts", async (AppDbContext db) =>
        {
            var alerts = new List<object>();

            var critDiags = await db.Diagnostics
                .Include("Vehicle")
                .Where(d => d.Severity == DiagnosticSeverity.Critical && d.Status != DiagnosticStatus.Resolved)
                .Take(3).ToListAsync();

            foreach (var d in critDiags)
                alerts.Add(new { title = $"🔴 Panne critique — {d.FaultCode}", description = d.Description, type = "danger", actionUrl = $"/diagnostics/{d.Id}" });

            var lowStock = await db.Parts.Where(p => p.StockQuantity <= p.MinStockAlert).Take(2).ToListAsync();
            foreach (var p in lowStock)
                alerts.Add(new { title = $"⚠ Stock critique — {p.Name}", description = $"Plus que {p.StockQuantity} unité(s) en stock.", type = "warning", actionUrl = $"/parts/{p.Id}" });

            return Results.Ok(alerts);
        });

        grp.MapGet("/revenue", async (AppDbContext db) =>
        {
            var months = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-i)).Reverse();
            var data = new List<object>();
            foreach (var month in months)
            {
                var start = new DateTime(month.Year, month.Month, 1);
                var end = start.AddMonths(1);
                var revRevenue = await db.Invoices
                    .Where(i => i.Status == "paid" && i.IssuedAt >= start && i.IssuedAt < end)
                    .SumAsync(i => i.TotalTTC);
                data.Add(new { month = month.ToString("MMM"), revisions = revRevenue * .7m, parts = revRevenue * .3m });
            }
            return Results.Ok(data);
        });
    }
}

// ─────────────────────────────────────────────────────────────
// CART SERVICE (Blazor)
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Blazor.Services;

public class CartService
{
    private readonly List<CartItem> _items = new();
    public event Action? OnChanged;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public int ItemCount => _items.Sum(i => i.Quantity);
    public decimal TotalHT => _items.Sum(i => i.UnitPrice * i.Quantity);
    public decimal TVA => TotalHT * 0.20m;
    public decimal TotalTTC => TotalHT + TVA;

    public void Add(CartItem item)
    {
        var existing = _items.FirstOrDefault(i => i.PartId == item.PartId);
        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            _items.Add(item);
        OnChanged?.Invoke();
    }

    public void Increase(Guid partId)
    {
        var item = _items.FirstOrDefault(i => i.PartId == partId);
        if (item != null) { item.Quantity++; OnChanged?.Invoke(); }
    }

    public void Decrease(Guid partId)
    {
        var item = _items.FirstOrDefault(i => i.PartId == partId);
        if (item != null)
        {
            item.Quantity--;
            if (item.Quantity <= 0) _items.Remove(item);
            OnChanged?.Invoke();
        }
    }

    public void Remove(Guid partId)
    {
        _items.RemoveAll(i => i.PartId == partId);
        OnChanged?.Invoke();
    }

    public void Clear() { _items.Clear(); OnChanged?.Invoke(); }
}

public class CartItem
{
    public Guid PartId { get; }
    public string Name { get; }
    public string Reference { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; set; }

    public CartItem(Guid partId, string name, string reference, decimal unitPrice, int quantity = 1)
    { PartId = partId; Name = name; Reference = reference; UnitPrice = unitPrice; Quantity = quantity; }
}

// ─────────────────────────────────────────────────────────────
// ADDITIONAL DTOS FOR ENDPOINTS
// ─────────────────────────────────────────────────────────────

public record ResolveDiagnosticDto(string Resolution);
public record CompleteRevisionDto(int ActualMinutes, decimal ActualCost, string? Notes);

public record PartPagedResult(
    IEnumerable<PartDto> Items, int Total, int TotalPages, int LowStockCount);

public record ShopPartDto(
    Guid Id, string Reference, string Name, string Category, string? Brand,
    decimal UnitPrice, int StockQuantity, bool IsLowStock, bool IsAvailable, string? ImageUrl);

public record ShopPartPagedResult(IEnumerable<ShopPartDto> Items, int Total, int TotalPages);
public record ShopStatsDto(decimal MonthRevenue, int MonthOrders, decimal MonthPurchases, decimal MarginPercent);
public record CheckoutIntentDto(string ClientSecret, string PaymentIntentId, Guid OrderId);

// ─────────────────────────────────────────────────────────────
// CSS COMPLÉMENTS (à ajouter dans mecapro.css)
// ─────────────────────────────────────────────────────────────

/*
── DIAGNOSTICS ───────────────────────────────────────────────

.severity-row { display:grid; grid-template-columns:repeat(5,1fr); gap:12px; margin-bottom:22px; }

.severity-card {
  background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg);
  padding:16px; text-align:center; cursor:pointer; transition:all .2s;
}
.severity-card:hover,.severity-card.active { transform:translateY(-2px); box-shadow:var(--shadow); }
.severity-card.critical.active { border-color:var(--red); background:var(--red-d); }
.severity-card.major.active    { border-color:var(--orange); background:var(--orange-d); }
.severity-card.minor.active    { border-color:var(--amber); background:var(--amber-d); }
.severity-card.info.active     { border-color:var(--blue); background:var(--blue-d); }
.severity-card.resolved.active { border-color:var(--green); background:var(--green-d); }

.sev-icon  { font-size:24px; margin-bottom:6px; }
.sev-count { font-family:var(--font-cond); font-size:28px; font-weight:800; }
.sev-label { font-size:11px; color:var(--text2); }

.diag-list { display:flex; flex-direction:column; gap:10px; margin-bottom:20px; }

.diag-card {
  display:flex; gap:0; background:var(--card); border:1px solid var(--border);
  border-radius:var(--radius); overflow:hidden; cursor:pointer; transition:all .15s;
}
.diag-card:hover { border-color:var(--border2); }
.diag-card.resolved { opacity:.65; }

.diag-card-severity { width:4px; flex-shrink:0; }
.diag-card.severity-critical .diag-card-severity { background:var(--red); }
.diag-card.severity-major    .diag-card-severity { background:var(--orange); }
.diag-card.severity-minor    .diag-card-severity { background:var(--amber); }
.diag-card.severity-info     .diag-card-severity { background:var(--blue); }

.diag-card-body { flex:1; padding:14px 16px; }
.diag-card-header { display:flex; align-items:center; gap:10px; margin-bottom:6px; }
.diag-card-code { font-family:var(--font-mono); font-size:13px; color:var(--amber); font-weight:700; }

.diag-card-sev-badge { font-size:10px; font-weight:700; padding:2px 8px; border-radius:99px; }
.sev-critical { background:var(--red-d); color:var(--red); }
.sev-major    { background:var(--orange-d); color:var(--orange); }
.sev-minor    { background:var(--amber-d); color:var(--amber); }
.sev-info     { background:var(--blue-d); color:var(--blue); }

.diag-card-desc { font-size:13px; font-weight:500; margin-bottom:8px; }
.diag-card-meta { display:flex; flex-wrap:wrap; gap:14px; font-size:12px; color:var(--text2); }
.diag-plate { font-family:var(--font-mono); color:var(--amber); }
.tool-badge { background:var(--purple-d); color:var(--purple); padding:2px 8px; border-radius:4px; font-size:10px; }
.diag-causes { font-size:12px; color:var(--text2); margin-top:8px; padding:8px 10px; background:var(--panel); border-radius:6px; }
.diag-card-actions { display:flex; flex-direction:column; gap:6px; padding:12px; justify-content:center; border-left:1px solid var(--border); }

── REVISIONS KPI ─────────────────────────────────────────────

.rev-kpi-row { display:grid; grid-template-columns:repeat(6,1fr); gap:12px; margin-bottom:22px; }
.rev-kpi {
  background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg);
  padding:14px; text-align:center; cursor:pointer; transition:all .15s;
}
.rev-kpi:hover,.rev-kpi.active { border-color:var(--amber); background:var(--amber-d); }
.rev-kpi-val { font-family:var(--font-cond); font-size:24px; font-weight:800; }
.rev-kpi-label { font-size:11px; color:var(--text2); margin-top:2px; }

── KANBAN ────────────────────────────────────────────────────

.kanban-board { display:grid; grid-template-columns:repeat(4,1fr); gap:16px; align-items:start; }

.kanban-col { background:var(--surface); border:1px solid var(--border); border-radius:var(--radius-lg); overflow:hidden; }

.kanban-col-header {
  padding:12px 14px; display:flex; align-items:center; justify-content:space-between;
  font-family:var(--font-cond); font-size:14px; font-weight:700; letter-spacing:.3px;
  border-bottom:1px solid var(--border);
}
.col-pending  .kanban-col-header { background:rgba(100,116,139,.08); }
.col-progress .kanban-col-header { background:var(--amber-d); color:var(--amber); }
.col-blocked  .kanban-col-header { background:var(--red-d); color:var(--red); }
.col-done     .kanban-col-header { background:var(--green-d); color:var(--green); }

.kanban-count { background:var(--panel); padding:2px 8px; border-radius:99px; font-size:11px; }

.kanban-cards { padding:10px; display:flex; flex-direction:column; gap:8px; min-height:100px; }

.kanban-card {
  background:var(--card); border:1px solid var(--border); border-radius:var(--radius);
  padding:12px; cursor:pointer; transition:all .15s;
}
.kanban-card:hover { border-color:var(--border2); transform:translateY(-1px); box-shadow:var(--shadow-sm); }

.kc-header { display:flex; justify-content:space-between; margin-bottom:6px; }
.kc-id     { font-family:var(--font-mono); font-size:10px; color:var(--text3); }
.kc-cost   { font-family:var(--font-mono); font-size:11px; color:var(--amber); font-weight:700; }
.kc-title  { font-size:13px; font-weight:600; margin-bottom:4px; }
.kc-plate  { font-family:var(--font-mono); font-size:10px; color:var(--amber); margin-bottom:6px; }
.kc-meta   { display:flex; justify-content:space-between; font-size:11px; color:var(--text2); margin-bottom:6px; }
.kc-parts  { font-size:11px; color:var(--text3); margin-bottom:6px; }
.kc-footer { display:flex; justify-content:space-between; align-items:center; }

.table-total td { border-top:2px solid var(--border2); font-size:13px; padding-top:12px; }
.text-right { text-align:right; }

── PARTS ─────────────────────────────────────────────────────

.category-tabs { display:flex; gap:6px; flex-wrap:wrap; margin-bottom:18px; }
.cat-tab {
  padding:6px 14px; border-radius:99px; font-size:12px; font-weight:600;
  background:var(--surface); border:1px solid var(--border); color:var(--text2); transition:all .15s;
}
.cat-tab:hover { color:var(--text); border-color:var(--border2); }
.cat-tab.active { background:var(--amber-d); border-color:rgba(245,166,35,.3); color:var(--amber); }

.parts-grid { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:20px; }
.part-card {
  background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg);
  overflow:hidden; transition:all .2s;
}
.part-card:hover { border-color:var(--border2); transform:translateY(-2px); }
.part-card.low-stock { border-color:rgba(239,68,68,.3); }

.part-img { height:100px; background:var(--surface); display:flex; align-items:center; justify-content:center; border-bottom:1px solid var(--border); overflow:hidden; }
.part-img img { width:100%; height:100%; object-fit:contain; }
.part-img-placeholder { font-size:36px; opacity:.4; }
.part-body { padding:12px; }
.part-ref   { font-family:var(--font-mono); font-size:10px; color:var(--amber); margin-bottom:4px; }
.part-name  { font-size:13px; font-weight:600; margin-bottom:3px; }
.part-brand { font-size:11px; color:var(--text3); margin-bottom:3px; }
.part-compat{ font-size:10px; }
.part-footer{ display:flex; justify-content:space-between; align-items:center; padding:10px 12px; border-top:1px solid var(--border); }
.part-price { font-family:var(--font-cond); font-size:18px; font-weight:800; color:var(--amber); }
.part-stock { font-family:var(--font-mono); font-size:11px; }
.part-actions { display:flex; gap:6px; padding:10px 12px; border-top:1px solid var(--border); }

.stock-modal { display:flex; flex-direction:column; gap:12px; align-items:center; }
.stock-current,.stock-preview { font-size:14px; color:var(--text2); }
.stock-controls { display:flex; align-items:center; gap:12px; }

── SHOP ──────────────────────────────────────────────────────

.shop-stats-row { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:22px; }
.shop-stat-card { background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg); padding:16px; }
.shop-stat-val { font-family:var(--font-cond); font-size:26px; font-weight:800; }
.shop-stat-label { font-size:12px; color:var(--text2); margin:2px 0; }
.shop-stat-trend { font-size:11px; font-weight:700; }

.shop-tabs { display:flex; gap:4px; background:var(--surface); border-radius:10px; padding:4px; margin-bottom:22px; border:1px solid var(--border); }
.shop-tab { padding:8px 16px; border-radius:8px; font-size:12px; font-weight:600; color:var(--text3); transition:all .15s; }
.shop-tab:hover { color:var(--text); }
.shop-tab.active { background:linear-gradient(135deg,var(--amber),var(--amber2)); color:var(--base); }

.shop-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:16px; margin-bottom:20px; }
.shop-card { background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg); overflow:hidden; transition:all .2s; }
.shop-card:hover { border-color:var(--blue); transform:translateY(-3px); box-shadow:0 8px 24px rgba(59,130,246,.12); }

.shop-img { height:120px; background:var(--surface); display:flex; align-items:center; justify-content:center; border-bottom:1px solid var(--border); position:relative; overflow:hidden; }
.shop-img img { width:100%; height:100%; object-fit:contain; }
.shop-img-placeholder { font-size:48px; opacity:.3; }
.stock-badge { position:absolute; top:8px; right:8px; font-size:9px; font-weight:800; padding:3px 8px; border-radius:99px; }
.stock-badge.low { background:var(--amber-d); color:var(--amber); }
.stock-badge.out { background:var(--red-d); color:var(--red); }

.shop-body { padding:14px; }
.shop-brand { font-size:10px; color:var(--text3); text-transform:uppercase; letter-spacing:1px; margin-bottom:4px; }
.shop-name  { font-size:13px; font-weight:600; margin-bottom:4px; }
.shop-ref   { font-family:var(--font-mono); font-size:10px; color:var(--text3); margin-bottom:10px; }
.shop-price-row { display:flex; justify-content:space-between; align-items:center; margin-bottom:12px; }
.shop-price { font-family:var(--font-cond); font-size:22px; font-weight:800; color:var(--blue); }
.shop-stock { font-family:var(--font-mono); font-size:10px; }
.shop-actions { display:flex; gap:8px; align-items:center; }
.qty-control { display:flex; align-items:center; gap:6px; background:var(--surface); border:1px solid var(--border); border-radius:6px; padding:4px 8px; }
.qty-control button { color:var(--text2); font-size:16px; }
.qty-control span { font-family:var(--font-mono); font-size:13px; min-width:20px; text-align:center; }

── CART DRAWER ────────────────────────────────────────────────

.drawer-overlay { position:fixed; inset:0; background:rgba(0,0,0,.7); z-index:999; display:flex; justify-content:flex-end; }
.drawer { width:380px; background:var(--card); border-left:1px solid var(--border); display:flex; flex-direction:column; animation:slideIn .25s ease-out; height:100%; }
.drawer-header { padding:18px 20px; border-bottom:1px solid var(--border); display:flex; justify-content:space-between; align-items:center; }
.drawer-header h3 { font-family:var(--font-cond); font-size:18px; font-weight:700; }
.drawer-body { flex:1; overflow-y:auto; padding:16px; display:flex; flex-direction:column; gap:12px; }
.drawer-footer { padding:16px; border-top:1px solid var(--border); display:flex; flex-direction:column; gap:10px; }

.cart-item { display:flex; align-items:center; gap:12px; padding:10px; background:var(--surface); border:1px solid var(--border); border-radius:var(--radius); }
.cart-item-info { flex:1; }
.cart-item-name { font-size:13px; font-weight:600; }
.cart-item-ref { font-family:var(--font-mono); font-size:10px; color:var(--text3); }
.cart-item-qty { display:flex; align-items:center; gap:8px; background:var(--panel); border-radius:6px; padding:4px 8px; }
.cart-item-qty button { color:var(--text2); font-size:14px; }
.cart-item-qty span { font-family:var(--font-mono); min-width:16px; text-align:center; }
.cart-item-price { font-family:var(--font-mono); font-size:12px; color:var(--amber); font-weight:700; min-width:60px; text-align:right; }
.cart-item-remove { color:var(--text3); font-size:12px; }

.cart-total { display:flex; justify-content:space-between; font-size:13px; }
.cart-grand-total { font-size:16px; font-weight:700; color:var(--amber); padding-top:10px; border-top:1px solid var(--border); }

── INVOICES ──────────────────────────────────────────────────

.invoice-kpi-row { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:22px; }
.inv-kpi { background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg); padding:16px; text-align:center; }
.inv-kpi-val { font-family:var(--font-cond); font-size:28px; font-weight:800; }
.inv-kpi-label { font-size:12px; color:var(--text2); margin-top:3px; }

── SETTINGS ──────────────────────────────────────────────────

.settings-layout { display:grid; grid-template-columns:220px 1fr; gap:24px; align-items:start; }
.settings-nav { background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg); padding:8px; }
.settings-nav-item { display:flex; align-items:center; gap:10px; width:100%; padding:10px 12px; border-radius:var(--radius); font-size:13px; font-weight:500; color:var(--text2); transition:all .15s; margin-bottom:2px; }
.settings-nav-item:hover { background:var(--surface); color:var(--text); }
.settings-nav-item.active { background:var(--amber-d); color:var(--amber); border:1px solid rgba(245,166,35,.2); }
.settings-content { }
.settings-section { }
.settings-title { font-family:var(--font-cond); font-size:22px; font-weight:800; margin-bottom:20px; }

.settings-card { background:var(--card); border:1px solid var(--border); border-radius:var(--radius-lg); padding:20px; margin-bottom:16px; }
.settings-card-header { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:14px; }
.settings-card-title { font-family:var(--font-cond); font-size:15px; font-weight:700; margin-bottom:3px; }
.settings-card-desc { font-size:12px; color:var(--text2); }

.toggle-switch { width:44px; height:24px; border-radius:99px; background:var(--border2); position:relative; cursor:pointer; transition:background .2s; }
.toggle-switch::after { content:''; position:absolute; width:18px; height:18px; border-radius:50%; background:white; top:3px; left:3px; transition:transform .2s; }
.toggle-switch.on { background:var(--green); }
.toggle-switch.on::after { transform:translateX(20px); }

.sessions-list { display:flex; flex-direction:column; gap:10px; margin-bottom:14px; }
.session-item { display:flex; justify-content:space-between; align-items:center; padding:10px 12px; background:var(--surface); border-radius:var(--radius); font-size:13px; }

.api-key-row { display:flex; gap:8px; align-items:center; }

.notif-pref-item { display:flex; justify-content:space-between; align-items:center; padding:14px 0; border-bottom:1px solid var(--border); }
.notif-pref-item:last-child { border-bottom:none; }
.notif-pref-label { font-size:13px; font-weight:600; margin-bottom:3px; }
.notif-pref-desc  { font-size:12px; }
.notif-channels { display:flex; gap:16px; font-size:12px; }
.notif-channels label { display:flex; align-items:center; gap:5px; cursor:pointer; }
*/
