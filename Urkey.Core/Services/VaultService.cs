using Urkey.Core.Repository;
using Urkey.Core.Models;
namespace Urkey.Core.Services;

public class VaultService
{
    private readonly VaultRepository _repo;
    private Vault? _vault;

    public VaultService()
    {
        _repo = new VaultRepository();
        _vault = new Vault();
    }

    public bool VaultExists() => _repo.VaultExists();

    public Vault Load()
    {
        _vault = _repo.Load();
        return _vault;
    }

    public void AddEntry(VaultEntry entry)
    {
        if (_vault == null)
            throw new InvalidOperationException("Vault not loaded");

        _vault.Entries.Add(entry);
        _repo.Save(_vault);
    }

    public void Save()
    {
        if (_vault == null)
            throw new InvalidOperationException("Vault not loaded");

        _repo.Save(_vault);
    }

    public string GetVaultPath() => _repo.GetVaultPath();

    public string GetVaultDirectory() => _repo.GetVaultDirectory();

    public Vault LoadVault() => _vault = _repo.Load(); 
}
