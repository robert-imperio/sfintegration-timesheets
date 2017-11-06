using bhive.entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sfintegration.infrastructure.Interface.Bhive
{
    public interface IQueryService
    {
        IEnumerable<UserTimeClock> GetUserTimeClocks(DateTime startDate, DateTime endDate, DateTime startTime);
    }
}
