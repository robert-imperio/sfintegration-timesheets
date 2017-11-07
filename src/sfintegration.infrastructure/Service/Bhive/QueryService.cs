using System;
using System.Collections.Generic;
using System.Linq;
using bhive.entities;
using sfintegration.infrastructure.Interface.Bhive;
using System.Data.Entity;


namespace sfintegration.infrastructure.Service.Bhive
{
    public class QueryService : IQueryService
    {
        public IEnumerable<UserTimeClock> GetUserTimeClocks(DateTime startDate, DateTime endDate, DateTime endTime)
        {
            const string _offShiftId = "a0h8000000DyYFwAAN";
            const string _preShiftId = "preshift";

            using (var context =  new BhiveContext())
            {
                var userTimeClocks = context.UserTimeClocks.Include(m => m.JobOrder)
                    .Where
                    (m =>
                    m.UserId != ""
                    && m.StartTime >= startDate 
                    && m.StartTime < endDate 
                    && m.EndTime != null
                    && m.ActivityId != _preShiftId
                    && m.ActivityId != _offShiftId
                    && m.JobOrderId != null
                    && m.EndTime >= endTime
                    && m.UserId == "00380000026X7mRAAS"
                    );

                // Filter out any activities not lasting longer than a minute.
                return userTimeClocks.ToList().Where(m => m.EndTime.Value.Subtract(m.StartTime).TotalMinutes >= 1);
            }
        }
    }
}
