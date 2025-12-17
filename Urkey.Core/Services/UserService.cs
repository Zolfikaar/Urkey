using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Urkey.Core.Models;

namespace Urkey.Core.Services
{
    public class UserService
    {
        //===== Properties =====//
        private User _userModel;
        public enum FirstSetupError
        {
            None,
            EmptyMasterPassword,
            InvalidMasterPassword,
            InvalidEmail,
            InvalidPhone
        }
        public class FirstSetupResult
        {
            public bool Success { get; set; }
            public FirstSetupError Error { get; set; }
        }

        //===== Constructor =====//
        public UserService()
        {
            _userModel = new User();
        }

        //===== Methods - public =====//
        public FirstSetupResult FirstSetup(string? email, string? phone, string enteredMasterPassword)
        {
            if (string.IsNullOrWhiteSpace(enteredMasterPassword))
                return new FirstSetupResult
                {
                    Success = false,
                    Error = FirstSetupError.EmptyMasterPassword
                };

            if (enteredMasterPassword.Length < 4)
                return new FirstSetupResult
                {
                    Success = false,
                    Error = FirstSetupError.InvalidMasterPassword
                };

            if (!string.IsNullOrEmpty(email) && !CheckValidEmail(email))
                return new FirstSetupResult
                {
                    Success = false,
                    Error = FirstSetupError.InvalidEmail
                };
            
            if (!string.IsNullOrEmpty(phone) && !CheckValidPhone(phone))
                return new FirstSetupResult
                {
                    Success = false,
                    Error = FirstSetupError.InvalidPhone
                };
            
            
            // إنشاء المستخدم
            string salt = GenerateSalt();
            _userModel.Email = email ?? "";
            _userModel.Phone = phone ?? "";
            _userModel.Salt = salt;
            _userModel.HashPassword =
                EncryptionService.HashPassword(enteredMasterPassword, salt);
            _userModel.FirstOpenDate = DateOnly.FromDateTime(DateTime.Now);

            return new FirstSetupResult
            {
                Success = true,
                Error = FirstSetupError.None
            };
        }

        public bool NormalSetup(string enteredMasterPassword)
        {
            // check the user model salt & hash, if not exists in user model(user.json file), return false
            if (string.IsNullOrEmpty(_userModel.Salt) ||
                string.IsNullOrEmpty(_userModel.HashPassword))
                return false;

            // else(they are exists), then
            // Generate hash for the enteredMasterPassword
            var enteredHash = EncryptionService.HashPassword(
                enteredMasterPassword,
                _userModel.Salt);
            
            // if the two hashes didn't match, return false
            if (!SlowEquals(enteredHash, _userModel.HashPassword))
                return false;

            // else(the hashes (passwords), match), then
            // initialize encryption service to generate key, and return true
            EncryptionService.Initialize(enteredMasterPassword, _userModel.Salt);
            return true;
        }

        //===== Methods - private =====//
        private string GenerateSalt()
        {
            var byteSalt = new byte[128 / 8];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(byteSalt);
             
            return Convert.ToBase64String(byteSalt);
            
        }

        private bool CheckValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckValidPhone(string phone)
        {

            var regex = new System.Text.RegularExpressions.Regex(
                @"^(077|078)\d{8}$"
            );

            return regex.IsMatch(phone);
        }
        
        private bool SlowEquals(string a, string b)
        {
            var diff = a.Length ^ b.Length;
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
        
    }
}
