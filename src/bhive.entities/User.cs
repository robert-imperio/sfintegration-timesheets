namespace bhive.entities
{
    public class User
    {
        public string Id { get; set; }
        public string RoleId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PersonalEmail { get; set; }
        public string BPEmail { get; set; }
        public string HomeCity { get; set; }
        public string HomeState { get; set; }
        public string SecurityStamp { get; set; }
        public string MachineName { get; set; }
        public bool? Visible { get; set; }
        public string AccountId { get; set; }
        public string UserType { get; set; }
        public string ConnectionId { get; set; }
        public string Classification { get; set; }
        public bool? Terminated { get; set; }
        public int? AssignmentsQty { get; set; }
        public decimal? PerformanceAvg { get; set; }
        public string TimeZone { get; set; }
        public int? TimeZoneOffset { get; set; }
        public bool? DayLightSavings { get; set; }
        public string Gender { get; set; }
        public string PayClass { get; set; }
        public int? YearStarted { get; set; }
    }
}
