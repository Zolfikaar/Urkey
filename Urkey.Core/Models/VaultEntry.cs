using System;
using System.Formats.Tar;
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
    }
}
