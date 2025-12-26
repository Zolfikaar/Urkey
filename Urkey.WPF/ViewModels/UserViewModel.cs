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
        
        // Load user data from settings
        if (App.Settings != null && !string.IsNullOrEmpty(App.Settings.Salt) && !string.IsNullOrEmpty(App.Settings.HashPassword))
        {
            _userService.LoadUser(App.Settings.Salt, App.Settings.HashPassword);
        }
    }

    public bool Unlock(string enteredMasterPassword)
    {
        return _userService.NormalSetup(enteredMasterPassword);
    }
    
}