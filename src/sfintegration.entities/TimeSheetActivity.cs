using System;

namespace sfintegration.entities
{
    public class TimeSheetActivity
    {
        public string Id { get; set; }

        public string TimeSheetId { get; set; }

        public string UserId { get; set; }

        public string ProjectId { get; set; }

        public string JobOrderId { get; set; }

        public string ActivityId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime LastUpdated { get; set; }

        public string UpdatedBy { get; set; }
    }
}
