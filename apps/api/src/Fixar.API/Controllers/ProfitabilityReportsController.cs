using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Application.Features.Profitability;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize(Policy = AuthorizationPolicies.CanViewProfitability)]
[Route("api/v{version:apiVersion}/profitability")]
public sealed class ProfitabilityReportsController(IProfitabilityReportService reports, ApplicationDbContext db, ICurrentUserService user) : ControllerBase
{
    [HttpGet("executive-summary"), Authorize(Policy = AuthorizationPolicies.CanViewExecutiveDashboard)] public async Task<IActionResult> Executive([FromQuery] ReportQuery q, CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(await reports.GetExecutiveSummaryAsync(q.ToFilter(), ct)));
    [HttpGet("customers")] public async Task<IActionResult> Customers([FromQuery] ReportQuery q, CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(await reports.GetCustomerProfitabilityAsync(q.ToFilter(), ct)));
    [HttpGet("customers/{id:guid}")] public async Task<IActionResult> Customer(Guid id, [FromQuery] ReportQuery q, CancellationToken ct) { var x=(await reports.GetCustomerProfitabilityAsync(q.ToFilter() with { CustomerId=id },ct)).FirstOrDefault();return x is null?NotFound(ApiResponse<object>.Fail("Bu müşteri için kârlılık kaydı bulunamadı.","NOT_FOUND")):Ok(ApiResponse<object>.SuccessResponse(x)); }
    [HttpGet("orders")] public async Task<IActionResult> Orders([FromQuery] ReportQuery q, CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(await reports.GetOrderProfitabilityAsync(q.ToFilter(), ct)));
    [HttpGet("orders/{id:guid}")] public async Task<IActionResult> Order(Guid id,[FromQuery] ReportQuery q,CancellationToken ct){var x=(await reports.GetOrderProfitabilityAsync(q.ToFilter() with {OrderId=id},ct)).FirstOrDefault();return x is null?NotFound(ApiResponse<object>.Fail("Bu sipariş için tamamlanmış maliyet snapshot’ı bulunamadı.","NOT_FOUND")):Ok(ApiResponse<object>.SuccessResponse(x));}
    [HttpGet("products")] public async Task<IActionResult> Products([FromQuery] ReportQuery q, CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(await reports.GetProductProfitabilityAsync(q.ToFilter(), ct)));
    [HttpGet("products/{id:guid}")] public async Task<IActionResult> Product(Guid id,[FromQuery] ReportQuery q,CancellationToken ct){var x=(await reports.GetProductProfitabilityAsync(q.ToFilter() with {ProductId=id},ct)).FirstOrDefault();return x is null?NotFound(ApiResponse<object>.Fail("Bu ürün için yeterli üretim maliyeti bulunamadı.","NOT_FOUND")):Ok(ApiResponse<object>.SuccessResponse(x));}
    [HttpGet("work-orders")] public async Task<IActionResult> WorkOrders([FromQuery] ReportQuery q,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await reports.GetWorkOrderProfitabilityAsync(q.ToFilter(),ct)));
    [HttpGet("monthly-trend")] public async Task<IActionResult> Trend([FromQuery] ReportQuery q,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await reports.GetMonthlyTrendAsync(q.ToFilter(),ct)));
    [HttpGet("cost-breakdown")] public async Task<IActionResult> Breakdown([FromQuery] ReportQuery q,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await reports.GetCostCategoryBreakdownAsync(q.ToFilter(),ct)));
    [HttpGet("top-bottom")] public async Task<IActionResult> TopBottom([FromQuery] ReportQuery q,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await reports.GetTopAndBottomPerformersAsync(q.ToFilter(),ct)));
    [HttpGet("data-quality")] public async Task<IActionResult> Quality([FromQuery] ReportQuery q,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await reports.GetDataQualityAsync(q.ToFilter(),ct)));
    [HttpGet("export")]
    public async Task<IActionResult> Export(string report,[FromQuery] ReportQuery q,CancellationToken ct){var f=q.ToFilter();IEnumerable<object> rows=report.ToLowerInvariant() switch{"customers"=>(await reports.GetCustomerProfitabilityAsync(f,ct)).Cast<object>(),"orders"=>(await reports.GetOrderProfitabilityAsync(f,ct)).Cast<object>(),"products"=>(await reports.GetProductProfitabilityAsync(f,ct)).Cast<object>(),"work-orders"=>(await reports.GetWorkOrderProfitabilityAsync(f,ct)).Cast<object>(),_=>throw new ArgumentException("Geçersiz rapor tipi.")};var list=rows.ToList();var sb=new StringBuilder("\uFEFFRaporlama Para Birimi;"+f.Currency+"\n");if(list.Count>0){var props=list[0].GetType().GetProperties().Where(x=>x.PropertyType.IsPrimitive||x.PropertyType==typeof(string)||x.PropertyType==typeof(decimal)||x.PropertyType==typeof(decimal?)||x.PropertyType==typeof(DateTime)||x.PropertyType==typeof(DateTime?)).ToList();sb.AppendLine(string.Join(';',props.Select(x=>CsvHeader(x.Name))));foreach(var row in list)sb.AppendLine(string.Join(';',props.Select(x=>(x.GetValue(row)?.ToString()??"").Replace(';',','))));}db.AuditLogs.Add(new AuditLog{UserId=user.UserId,UserName=user.UserName,Action=AuditAction.Create,EntityName="Profitability Report Exported",EntityId=report,NewValues=JsonSerializer.Serialize(f),Timestamp=DateTime.UtcNow,IpAddress=user.IpAddress});await db.SaveChangesAsync(ct);return File(Encoding.UTF8.GetBytes(sb.ToString()),"text/csv; charset=utf-8",$"karlilik-{report}-{DateTime.UtcNow:yyyyMMdd}.csv");}
    private static string CsvHeader(string name) => name switch { "CustomerCode" => "Müşteri Kodu", "CustomerName" => "Müşteri", "OrderNumber" => "Sipariş No", "WorkOrderNumber" => "İş Emri", "ProductCode" => "Ürün Kodu", "ProductName" => "Ürün", "OrderCount" => "Sipariş Sayısı", "WorkOrderCount" => "İş Emri Sayısı", "TotalProducedPairs" or "ProducedPairs" => "Üretilen Çift", "GoodPairs" => "İyi Ürün", "FirePairs" or "TotalFirePairs" => "Fire Çifti", "FireRatePercent" => "Fire Oranı %", "NetSalesRevenue" or "AllocatedRevenue" or "SalesRevenue" => "Net Satış Geliri", "TotalActualCost" or "ActualCost" => "Gerçek Maliyet", "GrossProfit" => "Brüt Kâr", "GrossMarginPercent" => "Brüt Marj %", "DataCompletenessPercent" => "Veri Tamlığı %", "SnapshotDate" => "Snapshot Tarihi", "ProfitabilityStatus" => "Kârlılık Durumu", _ => name };
}
public sealed class ReportQuery{public DateTime? DateFrom{get;set;}public DateTime? DateTo{get;set;}public string Currency{get;set;}="TRY";public Guid? CustomerId{get;set;}public Guid? OrderId{get;set;}public Guid? ProductId{get;set;}public Guid? WorkOrderId{get;set;}public string? ProductionType{get;set;}public bool IncludeIncomplete{get;set;}=true;public int Limit{get;set;}=10;public string? Search{get;set;}public ProfitabilityFilter ToFilter()=>new(DateFrom,DateTo,Currency,CustomerId,OrderId,ProductId,WorkOrderId,ProductionType,IncludeIncomplete,Limit,Search);}
