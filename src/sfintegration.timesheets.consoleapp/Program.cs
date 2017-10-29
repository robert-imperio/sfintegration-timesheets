using System;
using System.Linq;
using sfintegration.infrastructure.Extension;
using sfintegration.salesforce.api.client;
using sfintegration.infrastructure.Mapping;
using System.Collections.Generic;
using System.Diagnostics;
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
            LogManager.Configuration.Variables["StartDate"] = startDate.ToString("yyyy-MM-dd");
            _logger.Info("Started UserTimeClock submissions to SalesForce.");
            
            var startTime = DateTime.Now;
            Console.WriteLine("Started");
            Console.WriteLine(startTime);
            
            var startDate = DateTime.Now.StartOfWorkWeek();
            var endDate = DateTime.Now.EndOfWorkWeek();
            var lastModifiedDate = DateTime.UtcNow.AddDays(-2);

            LogManager.Configuration.Variables["StartDate"] = startDate.ToString("yyyy-MM-dd");
            _logger.Info("Started UserTimeClock submissions to SalesForce.");

            Debug.WriteLine("Reading UserTimeClocks");
            _logger.Info($"Regtrieving UserTimeClocks from Bhive for work week starting {startDate}");
            var userTimeClocks = _bhQuery.GetUserTimeClocks(startDate, endDate);
            if (userTimeClocks != null && userTimeClocks.Count() > 0)
            {
                _logger.Info($"Staging {userTimeClocks.Count()} UserTimeClock records.");
                Debug.WriteLine($"Writing {userTimeClocks.Count()} UserTimeClocks");
                WriteUserTimeClocksToIntegrationDB(startDate, userTimeClocks);

                Debug.WriteLine($"Reading SalesForce TimeSheets");
                var salesForceTimeSheets = _sfQuery.GetTimeSheets(startDate, lastModifiedDate).GetAwaiter().GetResult();
                WriteSalesForceTimeSheetsToIntegrationDB(salesForceTimeSheets);

                Debug.WriteLine($"Reading SalesForce TimeSheetActivities");
                var salesForceTimeSheetActivities = _sfQuery.GetTimeSheetActivities(startDate, lastModifiedDate).GetAwaiter().GetResult();
                WriteSalesForceTimeSheetActivitiesToIntegrationDB(salesForceTimeSheetActivities);

                Debug.WriteLine($"Setting TimeSheet Ids");
                // This sets the TimeSheetId and Status from SalesForce
                _igCommand.UpdateUserTimeClocksFromTimeSheets(startDate);

                Debug.WriteLine($"Setting TimeSheetActivity Ids");
                // This sets the TimeSheetActivityId 
                _igCommand.UpateUserTimeClocksFromTimeSheetActivities(startDate);

                Debug.WriteLine($"Setting Conflict flags");
                // Flag over-lapping activities between SalesForce and Bhive as conflicts
                _igCommand.FlagConflicts(startDate);

                Debug.WriteLine($"Getting TimeSheets to submit");
                // Create SalesForce TimeSheet records for UserTimeClocks with no TimeSheetIds
                // then read from SalesForce and write ids back to integration db
                var missingTimeSheetIds = _igQuery.GetMissingTimeSheetId(startDate);
                if (missingTimeSheetIds != null && missingTimeSheetIds.Count() > 0)
                {
                    Debug.WriteLine($"Submitting TimeSheets");
                    // Submit here
                    _sfCommand.SubmitTimeSheets(missingTimeSheetIds).GetAwaiter().GetResult();
                    _logger.Info($"Submitted {missingTimeSheetIds.Count()} TimeSheet records to SalesForce");

                    Debug.WriteLine($"Reading back SalesForce TimeSheets and updating local table");
                    // Read back here
                    salesForceTimeSheets = _sfQuery.GetTimeSheets(startDate, lastModifiedDate).GetAwaiter().GetResult();
                    WriteSalesForceTimeSheetsToIntegrationDB(salesForceTimeSheets);
                    _igCommand.UpdateUserTimeClocksFromTimeSheets(startDate);
                }

                Debug.WriteLine($"Getting UserTimeClocksk to submit");
                // Submit usertimeclocks to SalesForce
                var userTimeClocksToSubmit = _igQuery.GetUserTimeClocksToSubmit(startDate);
                if (userTimeClocksToSubmit != null && userTimeClocksToSubmit.Count() > 0)
                {
                    _logger.Info($"Started UserTimeClock submissions");
                    //Debug.WriteLine($"Submitting UserTimeClocks");
                    //// Submit here
                    //_sfCommand.SubmitUserTimeClocks(userTimeClocksToSubmit).GetAwaiter().GetResult();
                    //_igCommand.UpdateUserTimeClockSubmittedDate(userTimeClocksToSubmit);
                    _logger.Info($"Submitted {userTimeClocksToSubmit.Count()} UserTimeClock records");

                    //Debug.WriteLine($"Reading back SalesForce TimeSheetActivities and updating local table");
                    //// Read back here
                    //salesForceTimeSheetActivities = _sfQuery.GetTimeSheetActivities(startDate, lastModifiedDate).GetAwaiter().GetResult();
                    //WriteSalesForceTimeSheetActivitiesToIntegrationDB(salesForceTimeSheetActivities);
                    //_igCommand.UpateUserTimeClocksFromTimeSheetActivities(startDate);
                }

            }

            var endTime = DateTime.Now;
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

        private static void WriteUserTimeClocksToIntegrationDB(DateTime startDate, IEnumerable<bhive.entities.UserTimeClock> userTimeClocks)
        {
            var config = BhiveToIntegrationMapConfigProvider.UserTimeClockConfiguration(startDate);
            var mapper = config.CreateMapper();
            var igUserTimeClocks = mapper.Map<IEnumerable<sfintegration.entities.UserTimeClock>>(userTimeClocks);

            _igCommand.UpsertUserTimeClocks(igUserTimeClocks).GetAwaiter().GetResult();
        }

    }
}
