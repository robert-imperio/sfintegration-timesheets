using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sfintegration.salesforce.api.client
{
    public interface ISalesForceQuery<T, U>
        where T : class
        where U : class
    {
        Task<IEnumerable<T>> GetRecords(Func<string, int, string> soql);

        Task<IEnumerable<U>> GetRecords(Func<string, string> soql);
    }
}
