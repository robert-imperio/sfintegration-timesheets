using Salesforce.Common;
using Salesforce.Force;
using System.Net;
using System.Threading.Tasks;

namespace sfintegration.salesforce.api.client
{
    public enum ClientType
    {
        Test,
        Prod
    }

    public static class SalesForceClientFactory
    {
        private static ConfigHelper configHelper = new ConfigHelper();

        public static async Task<IForceClient> GetClient(ClientType clientType)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Need to set this for SalesForce API 

            switch (clientType)
            {
                case ClientType.Test:
                    return await GetTestClient();
                case ClientType.Prod:
                    return await GetProdClient();
                default:
                    return await GetProdClient();
            }
        }

        private static async Task<IForceClient> GetTestClient()
        {
            var auth = new AuthenticationClient();
            var userName = configHelper.GetClientSetting("TestUserName");
            var passWord = configHelper.GetClientSetting("TestPassword");
            var consumerKey = configHelper.GetClientSetting("TestConsumerKey");
            var consumerSecret = configHelper.GetClientSetting("TestConsumerSecret");
            var loginUrl = configHelper.GetClientSetting("TestLoginUrl");

            await auth.UsernamePasswordAsync(consumerKey, consumerSecret, userName, passWord, loginUrl);

            return new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);
        }

        private static async Task<IForceClient> GetProdClient()
        {
            var auth = new AuthenticationClient();
            var userName = configHelper.GetClientSetting("ProdUserName");
            var passWord = configHelper.GetClientSetting("ProdPassword");
            var consumerKey = configHelper.GetClientSetting("ProdConsumerKey");
            var consumerSecret = configHelper.GetClientSetting("ProdConsumerSecret");
            var loginUrl = configHelper.GetClientSetting("ProdLoginUrl");

            await auth.UsernamePasswordAsync(consumerKey, consumerSecret, userName, passWord, loginUrl);

            return new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);
        }
    }
}
