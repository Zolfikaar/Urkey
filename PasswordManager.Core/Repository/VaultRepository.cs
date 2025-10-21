using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using PasswordManager.Core.Models;


namespace PasswordManager.Core.Repository
{
    public class VaultRepository
    {
        //// a path for password encrypted file
        private readonly string _filePath = "";
        private bool _isMasterPasswordSet = false;
        private readonly string _masterPassword = "";

        // Constructor
        public VaultRepository(string filePath, string masterPassword)
        {
            if (_isMasterPasswordSet == true && masterPassword == _masterPassword)
            {

            }
            else
            {

            }
            _filePath = filePath;
            _masterPassword = masterPassword;
        }

        //public string MasterPassword()
        //{
        //    return _masterPassword;
        //}


        public Vault LoadVault()
        {
            // if there is no password file exists 
            if (!File.Exists(_filePath))
                // create new empty one
                return new Vault();

            string json = File.ReadAllText(_filePath);
            var vault = JsonSerializer.Deserialize<Vault>(json) ?? new Vault();

            // TODO: Add encryption/decryption when EncryptionHelper is implemented
            // foreach (var cred in vault.Credentials)
            // {
            //     cred.Password = EncryptionHelper.Decrypt(cred.Password, _masterPassword);
            // }

            return vault;
        }

        public void SaveVault(Vault vault)
        {
            // TODO: Add encryption when EncryptionHelper is implemented
            // foreach (var cred in vault.Credentials)
            // {
            //     cred.Password = EncryptionHelper.Encrypt(cred.Password, _masterPassword);
            // }

            string json = JsonSerializer.Serialize(vault, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);

            // TODO: Decrypt passwords back for in-memory use when EncryptionHelper is implemented
            // foreach (var cred in vault.Credentials)
            // {
            //     cred.Password = EncryptionHelper.Decrypt(cred.Password, _masterPassword);
            // }
        }

        public void AddCredential(Credential credential)
        {
            var vault = LoadVault();
            vault.Credentials.Add(credential);
            SaveVault(vault);
        }

        ////public void SetMasterPassword(string masterPassword)
        ////{
        ////    if (_isMasterPasswordSet)
        ////        throw new InvalidOperationException("Master password is already set.");
        ////    // Here you might want to add logic to validate the master password strength
        ////    // For simplicity, we just set it directly
        ////    _masterPassword = masterPassword;
        ////    _isMasterPasswordSet = true;
        ////}

        //public void Unlock_Vault(string masterPassword)
        //{
        //}
    }
}
