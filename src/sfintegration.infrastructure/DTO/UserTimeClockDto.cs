using System;

namespace sfintegration.infrastructure.DTO
{
    public class UserTimeClockDto
    {
        public string UserId { get; set; }
        public string ActivityId { get; set; }
        public string ProjectId { get; set; }
        public string JobOrderId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string TimeZoneId { get; set; }
    }
}
