using NLog;
using Salesforce.Force;
using sfintegration.salesforce.api.client;
using sfintegration.salesforce.api.client.SFEnterprise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfintegration.infrastructure.Service.SalesForce
{
    public class QueryService
    {
        private readonly IForceClient _forceClient;
        private readonly Logger _logger = LogManager.GetLogger("AzureLogger");

        public QueryService(IForceClient forceClient)
        {
            _forceClient = forceClient;
        }

        public async Task<IEnumerable<BP_Weekly_Timesheet__c>> GetTimeSheets(DateTime startDate, DateTime lastModifiedDate)
        {
            const int batchReadLimit = 1000;
            var qry = new SalesForceQuery<BP_Weekly_Timesheet__c, BP_Weekly_Timesheet__c>(_forceClient, null);

            Func<string, string> soql = lastId => $@"
                select 
                    Id, 
                    Name, 
                    Contact_Last_First_Name__c, 
                    Submitted_By__c, 
                    start_date__c, 
                    status__c,
                    LastModifiedDate,
                    LastModifiedBy.FirstName,
                    LastModifiedBy.LastName
                from 
                    BP_Weekly_Timesheet__c 
                where 
                    start_date__c = {startDate.ToString("yyyy-MM-dd")} 
                    and Id > '{lastId}' 
                    and LastModifiedDate >= {lastModifiedDate.ToString("yyyy-MM-dd") + "T00:00:00.000+0000"}
                order by 
                    Id
                limit {batchReadLimit}
                ".Trim();

            return await qry.GetRecords(soql);
        }

        public async Task<IEnumerable<BP_Timesheet_Activity__c>> GetTimeSheetActivities(DateTime startDate, DateTime lastModifiedDate)
        {
            try
            {
                const int batchReadLimit = 500;
                var qry = new SalesForceQuery<BP_Timesheet_Activity__c, BP_Timesheet_Activity__c>(_forceClient, null);

                Func<string, string> soql = lastId => $@"
                    select 
                        Timesheet_Reference__r.Submitted_By__c, Id, 
                        TimeSheet_Reference__c, 
                        BP_Project__c, 
                        Job_Order__c, 
                        BP_Activity__c, 
                        Timesheet_Reference__r.Start_Date__c,
                        Start_Time__c, 
                        End_Time__c,
                        LastModifiedDate,
                        LastModifiedById,
                        LastModifiedBy.FirstName,
                        LastModifiedBy.LastName
                    from 
                        BP_Timesheet_Activity__c 
                    where 
                        Timesheet_Reference__r.Start_Date__c = {startDate.ToString("yyyy-MM-dd")} 
                        and LastModifiedDate >= {lastModifiedDate.ToString("yyyy-MM-dd") + "T00:00:00.000+0000"}
                        and Id > '{lastId}' 
                    order by Id 
                        limit {batchReadLimit}
                    ".Trim();

                return await qry.GetRecords(soql);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return null;
            }
        }
    }
}
