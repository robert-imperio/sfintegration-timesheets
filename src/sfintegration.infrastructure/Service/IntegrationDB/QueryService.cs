using sfintegration.infrastructure.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using sfintegration.infrastructure.Extension;

namespace sfintegration.infrastructure.Service.IntegrationDB
{
    public class QueryService
    {
        public IEnumerable<sfintegration.entities.TimeSheet> GetMissingTimeSheetId(DateTime startDate)
        {
            using (var context = new SFIntegrationContext())
            {
                var userTimeClocks = context.UserTimeClocks.Where(m => m.StartDate == startDate && m.TimeSheetId == null).ToList();

                return userTimeClocks
                    .Select(m => new entities.TimeSheet { UserId = m.UserId, StartDate = m.StartDate })
                    .Distinct(new TimeSheetEqualityComparer());
            }
        }

        public IEnumerable<sfintegration.entities.UserTimeClock> GetUserTimeClocksToSubmit(DateTime startDate)
        {
            using(var context = new SFIntegrationContext())
            {
                return context.UserTimeClocks
                    .Where(m =>
                    m.StartDate == startDate
                    && (m.TimeSheetId != null || m.TimeSheetId != string.Empty)
                    && m.TimeSheetActivityId == null
                    && (m.HasConflict == null || m.HasConflict == false)
                    && m.Status.ToLower() == "created - in progress"
                    ).ToList();
            }
        }

        public DateTime GetLastStagedUserTimeClockEndTime()
        {
            using(var context = new SFIntegrationContext())
            {
                if (context.UserTimeClocks != null)
                {
                    return context.UserTimeClocks
                        .Where(m => m.EndTime != null)
                        .OrderByDescending(m => m.StartTime)
                        .FirstOrDefault().EndTime;
                }

                // If no records staged, default to current start of work week.
                return DateTime.UtcNow.StartOfWorkWeek();
            }
        }
    }
}
