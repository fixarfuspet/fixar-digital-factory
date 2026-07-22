using Asp.Versioning;
using ClosedXML.Excel;
using Fixar.API.Security;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Route("api/v{version:apiVersion}/finance-exports")]
[Authorize(Policy = AuthorizationPolicies.CanViewFinancialAccounts)]
public sealed class FinanceExportsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("{report}.xlsx")]
    public async Task<IActionResult> Xlsx(string report, Guid? customerId, Guid? supplierId, Guid? reconciliationId, DateTime? dateFrom, DateTime? dateTo, string? currency, CancellationToken ct)
    {
        var data = await Build(report, customerId, supplierId, reconciliationId, dateFrom, dateTo, currency, ct);
        if (data.Error is not null) return data.Error;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Rapor");
        sheet.Cell(1, 1).Value = data.Title; sheet.Range(1, 1, 1, 7).Merge(); sheet.Cell(1, 1).Style.Font.Bold = true; sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(2, 1).Value = $"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm} · Hazırlayan: {User.Identity?.Name ?? "Sistem"}"; sheet.Range(2, 1, 2, 7).Merge();
        var headers = new[] { "Tarih", "Belge / Referans", "Kaynak / Kategori", "Açıklama / Karşı Taraf", "Borç / Giriş", "Alacak / Çıkış", "Bakiye" };
        for (var column = 0; column < headers.Length; column++) sheet.Cell(4, column + 1).Value = headers[column];
        sheet.Range(4, 1, 4, 7).Style.Font.Bold = true; sheet.Range(4, 1, 4, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
        var row = 5;
        foreach (var item in data.Rows)
        {
            sheet.Cell(row, 1).Value = item.Date; sheet.Cell(row, 1).Style.DateFormat.Format = "dd.mm.yyyy";
            sheet.Cell(row, 2).Value = item.Reference; sheet.Cell(row, 3).Value = item.Source; sheet.Cell(row, 4).Value = item.Description;
            sheet.Cell(row, 5).Value = item.Debit; sheet.Cell(row, 6).Value = item.Credit; sheet.Cell(row, 7).Value = item.Balance;
            sheet.Range(row, 5, row, 7).Style.NumberFormat.Format = "#,##0.00"; row++;
        }
        sheet.Cell(row, 4).Value = "TOPLAM"; sheet.Cell(row, 4).Style.Font.Bold = true;
        sheet.Cell(row, 5).FormulaA1 = $"SUM(E5:E{Math.Max(5, row - 1)})"; sheet.Cell(row, 6).FormulaA1 = $"SUM(F5:F{Math.Max(5, row - 1)})"; sheet.Cell(row, 7).Value = data.ClosingBalance;
        sheet.Range(row, 4, row, 7).Style.Font.Bold = true; sheet.Range(row, 5, row, 7).Style.NumberFormat.Format = "#,##0.00";
        sheet.SheetView.FreezeRows(4); if (row > 5) sheet.Range(4, 1, row - 1, 7).SetAutoFilter(); sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream); await Audit(report, data.Title, "XLSX", ct);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{Safe(report)}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("{report}.pdf")]
    public async Task<IActionResult> Pdf(string report, Guid? customerId, Guid? supplierId, Guid? reconciliationId, DateTime? dateFrom, DateTime? dateTo, string? currency, CancellationToken ct)
    {
        var data = await Build(report, customerId, supplierId, reconciliationId, dateFrom, dateTo, currency, ct);
        if (data.Error is not null) return data.Error;
        var actor = User.Identity?.Name ?? "Sistem";
        var bytes = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(24); page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(column => { column.Item().Text(data.Title).Bold().FontSize(16); column.Item().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm} · Hazırlayan: {actor} · Para Birimi: {data.Currency}"); column.Item().Text($"Açılış bakiyesi: {data.OpeningBalance:N2}"); });
            page.Content().PaddingVertical(12).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(52); columns.RelativeColumn(1.2f); columns.RelativeColumn(1); columns.RelativeColumn(1.7f); columns.RelativeColumn(.8f); columns.RelativeColumn(.8f); columns.RelativeColumn(.8f); });
                table.Header(header => { foreach (var value in new[] { "Tarih", "Belge", "Kaynak", "Açıklama", "Borç/Giriş", "Alacak/Çıkış", "Bakiye" }) header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(value).Bold(); });
                foreach (var item in data.Rows) { Cell(item.Date.ToString("dd.MM.yyyy")); Cell(item.Reference); Cell(item.Source); Cell(item.Description); Cell(item.Debit.ToString("N2")); Cell(item.Credit.ToString("N2")); Cell(item.Balance.ToString("N2")); }
                void Cell(string? value) => table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value ?? "-");
            });
            page.Footer().Column(column => { column.Item().AlignRight().Text($"Kapanış bakiyesi: {data.ClosingBalance:N2} {data.Currency}").Bold(); if (data.IsReconciliation) { column.Item().PaddingTop(24).Row(row => { row.RelativeItem().Text("Hazırlayan İmza: __________________"); row.RelativeItem().Text("Karşı Taraf İmza: __________________"); }); } column.Item().AlignCenter().Text(x => { x.Span("Sayfa "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); }); });
        })).GeneratePdf();
        await Audit(report, data.Title, "PDF", ct);
        return File(bytes, "application/pdf", $"{Safe(report)}-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    private async Task<ReportData> Build(string report, Guid? customerId, Guid? supplierId, Guid? reconciliationId, DateTime? from, DateTime? to, string? currency, CancellationToken ct)
    {
        var start = (from ?? DateTime.UtcNow.Date.AddMonths(-1)).ToUniversalTime(); var end = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).ToUniversalTime(); var unit = (currency ?? "TRY").ToUpperInvariant();
        if (report is "cash-transactions" or "income-expense")
        {
            var query = db.FinancialTransactions.AsNoTracking().Where(x => x.TransactionDate >= start && x.TransactionDate < end && x.Currency == unit && !x.IsReversed);
            if (report == "income-expense") query = query.Where(x => x.TransactionType == "ManualIncome" || x.TransactionType == "ManualExpense");
            var source = await query.OrderBy(x => x.TransactionDate).Select(x => new { x.TransactionDate, x.DocumentNumber, x.ReferenceNumber, x.TransactionType, Category = x.FinanceCategory != null ? x.FinanceCategory.Name : x.SourceType, x.CounterpartyName, x.Description, x.Direction, x.Amount }).ToListAsync(ct);
            decimal balance = 0; var rows = source.Select(x => { balance += x.Direction == "Inflow" ? x.Amount : -x.Amount; return new ReportRow(x.TransactionDate, x.DocumentNumber ?? x.ReferenceNumber ?? "-", x.Category, x.CounterpartyName ?? x.Description ?? "-", x.Direction == "Inflow" ? x.Amount : 0, x.Direction == "Outflow" ? x.Amount : 0, balance); }).ToList();
            return new(report == "cash-transactions" ? "Kasa ve Banka Hareketleri" : "Gelir - Gider Raporu", unit, 0, balance, rows, false, null);
        }
        if (report.StartsWith("customer-"))
        {
            if (!customerId.HasValue || !await db.Customers.AnyAsync(x => x.Id == customerId, ct)) return ReportData.Fail(NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND")));
            var opening = await db.CustomerLedgerEntries.Where(x => x.CustomerId == customerId && x.Currency == unit && x.TransactionDate < start).SumAsync(x => (decimal?)(x.DebitAmount - x.CreditAmount), ct) ?? 0;
            var source = await db.CustomerLedgerEntries.AsNoTracking().Where(x => x.CustomerId == customerId && x.Currency == unit && x.TransactionDate >= start && x.TransactionDate < end).OrderBy(x => x.TransactionDate).ThenBy(x => x.Created).ToListAsync(ct);
            decimal balance = opening; var rows = source.Select(x => { balance += x.DebitAmount - x.CreditAmount; return new ReportRow(x.TransactionDate, x.ReferenceNumber, x.SourceType, x.Description ?? "-", x.DebitAmount, x.CreditAmount, balance); }).ToList();
            return new(report == "customer-reconciliation" ? "Müşteri Mutabakatı" : "Müşteri Cari Ekstresi", unit, opening, balance, rows, report == "customer-reconciliation", null);
        }
        if (report.StartsWith("supplier-"))
        {
            if (!supplierId.HasValue || !await db.Suppliers.AnyAsync(x => x.Id == supplierId, ct)) return ReportData.Fail(NotFound(ApiResponse<object>.Fail("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND")));
            var opening = await db.SupplierLedgerEntries.Where(x => x.SupplierId == supplierId && x.Currency == unit && x.TransactionDate < start).SumAsync(x => (decimal?)(x.CreditAmount - x.DebitAmount), ct) ?? 0;
            var source = await db.SupplierLedgerEntries.AsNoTracking().Where(x => x.SupplierId == supplierId && x.Currency == unit && x.TransactionDate >= start && x.TransactionDate < end).OrderBy(x => x.TransactionDate).ThenBy(x => x.Created).ToListAsync(ct);
            decimal balance = opening; var rows = source.Select(x => { balance += x.CreditAmount - x.DebitAmount; return new ReportRow(x.TransactionDate, x.ReferenceNumber, x.SourceType, x.Description ?? "-", x.CreditAmount, x.DebitAmount, balance); }).ToList();
            return new(report == "supplier-reconciliation" ? "Tedarikçi Mutabakatı" : "Tedarikçi Cari Ekstresi", unit, opening, balance, rows, report == "supplier-reconciliation", null);
        }
        return ReportData.Fail(BadRequest(ApiResponse<object>.Fail("Geçersiz finans raporu.", "INVALID_REPORT")));
    }

    private async Task Audit(string report, string title, string format, CancellationToken ct) { db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserName = User.Identity?.Name ?? "system", Action = AuditAction.Create, EntityName = "Finance Report Exported", EntityId = report, NewValues = System.Text.Json.JsonSerializer.Serialize(new { title, format }), Timestamp = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }); await db.SaveChangesAsync(ct); }
    private static string Safe(string value) => string.Concat(value.Where(x => char.IsLetterOrDigit(x) || x == '-'));
}

public sealed record ReportRow(DateTime Date, string Reference, string Source, string Description, decimal Debit, decimal Credit, decimal Balance);
public sealed record ReportData(string Title, string Currency, decimal OpeningBalance, decimal ClosingBalance, IReadOnlyList<ReportRow> Rows, bool IsReconciliation, IActionResult? Error)
{
    public static ReportData Fail(IActionResult error) => new("", "TRY", 0, 0, [], false, error);
}
