using System.Text.Json.Serialization;

namespace Urkey.Core.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(AccountEntry), typeDiscriminator: "account")]
    [JsonDerivedType(typeof(AddressEntry), typeDiscriminator: "address")]
    [JsonDerivedType(typeof(CardEntry), typeDiscriminator: "card")]
    [JsonDerivedType(typeof(DocumentEntry), typeDiscriminator: "document")]
    [JsonDerivedType(typeof(NoteEntry), typeDiscriminator: "note")]
    public abstract class VaultEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time the entry was opened or used. Falls back to <see cref="UpdatedAt"/> when unset.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }
    }
}
