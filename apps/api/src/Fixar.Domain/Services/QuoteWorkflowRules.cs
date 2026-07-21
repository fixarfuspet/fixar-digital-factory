namespace Fixar.Domain.Services;

public static class QuoteWorkflowRules
{
    public static bool CanEdit(string status) => status == "Draft";
    public static bool CanSend(string status) => status == "Draft";
    public static bool CanApproveOrReject(string status) => status == "Sent";
    public static bool CanCancel(string status) => status is "Draft" or "Sent";
    public static bool CanConvert(string status, Guid? convertedOrderId) => status == "Approved" && !convertedOrderId.HasValue;
}
