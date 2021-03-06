﻿using System;
using System.Linq;
using sfintegration.infrastructure.Extension;
using sfintegration.salesforce.api.client;
using sfintegration.infrastructure.Mapping;
using System.Collections.Generic;
using bhive.entities;
using NLog;

namespace sfintegration.timesheets.consoleapp
{
    class Program
    {
        private readonly static Logger _logger = LogManager.GetLogger("AzureLogger");

        private readonly static sfintegration.infrastructure.Service.SalesForce.QueryService _sfQuery;
        private readonly static sfintegration.infrastructure.Service.SalesForce.CommandService _sfCommand;

        private readonly static sfintegration.infrastructure.Service.IntegrationDB.CommandService _igCommand;
        private readonly static sfintegration.infrastructure.Service.IntegrationDB.QueryService _igQuery;

        private readonly static sfintegration.infrastructure.Service.Bhive.QueryService _bhQuery;

        static Program()
        {
            var forceClient = SalesForceClientFactory.GetClient(ClientType.Prod).GetAwaiter().GetResult();

            _sfQuery = new infrastructure.Service.SalesForce.QueryService(forceClient);
            _sfCommand = new infrastructure.Service.SalesForce.CommandService(forceClient);
            _igCommand = new infrastructure.Service.IntegrationDB.CommandService();
            _igQuery = new infrastructure.Service.IntegrationDB.QueryService();
            _bhQuery = new infrastructure.Service.Bhive.QueryService();
        }

        static void Main(string[] args)
        {                        
            var startTime = DateTime.UtcNow;
            Console.WriteLine("Started");
            Console.WriteLine(startTime);

            var startDate = DateTime.UtcNow.StartOfWorkWeek();
            var lastModifiedDate = DateTime.UtcNow.AddDays(-2);

            LogManager.Configuration.Variables["StartDate"] = startDate.ToString("yyyy-MM-dd");
            _logger.Info("Started UserTimeClock submissions to SalesForce.");

            // Get Last staged end time and go back 1 hour just in case
            var lastEndTime = _igQuery.GetLastStagedUserTimeClockEndTime().AddHours(-1);

            Console.WriteLine("Reading UserTimeClocks");
            _logger.Info($"Regtrieving UserTimeClocks from Bhive for work week starting {startDate}");
            var userTimeClocks = _bhQuery.GetUserTimeClocks(lastEndTime);
            if (userTimeClocks != null && userTimeClocks.Count() > 0)
            {
                _logger.Info($"Staging {userTimeClocks.Count()} UserTimeClock records.");
                Console.WriteLine($"Writing {userTimeClocks.Count()} UserTimeClocks");
                userTimeClocks = FilterOutShortActivities(userTimeClocks);
                WriteUserTimeClocksToIntegrationDB(userTimeClocks);

                Console.WriteLine($"Reading SalesForce TimeSheets");
                var salesForceTimeSheets = _sfQuery.GetTimeSheets(startDate, lastModifiedDate).GetAwaiter().GetResult();
                WriteSalesForceTimeSheetsToIntegrationDB(salesForceTimeSheets);

                Console.WriteLine($"Reading SalesForce TimeSheetActivities");
                var salesForceTimeSheetActivities = _sfQuery.GetTimeSheetActivities(startDate, lastModifiedDate).GetAwaiter().GetResult();
                WriteSalesForceTimeSheetActivitiesToIntegrationDB(salesForceTimeSheetActivities);

                Console.WriteLine($"Setting TimeSheet Ids");
                // This sets the TimeSheetId and Status from SalesForce
                _igCommand.UpdateUserTimeClocksFromTimeSheets();

                Console.WriteLine($"Setting TimeSheetActivity Ids");
                // This sets the TimeSheetActivityId 
                _igCommand.UpateUserTimeClocksFromTimeSheetActivities(startDate);

                Console.WriteLine($"Setting Conflict flags");
                // Flag over-lapping activities between SalesForce and Bhive as conflicts
                _igCommand.FlagConflicts(startDate);

                Console.WriteLine($"Getting TimeSheets to submit");
                // Create SalesForce TimeSheet records for UserTimeClocks with no TimeSheetIds
                // then read from SalesForce and write ids back to integration db
                var missingTimeSheetIds = _igQuery.GetMissingTimeSheetId(startDate);
                if (missingTimeSheetIds != null && missingTimeSheetIds.Count() > 0)
                {
                    Console.WriteLine($"Submitting TimeSheets");
                    // Submit here
                    _sfCommand.SubmitTimeSheets(missingTimeSheetIds).GetAwaiter().GetResult();
                    _logger.Info($"Submitted {missingTimeSheetIds.Count()} TimeSheet records to SalesForce");

                    Console.WriteLine($"Reading back SalesForce TimeSheets and updating local table");
                    // Read back here
                    salesForceTimeSheets = _sfQuery.GetTimeSheets(startDate, lastModifiedDate).GetAwaiter().GetResult();
                    WriteSalesForceTimeSheetsToIntegrationDB(salesForceTimeSheets);
                    _igCommand.UpdateUserTimeClocksFromTimeSheets();
                }
              
                Console.WriteLine($"Getting UserTimeClocks to submit");
                // Submit usertimeclocks to SalesForce
                var userTimeClocksToSubmit = _igQuery.GetUserTimeClocksToSubmit(startDate);
                if (userTimeClocksToSubmit != null && userTimeClocksToSubmit.Count() > 0)
                {
                    _logger.Info($"Started submitting {userTimeClocksToSubmit.Count()} UserTimeClocks");
                    Console.WriteLine($"Submitting UserTimeClocks");
                    // Submit here
                    var submittedUserTimeClocks = _sfCommand.SubmitUserTimeClocks(userTimeClocksToSubmit).GetAwaiter().GetResult();
                    _igCommand.SaveUserTimeClocks(submittedUserTimeClocks); 
                    
                    var processed = submittedUserTimeClocks.Count(m => !string.IsNullOrEmpty(m.TimeSheetActivityId));
                    var failed = submittedUserTimeClocks.Count(m => string.IsNullOrEmpty(m.TimeSheetActivityId));
                    _logger.Info($"Submitted {submittedUserTimeClocks.Count()} UserTimeClock records");
                    _logger.Info($"UserTimeClocks processed: {processed}");
                    _logger.Info($"UserTimeClocks failed: {failed}");
                }

            }

            var endTime = DateTime.UtcNow;
            Console.WriteLine("Finis");
            Console.WriteLine(endTime);
            Console.WriteLine("Total Runtime: " + (endTime - startTime).ToString());
            //Console.ReadLine();

            _logger.Info($"Total processing time: {(endTime - startTime)}");
        }

