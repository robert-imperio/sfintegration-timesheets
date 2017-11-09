using sfintegration.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using NLog;

namespace sfintegration.infrastructure.Service.IntegrationDB
{    
    public class CommandService
    {
        private readonly Logger _logger = LogManager.GetLogger("AzureLogger");

        public async Task UpsertTimeSheets(IEnumerable<TimeSheet> timeSheets)
        {
            using (var context = new SFIntegrationContext())
            {
                await context.BulkMergeAsync(timeSheets);
            }
        }

        public async Task UpsertTimeSheetActivities(IEnumerable<TimeSheetActivity> timeSheetActivities)
        {
            using (var context = new SFIntegrationContext())
            {
                await context.BulkMergeAsync(timeSheetActivities);
            }
        }

        public async Task UpsertUserTimeClocks(IEnumerable<UserTimeClock> userTimeClocks)
        {
            try
            {
                using (var context = new SFIntegrationContext())
                {
                    await context.BulkMergeAsync(userTimeClocks, options =>
                        options.ColumnInputExpression = entity => new
                        {
                            entity.UserId,
                            entity.ProjectId,
                            entity.JobOrderId,
                            entity.ActivityId,
                            entity.StartDate,
                            entity.StartTime,
                            entity.EndTime
                        }
                    );
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                _logger.Error(e, "Error in UpsertUserTimeClocks");
            }
        }

        public void UpdateUserTimeClocksFromTimeSheets()
        {
            using (var context = new SFIntegrationContext())
            {
                var sql = $@"
                    update UserTimeClocks
                    set TimeSheetId = ts.Id, [Status] = ts.Status
                    from
	                    UserTimeClocks utc
	                    join TimeSheets ts on utc.UserId = ts.UserId and utc.StartDate = ts.StartDate
                    where
                        utc.TimeSheetId is null
                    ".Trim();

                context.Database.ExecuteNonQuery(sql);
            }
        }

        public void UpateUserTimeClocksFromTimeSheetActivities(DateTime startDate)
        {
            using (var context = new SFIntegrationContext())
            {
                var localTimeZone = TimeZone.CurrentTimeZone;
                var localDateTime = DateTime.Now;
                var utcOffset = localTimeZone.GetUtcOffset(localDateTime).TotalHours;
                var sql = $@"
                    update UserTimeClocks
                    set 
                        TimeSheetActivityId = tsa.Id, 
                        Status = (select top 1 Status from TimeSheets ts where ts.UserId = utc.UserId and ts.StartDate = utc.StartDate),
                        LastUpdated = tsa.LastUpdated,
                        UpdatedBy = tsa.UpdatedBy
                    from
	                    UserTimeClocks utc
	                    join TimeSheetActivities tsa on utc.UserId = tsa.UserId 
	                    and tsa.StartDate = utc.StartDate
                    where
	                    utc.StartDate = '{startDate.ToString("yyyy-MM-dd")}'
                        and year(utc.StartTime) = year(tsa.StartTime)
                        and month(utc.StartTime) = month(tsa.StartTime)
                        and day(utc.StartTime) = day(tsa.StartTime)
                        and datepart(hour, DateAdd(hour, {utcOffset}, utc.StartTime)) = datepart(hour, tsa.StartTime)
                        and datepart(minute, utc.StartTime) = datepart(minute, tsa.StartTime)
                        ".Trim();

                context.Database.ExecuteNonQuery(sql);
            }            
        }

        public void FlagConflicts(DateTime startDate)
        {
            using (var context = new SFIntegrationContext())
            {
                var localTimeZone = TimeZone.CurrentTimeZone;
                var localDateTime = DateTime.Now;
                var utcOffset = localTimeZone.GetUtcOffset(localDateTime).TotalHours;
                var sql = $@"
                    update UserTimeClocks	
                    set HasConflict = 1
                    from
	                    UserTimeClocks utc
	                    join TimeSheetActivities tsa 
                    on
	                    tsa.UserId = utc.UserId
	                    and utc.StartDate = '{startDate.ToString("yyyy-MM-dd")}'
	                    and isnull(utc.TimeSheetActivityId, '') = ''
	                    and (
	                    (tsa.StartTime >= DateAdd(hour, {utcOffset}, utc.StartTime) and tsa.StartTime <= DateAdd(hour, {utcOffset}, utc.EndTime))
	                    or
	                    (tsa.StartTime <= DateAdd(hour, {utcOffset}, utc.StartTime) and tsa.EndTime >= DateAdd(hour, {utcOffset}, utc.StartTime))
	                    )
                    ".Trim();

                context.Database.ExecuteNonQuery(sql);
            }
        }

        public void UpdateUserTimeClockSubmittedDate(IEnumerable<entities.UserTimeClock> userTimeClocks)
        {
            using (var context = new SFIntegrationContext())
            {
                var submittedDate = DateTime.UtcNow;
                
                foreach(var userTimeClock in userTimeClocks)
                {
                    userTimeClock.SubmittedDate = submittedDate;
                }

                context.BulkMerge(userTimeClocks, options =>
                    options.ColumnInputExpression = entity => new {
                        entity.UserId,
                        entity.JobOrderId,
                        entity.ActivityId,
                        entity.StartTime,
                        entity.SubmittedDate
                    }
                );
            }
        }

        public IEnumerable<entities.UserTimeClock> SaveUserTimeClocks(IEnumerable<entities.UserTimeClock> userTimeClocks)
        {
            using (var context = new SFIntegrationContext())
            {
                var submittedDate = DateTime.UtcNow;

                foreach (var userTimeClock in userTimeClocks)
                {
                    userTimeClock.SubmittedDate = submittedDate;
                }

                context.BulkMerge(userTimeClocks, options =>
                    options.ColumnInputExpression = entity => new {
                        entity.UserId,
                        entity.JobOrderId,
                        entity.ActivityId,
                        entity.StartTime,
                        entity.TimeSheetActivityId,
                        entity.SubmittedDate,
                        entity.JobId,
                        entity.BatchId
                    }
                );
            }

            return userTimeClocks;
        }
    }
}
