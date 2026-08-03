namespace Urkey.Core.Models
{
    public class User
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Verifier hash only (never the vault encryption key). Base64.
        /// </summary>
        public string HashPassword { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// 0 = legacy (PBKDF2 100k, hash == AES key, AES-CBC).
        /// 1+ = current (PBKDF2 600k+, HKDF split, AES-GCM).
        /// </summary>
        public int CryptoVersion { get; set; }

        public int KdfIterations { get; set; }

        public enum SubscriptionType
        {
            Free,
            Guest,
            Paid,
        }

        public SubscriptionType Subscription = SubscriptionType.Guest;
        public DateOnly FirstOpenDate { get; set; }
        public DateOnly? StartSubscriptionDate { get; set; }
        public DateOnly? EndSubscriptionDate { get; set; }
    }
}
