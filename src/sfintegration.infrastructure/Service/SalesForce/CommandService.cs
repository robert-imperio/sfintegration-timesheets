using NLog;
using Salesforce.Common.Models.Xml;
using Salesforce.Force;
using sfintegration.entities;
using sfintegration.infrastructure.Helper;
using sfintegration.infrastructure.Service.IntegrationDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfintegration.infrastructure.Service.SalesForce
{
    public class CommandService
    {
        private readonly IForceClient _forceClient;
        private readonly Logger _logger = LogManager.GetLogger("AzureLogger");

        public CommandService(IForceClient forceClient)
        {
            _forceClient = forceClient;
        }

        public async Task<IEnumerable<BatchInfoResult>> SubmitTimeSheets(IEnumerable<entities.TimeSheet> timeSheets)
        {
            const int batchSize = 100;
            var jobInfo = await _forceClient.CreateJobAsync("BP_Weekly_Timesheet__c", BulkConstants.OperationType.Insert);            
            var tsBatchList = Batch_TimeSheets(timeSheets, batchSize);
            var batchInfoResultList = new List<BatchInfoResult>();

            foreach (var tsBatch in tsBatchList)
            {
                var batch = new SObjectList<SObject>();
                foreach (var ts in tsBatch)
                {
                    var rec = new SObject {
                        { "Name", $"Week of {ts.StartDate.ToString("yyyy-MM-dd")}" },
                        { "Submitted_By__c", $"{ts.UserId}" },
                        { "start_date__c", $"{ts.StartDate.ToString("yyyy-MM-dd")}" }
                    };
                    batch.Add(rec);
                }
                batchInfoResultList.Add(await _forceClient.CreateJobBatchAsync(jobInfo, batch));
            }

            var jonInfoResult = await _forceClient.CloseJobAsync(jobInfo); // Closing job prevents any more batches from being added.

            return batchInfoResultList;
        }

        public async Task<IEnumerable<entities.UserTimeClock>> SubmitUserTimeClocks(IEnumerable<entities.UserTimeClock> userTimeClocks)
        {
            try
            {
                var jobInfo = await _forceClient.CreateJobAsync("BP_TimeSheet_Activity__c", BulkConstants.OperationType.Insert);

                userTimeClocks = await BatchAndSubmit_UserTimeClocks(jobInfo, userTimeClocks);

                return userTimeClocks;
            }
            catch(Exception e)
            {
                _logger.Error(e, "Error while SubmittingUserTimeClocks.");
                return userTimeClocks;
            }
        }        

        private IEnumerable<IEnumerable<TimeSheet>> Batch_TimeSheets(IEnumerable<TimeSheet> timeSheets, int batchSize)
        {
            var batchList = new List<IEnumerable<TimeSheet>>();
            var curCount = 0;
            var addedToList = false;
            List<TimeSheet> batch = null;

            foreach (var ts in timeSheets)
            {
                batch = (curCount == 0) ? new List<TimeSheet>() : batch;
                batch.Add(ts);
                addedToList = false;
                curCount++;

                if (curCount >= batchSize)
                {
                    batchList.Add(batch);
                    curCount = 0;
                    addedToList = true;
                }
            }

            if (!addedToList) { batchList.Add(batch); }

            return batchList;
        }

        private async Task<IEnumerable<entities.UserTimeClock>> BatchAndSubmit_UserTimeClocks(Salesforce.Common.Models.Xml.JobInfoResult jobInfo, IEnumerable<UserTimeClock> userTimeClocks)
        {
            const int batchSize = 100;
            var utcBatchList = Batch_UserTimeClocks(userTimeClocks, batchSize);
            var batchInfoResultList = new List<BatchInfoResult>();
            var startDate = userTimeClocks.FirstOrDefault().StartDate;

            foreach (var utcBatch in utcBatchList)
            {
                // map bhive usertimeclock records to sales force time sheet activity records
                var tsaBatch = new SObjectList<SObject>();
                foreach (var utc in utcBatch)
                {                    
                    tsaBatch.Add(MapToSalesForceTimeSheetActivityRecord(utc));
                }

                // submit batch here
                var batchInfoResult = await _forceClient.CreateJobBatchAsync(jobInfo, tsaBatch);
                batchInfoResultList.Add(batchInfoResult);

                // Set SalesForce job id and batch id in user time clock record
                foreach (var utc in utcBatch)
                {
                    utc.JobId = jobInfo.Id;
                    utc.BatchId = batchInfoResult.Id;
                }

                SaveSubmittedBatchToBlobStorage(startDate, batchInfoResult.Id, tsaBatch);
            }

            // Closing job prevents any more batches from being added and
            // allows faster reading of submission results.
            await _forceClient.CloseJobAsync(jobInfo);

            var batchResultsList = await Get_BatchSubmissionResults(batchInfoResultList);
            userTimeClocks = setTimeSheetActivityIdsFromSalesForce(batchResultsList, userTimeClocks);

            return userTimeClocks;
        }

        private IEnumerable<UserTimeClock> setTimeSheetActivityIdsFromSalesForce(List<BatchResultList> batchResultsList, IEnumerable<UserTimeClock> userTimeClocks)
        {
            var batchResults = batchResultsList.SelectMany(m => m.Items).ToArray();
            var idx = 0;

            foreach(var utc in userTimeClocks)
            {
                utc.TimeSheetActivityId = batchResults[idx].Id;
                idx++;
            }

            return userTimeClocks;
        }

        private IEnumerable<IEnumerable<UserTimeClock>> Batch_UserTimeClocks(IEnumerable<UserTimeClock> userTimeClocks, int batchSize)
        {
            var batchList = new List<IEnumerable<UserTimeClock>>();
            var curCount = 0;
            var addedToList = false;
            List<UserTimeClock> batch = null;

            foreach (var utc in userTimeClocks)
            {
                batch = (curCount == 0) ? new List<UserTimeClock>() : batch;
                batch.Add(utc);
                addedToList = false;
                curCount++;

                if (curCount >= batchSize)
                {
                    batchList.Add(batch);
                    curCount = 0;
                    addedToList = true;
                }
            }

            if (!addedToList) { batchList.Add(batch); }

            return batchList;
        }

        private SObject MapToSalesForceTimeSheetActivityRecord(UserTimeClock utc)
        {
            var rec = new SObject {
                        { "Timesheet_Reference__c", $"{utc.TimeSheetId}" },
                        { "BP_Project__c", $"{utc.ProjectId}"},
                        { "Job_Order__c", $"{utc.JobOrderId}" },
                        { "BP_Activity__c", $"{utc.ActivityId}"},
                        { "Start_Time__c", $"{utc.StartTime.ToString("yyyy-MM-ddTHH:mm:ss")}" },
                        { "End_Time__c", $"{utc.EndTime.ToString("yyyy-MM-ddTHH:mm:ss")}" }
                    };
            return rec;
        }

        private async Task<List<BatchResultList>> Get_BatchSubmissionResults(List<BatchInfoResult> batchInfoList)
        {
            try
            {
                const float pollIncrease = 2.0f;
                var pollStart = 1.0f;
                var completedList = new List<BatchInfoResult>();

                while (batchInfoList.Count > 0)
                {
                    var removeList = new List<BatchInfoResult>();

                    foreach (var batchInfo in batchInfoList)
                    {
                        var newBatchInfo = await _forceClient.PollBatchAsync(batchInfo);
                        if (newBatchInfo.State.Equals(BulkConstants.BatchState.Completed.Value()) ||
                            newBatchInfo.State.Equals(BulkConstants.BatchState.Failed.Value()) ||
                            newBatchInfo.State.Equals(BulkConstants.BatchState.NotProcessed.Value()))
                        {
                            completedList.Add(newBatchInfo);
                            removeList.Add(batchInfo);
                        }
                    }

                    foreach (var removeInfo in removeList)
                    {
                        batchInfoList.Remove(removeInfo);
                    }

                    await Task.Delay((int)pollStart);
                    pollStart *= pollIncrease;
                }

                // get results
                var results = new List<BatchResultList>();
                foreach (var completedBatch in completedList)
                {
                    results.Add(await _forceClient.GetBatchResultAsync(completedBatch));
                }

                return results;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception while getting batch submssion results from SalesForce.");
                throw;
            }
        }

        private void SaveSubmittedBatchToBlobStorage(DateTime startDate, string batchId, SObjectList<SObject> batch)
        {
            const string containerName = "salesforcetimesheets";

            var jsonBuilder = new JsonBuilder<SObject>();
            var json = jsonBuilder.JsonifyWithLineFeed(batch);
            var gzippedStream = CompressionHelper.CompressToStream(Encoding.UTF8.GetBytes(json));
            var key = $"{startDate.ToString("yyyy-MM-dd")}/batch-{batchId}.gzip";

            BlobStorageHelper.Instance.UploadGZipStream(containerName, key, gzippedStream);            
        }
        
    }
}
