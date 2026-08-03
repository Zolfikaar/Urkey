namespace Urkey.Core.Models
{
    public class ActivityLogEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>added | edited | deleted | uploaded</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>account | card | address | note | document</summary>
        public string EntryType { get; set; } = string.Empty;

        public string EntryName { get; set; } = string.Empty;
        public Guid? EntryId { get; set; }
    }
}
