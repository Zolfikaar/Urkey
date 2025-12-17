using System;
using System.IO;

namespace Urkey.Core.Managers
{

    public class VaultManager
    {

        public bool IsUnlocked { get; private set; }

        public bool Unlock()
        {
            IsUnlocked = true;
            return true;
        }

        public void Lock()
        {
            IsUnlocked = false;
        }


    }

}
