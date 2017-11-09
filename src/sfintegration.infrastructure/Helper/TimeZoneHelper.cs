using System;
using System.Collections.Generic;

namespace sfintegration.infrastructure.Helper
{
    public sealed class TimeZoneHelper
    {
        private static TimeZoneHelper _instance;
        private static IDictionary<string, string> _timeZones; 

        public static TimeZoneHelper Instance => _instance ?? (_instance = new TimeZoneHelper());

        public DateTime ConvertFromUTC(DateTime utcDateTime, string abbreviatedTimeZoneId)
        {
            var tzId = _timeZones[abbreviatedTimeZoneId];
            var targetTz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, targetTz);
        }

        private TimeZoneHelper()
        {
            InitTimeZones();            
        }

        private static void  InitTimeZones()
        {
            _timeZones = new Dictionary<string, string>();

            _timeZones.Add("EST", "Eastern Standard Time");
            _timeZones.Add("CST", "Central Standard Time");
            _timeZones.Add("MST", "Mountain Standard Time");
            _timeZones.Add("AZ", "US Mountain Standard Time");
            _timeZones.Add("PST", "Pacific Standard Time");
            _timeZones.Add("AKST", "Alaskan Standard Time");
            _timeZones.Add("HST", "Hawaiian Standard Time");            
        }
    }
}
