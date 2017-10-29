using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfintegration.infrastructure.Extension
{
    public static class DateTimeExtension
    {
        /// <summary>
        /// Get work week start date for current work week. 
        /// Work weeks for Broad-Path employees start on Monday of each week.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime StartOfWorkWeek(this DateTime date)
        {
            int diff = date.DayOfWeek - DayOfWeek.Monday;

            if (diff < 0)
            {
                diff += 7;
            }

            var rtnDate = date.AddDays(-1 * diff).Date;

            return DateTime.Parse(rtnDate.ToString("yyy-MM-dd"));
        }

        public static DateTime EndOfWorkWeek(this DateTime date)
        {
            int diff = date.DayOfWeek - DayOfWeek.Monday;

            if (diff < 0)
            {
                diff += 7;
            }

            var rtnDate = date.AddDays(-1 * diff).Date.AddDays(6);

            return DateTime.Parse(rtnDate.ToString("yyy-MM-dd"));
        }

        public static object ToDbNull(this DateTime? val)
        {
            if (val == null) { return DBNull.Value; }

            var buf = new DateTime();
            if (!DateTime.TryParse(val.ToString(), out buf)) { return DBNull.Value; }
            if (buf < DateTime.Parse("1990-09-09")) { return DBNull.Value; }

            return val;
        }

        public static object ToDbNull(this DateTime val)
        {
            if (val == null) { return DBNull.Value; }

            var buf = new DateTime();
            if (!DateTime.TryParse(val.ToString(), out buf)) { return DateTime.Parse("9999-09-09"); }
            if (buf < DateTime.Parse("1990-09-09")) { return DateTime.Parse("1990-09-09"); }

            return val;
        }
    }
}
