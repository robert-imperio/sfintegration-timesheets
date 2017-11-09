using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace bhive.entities
{
    public class Project
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Name { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DisplayMode { get; set; }
        public string TimeZoneId { get; set; }
        public bool? DST { get; set; }
        public bool ScreenCaptureBlocked { get; set; }
        public bool BlockTimeClockNotifications { get; set; }
    }
}
