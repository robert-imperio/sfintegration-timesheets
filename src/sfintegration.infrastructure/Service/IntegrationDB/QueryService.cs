using sfintegration.infrastructure.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}
