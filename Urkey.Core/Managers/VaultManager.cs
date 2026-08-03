using Urkey.Core.Services;

namespace Urkey.Core.Managers
{
    /// <summary>
    /// Process-wide vault unlock flag. Locking always clears the encryption key.
    /// </summary>
    public static class VaultManager
    {
        public static bool IsUnlocked { get; private set; }

        public static bool Unlock()
        {
            IsUnlocked = true;
            return true;
        }

        public static void Lock()
        {
            IsUnlocked = false;
            EncryptionService.Clear();
        }
    }
}
