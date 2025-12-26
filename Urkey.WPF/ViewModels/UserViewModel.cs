using System;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
namespace Urkey.WPF.ViewModels;

    public class UserViewModel
    {
        private UserService _userService;
        public UserViewModel()
        {
            _userService = new UserService();
            
            // UserService constructor automatically loads user data from user.json via UserRepository
            // No need to manually load from AppSettings anymore
        }

    public bool Unlock(string enteredMasterPassword)
    {
        return _userService.NormalSetup(enteredMasterPassword);
    }
    
}