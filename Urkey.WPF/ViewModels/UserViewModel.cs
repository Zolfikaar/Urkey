using System;
using Urkey.Core.Services;
namespace Urkey.WPF.ViewModels;

public class UserViewModel
{
    private UserService _userService;
    public UserViewModel()
    {
        _userService = new UserService();
    }

    public bool Unlock(string enteredMasterPassword)
    {
        return _userService.NormalSetup(enteredMasterPassword);
    }
    
}