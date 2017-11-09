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
        public IEnumerable<UserTimeClock> GetUserTimeClocks(DateTime lastEndTime)
        {
            const string _offShiftId = "a0h8000000DyYFwAAN";
            const string _preShiftId = "preshift";

            using (var context = new BhiveContext())
            {
                var userTimeClocks = context.UserTimeClocks
                    .Include(m => m.JobOrder)
                    .Where
                    (m =>
                        m.UserId != ""
                        && m.EndTime != null
                        && m.ActivityId != _preShiftId
                        && m.ActivityId != _offShiftId
                        && m.JobOrderId != null
                        && m.EndTime >= lastEndTime
                    )
                    .OrderBy(m => new {m.UserId, m.StartTime});

                var projects = context.Projects.ToList();

                foreach (var utc in userTimeClocks)
                {
                    utc.TimeZoneId = projects.FirstOrDefault(m => m.Id == utc.JobOrder.ProjectId)?.TimeZoneId;
                }

                return userTimeClocks.ToList();
            }

        }
    }
}
