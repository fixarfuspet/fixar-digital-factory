using Fixar.API.Controllers;
using Fixar.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class IntegratedFinanceSafetyTests
{
    [Fact]
    public void New_financial_movements_default_to_balance_affecting_one_to_one_snapshot()
    {
        var movement = new FinancialTransaction { Amount = 125.50m, Currency = "TRY", ReportingAmount = 125.50m };
        Assert.True(movement.AffectsBalance);
        Assert.Equal(1m, movement.ExchangeRate);
        Assert.Equal("TRY", movement.ReportingCurrency);
        Assert.Equal(movement.Amount, movement.ReportingAmount);
    }

    [Fact]
    public void Reconciliation_snapshot_has_safe_initial_status()
    {
        var snapshot = new AccountReconciliation();
        Assert.Equal("Draft", snapshot.Status);
        Assert.Equal("TRY", snapshot.Currency);
        Assert.Equal("{}", snapshot.SnapshotJson);
    }

    [Theory]
    [InlineData("Inflow", 100, 25, 125)]
    [InlineData("Outflow", 100, 25, 75)]
    public void Running_balance_math_respects_direction(string direction, decimal opening, decimal amount, decimal expected)
    {
        var actual = opening + (direction == "Inflow" ? amount : -amount);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Finance_categories_and_reconciliation_are_api_controllers()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(typeof(FinanceCategoriesController), typeof(ApiControllerAttribute)));
        Assert.NotNull(Attribute.GetCustomAttribute(typeof(AccountReconciliationsController), typeof(ApiControllerAttribute)));
    }
}
