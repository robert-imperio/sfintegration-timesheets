using System;

namespace sfintegration.entities
{
    public class TimeSheet
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string UserId { get; set; }

        public DateTime StartDate { get; set; }

        public string Status { get; set; }

        public DateTime LastUpdated { get; set; }

        public string UpdatedBy { get; set; }
    }
}
