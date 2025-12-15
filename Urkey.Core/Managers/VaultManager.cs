using System;
using System.IO;
using Urkey.Core.Repository;

namespace Urkey.Core.Managers
{

    public static class VaultManager
    {
        public static string GetVaultPath() => VaultRepository.GetVaultPath();

        public static bool VaultExists() => VaultRepository.VaultExists();
        

        public static string GetVaultDirectory() => VaultRepository.GetVaultDirectory();




    }

}
