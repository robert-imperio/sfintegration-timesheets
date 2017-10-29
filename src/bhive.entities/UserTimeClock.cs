using System;

namespace bhive.entities
{
    public class UserTimeClock
    {
        public string UserId { get; set; }
        public string ActivityId { get; set; }
        public string JobOrderId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public virtual JobOrder JobOrder { get; set; }
    }
}
