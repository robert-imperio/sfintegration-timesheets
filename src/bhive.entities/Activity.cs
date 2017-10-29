namespace bhive.entities
{
    public class Activity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PayClass { get; set; }
        public int? DisplayIndex { get; set; }
        public string ActivityImage { get; set; }
        public bool? DisplayCamera { get; set; }
        public bool? DisplayTimer { get; set; }
        public bool? DisplayElapsedTime { get; set; }
        public bool? Active { get; set; }
        public string Color { get; set; }
        public bool? ActiveFilter { get; set; }
    }
}
