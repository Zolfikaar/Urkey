using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urkey.Core.Models;

namespace Urkey.Core.Managers
{

    public static class VaultManager
    {
        public static string GetVaultPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Urkey",
                "vault.json"
            );
        }

        public static bool VaultExists()
        {
            return File.Exists(GetVaultPath());
        }

        
    }

}
