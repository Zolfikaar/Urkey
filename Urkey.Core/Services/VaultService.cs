using Urkey.Core.Repository;
using Urkey.Core.Models;
namespace Urkey.Core.Services;

public class VaultService
{
    private static Vault _vault;
    private static VaultEntry _vaultEntry;
    
    public static Vault LoadVault() => VaultRepository.LoadVault();
    
    public static Vault ReloadFromDisk() => VaultRepository.ReloadFromDisk();
    
    public static void AddEntry() => VaultRepository.AddEntry(_vaultEntry);
    
    public static void SaveVault() => VaultRepository.SaveVault(_vault);
    
    public static string GetVaultPath() => VaultRepository.GetVaultPath();

    public static bool VaultExists() => VaultRepository.VaultExists();
    
    public static string GetVaultDirectory() => VaultRepository.GetVaultDirectory();

    public static void CreateVault() => new VaultRepository();
}