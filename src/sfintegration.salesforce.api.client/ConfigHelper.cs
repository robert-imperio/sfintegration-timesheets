using System.Configuration;

namespace sfintegration.salesforce.api.client
{
    public class ConfigHelper
    {
        private readonly Configuration config;

        public ConfigHelper()
        {
            var assemblyPath = this.GetType().Assembly.Location;
            config = ConfigurationManager.OpenExeConfiguration(assemblyPath);
        }

        public string GetClientSetting(string key)
        {
            return config.AppSettings.Settings[key].Value;
        }
    }
}
