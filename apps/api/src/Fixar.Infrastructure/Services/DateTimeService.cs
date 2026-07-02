using Fixar.Application.Common.Interfaces;

namespace Fixar.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
