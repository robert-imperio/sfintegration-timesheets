using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Salesforce.Force;

namespace sfintegration.salesforce.api.client
{
    public class SalesForceQuery<T, U> : ISalesForceQuery<T, U>
        where T : class
        where U : class
    {
        private readonly IForceClient _forceClient;
        private readonly IMapper _mapper;
        private const int MaxReadLimit = 200;

        public SalesForceQuery(IForceClient forceClient, IMapper mapper)
        {
            _forceClient = forceClient;
            _mapper = mapper;
        }

        /// <summary>
        /// SalesForce has a 2000 record max (could be less depending on number of fields requested), so
        /// need to read in batches of MaxReadLimit.
        /// </summary>
        /// <param name="soql"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetRecords(Func<string, int, string> soql)
        {
            try
            {
                var keepReading = true;
                var lastId = "";
                var buf = new List<T>();

                while (keepReading)
                {
                    var qry = soql(lastId, MaxReadLimit);
                    var response = await _forceClient.QueryAsync<U>(soql(lastId, MaxReadLimit));

                    buf.AddRange(_mapper.Map<IEnumerable<T>>(response.Records));
                    var listCount = response.Records.Count;
                    keepReading = false;
                    if (listCount != 0)
                    {
                        var obj = response.Records[listCount - 1];
                        lastId = ((U)obj).GetType().GetProperty("Id")?.GetValue(obj, null).ToString();
                        if (listCount >= MaxReadLimit) { keepReading = true; }
                    }
                }

                return buf;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public async Task<IEnumerable<U>> GetRecords(Func<string, string> soql)
        {
            var keepReading = true;
            var lastId = "";
            var buf = new List<U>();

            while (keepReading)
            {
                var qry = soql(lastId);
                var response = await _forceClient.QueryAsync<U>(soql(lastId));

                buf.AddRange(response.Records);
                var listCount = response.Records.Count;
                keepReading = false;
                if (listCount != 0)
                {
                    var lastRecord = response.Records[listCount - 1];
                    lastId = ((U)lastRecord).GetType().GetProperty("Id")?.GetValue(lastRecord, null).ToString();
                    keepReading = true; 
                }
            }

            return buf;
        }
    }
}
