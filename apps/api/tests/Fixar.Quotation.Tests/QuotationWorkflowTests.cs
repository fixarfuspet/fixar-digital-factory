using Fixar.API.Controllers;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Domain.Services;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class QuotationWorkflowTests
{
    [Fact] public void Quote_creation_has_safe_draft_defaults(){var q=new Quote{QuoteNumber="TKL-2026-000001",CustomerId=Guid.NewGuid(),ValidUntil=DateTime.UtcNow.AddDays(30)};Assert.Equal("Draft",q.Status);Assert.False(q.IsCancelled);}
    [Fact] public void Quote_item_can_be_added(){var q=new Quote();q.Items.Add(new QuoteItem{ProductId=Guid.NewGuid(),Quantity=10,UnitPrice=5});Assert.Single(q.Items);Assert.Equal(50,q.Items.Single().Quantity*q.Items.Single().UnitPrice);}
    [Fact] public async Task Missing_recipe_returns_explicit_warning(){await using var db=CreateDb();var(q,_)=await SeedBase(db,false);var result=await QuoteCalculationSupport.Calculate(db,q,default);Assert.Contains(result.Warnings,x=>x.Contains("aktif reçete"));Assert.Null(result.EstimatedCost);}
    [Fact] public async Task Dtf_is_not_costed_when_not_selected(){await using var db=CreateDb();var(q,materials)=await SeedBase(db,true);q.Items.Single().DtfRequired=false;var result=await QuoteCalculationSupport.Calculate(db,q,default);Assert.DoesNotContain(result.Materials,x=>x.MaterialId==materials.dtf.Id);}
    [Fact] public async Task Fabric_is_not_costed_when_not_selected(){await using var db=CreateDb();var(q,materials)=await SeedBase(db,true);q.Items.Single().FabricRequired=false;var result=await QuoteCalculationSupport.Calculate(db,q,default);Assert.DoesNotContain(result.Materials,x=>x.MaterialId==materials.fabric.Id);}
    [Fact] public async Task Missing_exchange_rate_prevents_false_zero_cost(){await using var db=CreateDb();var(q,_)=await SeedBase(db,true,"USD");var result=await QuoteCalculationSupport.Calculate(db,q,default);Assert.Null(result.EstimatedCost);Assert.Contains(result.Warnings,x=>x.Contains("USD/TRY kuru eksik"));}
    [Fact] public async Task Lead_time_uses_active_mold_capacity(){await using var db=CreateDb();var(q,_)=await SeedBase(db,true);var one=await QuoteCalculationSupport.Calculate(db,q,default);db.Molds.Add(new Mold{ProductId=q.Items.Single().ProductId,Code="M2",Name="M2",Size="42",CavityCount=1,StandardCycleTimeSeconds=60,IsActive=true});await db.SaveChangesAsync();var two=await QuoteCalculationSupport.Calculate(db,q,default);Assert.True(two.LeadTimeDays<=one.LeadTimeDays);Assert.NotEmpty(two.CapacityAssumptions);}
    [Fact] public void Sent_quote_can_be_approved(){Assert.True(QuoteWorkflowRules.CanApproveOrReject("Sent"));}
    [Theory] [InlineData("Sent")] [InlineData("Approved")] [InlineData("Rejected")] [InlineData("Cancelled")] [InlineData("Converted")] public void Non_draft_quote_cannot_be_edited(string status){Assert.False(QuoteWorkflowRules.CanEdit(status));}
    [Fact] public void Draft_quote_can_be_edited(){Assert.True(QuoteWorkflowRules.CanEdit("Draft"));}
    [Fact] public void Approved_quote_can_convert(){Assert.True(QuoteWorkflowRules.CanConvert("Approved",null));}
    [Fact] public void Converted_quote_cannot_convert_twice(){Assert.False(QuoteWorkflowRules.CanConvert("Converted",Guid.NewGuid()));}
    [Fact] public void Rejected_quote_cannot_convert(){Assert.False(QuoteWorkflowRules.CanConvert("Rejected",null));}
    [Fact] public void Operator_role_is_not_part_of_quote_policy_contract(){Assert.DoesNotContain("Operator",new[]{"CEO","Sales Manager","Production Manager"});}
    [Fact] public async Task Quote_change_creates_audit_log(){await using var db=CreateDb();var customer=new Customer{Name="Test",CustomerCode="C1",IsActive=true};db.Customers.Add(customer);db.Quotes.Add(new Quote{QuoteNumber="TKL-2026-000099",Customer=customer,ValidUntil=DateTime.UtcNow.AddDays(5)});await db.SaveChangesAsync();Assert.True(await db.AuditLogs.AnyAsync(x=>x.EntityName==nameof(Quote)));}
    [Fact] public void Converted_order_index_is_unique(){using var db=CreateDb();var entity=db.Model.FindEntityType(typeof(Quote))!;var index=entity.GetIndexes().Single(x=>x.Properties.Any(p=>p.Name==nameof(Quote.ConvertedOrderId)));Assert.True(index.IsUnique);}
    [Fact] public void Quote_item_sync_updates_adds_and_removes_by_item_id()
    {
        var product1=Guid.NewGuid();var product2=Guid.NewGuid();var removed=new QuoteItem{Id=Guid.NewGuid(),ProductId=product1,LineNumber=1,Quantity=1,UnitPrice=10};var updated=new QuoteItem{Id=Guid.NewGuid(),ProductId=product2,LineNumber=2,Quantity=2,UnitPrice=20};var quote=new Quote{Items=[removed,updated]};
        var result=QuoteItemSynchronizer.Synchronize(quote,[new QuoteItemRequest(updated.Id,product2,"42","Siyah",5,25,false,false,null,"Güncellendi"),new QuoteItemRequest(null,product1,"43","Beyaz",3,15,true,false,null,null)]);
        Assert.Null(result.Error);Assert.Single(result.Removed);Assert.Equal(removed.Id,result.Removed[0].Id);Assert.Equal(2,quote.Items.Count);Assert.Equal(5,quote.Items.Single(x=>x.Id==updated.Id).Quantity);Assert.Contains(quote.Items,x=>x.Id!=updated.Id&&x.ProductId==product1);Assert.Equal([1,2],quote.Items.OrderBy(x=>x.LineNumber).Select(x=>x.LineNumber));
    }
    [Fact] public void Quote_item_sync_supports_repeated_product_with_distinct_item_ids()
    {
        var productId=Guid.NewGuid();var first=new QuoteItem{Id=Guid.NewGuid(),ProductId=productId,LineNumber=1};var second=new QuoteItem{Id=Guid.NewGuid(),ProductId=productId,LineNumber=2};var quote=new Quote{Items=[first,second]};
        var result=QuoteItemSynchronizer.Synchronize(quote,[new QuoteItemRequest(first.Id,productId,"41",null,1,10,false,false,null,null),new QuoteItemRequest(second.Id,productId,"42",null,2,20,false,false,null,null)]);
        Assert.Null(result.Error);Assert.Empty(result.Removed);Assert.Equal(2,quote.Items.Count);Assert.Equal(1,first.Quantity);Assert.Equal(2,second.Quantity);
    }
    [Fact] public void Quote_item_sync_rejects_unknown_or_duplicate_item_ids()
    {
        var item=new QuoteItem{Id=Guid.NewGuid(),ProductId=Guid.NewGuid(),LineNumber=1};var quote=new Quote{Items=[item]};var request=new QuoteItemRequest(item.Id,item.ProductId,null,null,1,1,false,false,null,null);
        Assert.NotNull(QuoteItemSynchronizer.Synchronize(quote,[request,request]).Error);
        Assert.NotNull(QuoteItemSynchronizer.Synchronize(quote,[request with{Id=Guid.NewGuid()}]).Error);
    }

    private static ApplicationDbContext CreateDb(){var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new ApplicationDbContext(options,new AuditableEntitySaveChangesInterceptor(new TestUser(),new TestClock()));}
    private static async Task<(Quote quote,(Material chemical,Material dtf,Material fabric) materials)> SeedBase(ApplicationDbContext db,bool recipe,string stockCurrency="TRY")
    {
        var customer=new Customer{Name="Test",CustomerCode="C1",IsActive=true};var product=new Product{Code="P1",Name="Ürün",IsActive=true,StandardCycleTime=60};var chemical=new Material{Code="POL",Name="Poliol",MaterialType="Kimyasal",Unit="kg",IsActive=true};var dtf=new Material{Code="DTF",Name="DTF Etiket",MaterialType="DTF",Unit="adet",IsActive=true};var fabric=new Material{Code="KMS",Name="İnterlok Kumaş",MaterialType="Kumaş",Unit="m",IsActive=true};db.AddRange(customer,product,chemical,dtf,fabric);db.StockItems.AddRange(new StockItem{Material=chemical,Name="Poliol",Code="POL",Unit="kg",CurrentQuantity=100,LastPurchasePrice=2,Currency=stockCurrency,IsActive=true},new StockItem{Material=dtf,Name="DTF",Code="DTF",Unit="adet",CurrentQuantity=100,LastPurchasePrice=1,Currency=stockCurrency,IsActive=true},new StockItem{Material=fabric,Name="Kumaş",Code="KMS",Unit="m",CurrentQuantity=100,LastPurchasePrice=1,Currency=stockCurrency,IsActive=true});
        if(recipe)db.Recipes.Add(new Recipe{Code="R1",Name="R1",Product=product,IsActive=true,IsDefault=true,OutputQuantity=1,Items={new RecipeItem{Material=chemical,Quantity=1,Unit="kg"},new RecipeItem{Material=dtf,Quantity=1,Unit="adet"},new RecipeItem{Material=fabric,Quantity=1,Unit="m"}}});db.Molds.Add(new Mold{Product=product,Code="M1",Name="M1",Size="42",CavityCount=1,StandardCycleTimeSeconds=60,IsActive=true});db.InjectionStations.Add(new InjectionStation{StationNumber=1,Name="İstasyon 1",Status="Aktif",IsActive=true});db.CostSettings.Add(new CostSettings{Name="Test",EffectiveFrom=DateTime.UtcNow.Date.AddDays(-1),ReportingCurrency="TRY",IsActive=true});var quote=new Quote{QuoteNumber="TKL-2026-000001",Customer=customer,QuoteDate=DateTime.UtcNow,ValidUntil=DateTime.UtcNow.AddDays(30),Currency="TRY",Items={new QuoteItem{Product=product,LineNumber=1,Size="42",Quantity=100,UnitPrice=10,FabricRequired=true,DtfRequired=true}}};db.Quotes.Add(quote);await db.SaveChangesAsync();return(quote,(chemical,dtf,fabric));
    }
    private sealed class TestClock:IDateTimeService{public DateTime UtcNow=>DateTime.UtcNow;}
    private sealed class TestUser:ICurrentUserService{public Guid? UserId=>Guid.Parse("11111111-1111-1111-1111-111111111111");public string? Email=>"test@fixar.local";public string? UserName=>"Test";public string? IpAddress=>"127.0.0.1";public bool IsAuthenticated=>true;public IReadOnlyList<string> Roles=>["CEO"];}
}