        private static void WriteSalesForceTimeSheetsToIntegrationDB(IEnumerable<salesforce.api.client.SFEnterprise.BP_Weekly_Timesheet__c> sfTimeSheets)
        {
            if (sfTimeSheets == null || sfTimeSheets.Count() == 0) { return;  }

            var config = SalesForceToIntegrationMapConfigProvider.Configuration();
            var mapper = config.CreateMapper();            
            var igTimeSheets = mapper.Map<IEnumerable<sfintegration.entities.TimeSheet>>(sfTimeSheets);

            _igCommand.UpsertTimeSheets(igTimeSheets).GetAwaiter().GetResult();
        }

        private static void WriteSalesForceTimeSheetActivitiesToIntegrationDB(IEnumerable<salesforce.api.client.SFEnterprise.BP_Timesheet_Activity__c> sfTimeSheetActivities)
        {
            if (sfTimeSheetActivities == null || sfTimeSheetActivities.Count() == 0) { return; }

            var config = SalesForceToIntegrationMapConfigProvider.Configuration();
            var mapper = config.CreateMapper();
            var igTimeSheetActivities = mapper.Map<IEnumerable<sfintegration.entities.TimeSheetActivity>>(sfTimeSheetActivities);

            _igCommand.UpsertTimeSheetActivities(igTimeSheetActivities).GetAwaiter().GetResult();
        }

        private static void WriteUserTimeClocksToIntegrationDB(IEnumerable<bhive.entities.UserTimeClock> userTimeClocks)
        {
            var config = BhiveToIntegrationMapConfigProvider.UserTimeClockConfiguration();
            var mapper = config.CreateMapper();
            var igUserTimeClocks = mapper.Map<IEnumerable<sfintegration.entities.UserTimeClock>>(userTimeClocks);

            _igCommand.UpsertUserTimeClocks(igUserTimeClocks).GetAwaiter().GetResult();
        }

        private static IEnumerable<UserTimeClock> FilterOutShortActivities(IEnumerable<UserTimeClock> userTimeClocks)
        {
            // Filter out any activities not lasting longer than a minute.
            // SQL couldn't translate Subtract, so doing in C#.
            return userTimeClocks.Where(m => { return m.EndTime != null && m.EndTime.Value.Subtract(m.StartTime).TotalMinutes >= 1; });
        }

    }
}
