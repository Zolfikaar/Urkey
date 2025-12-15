using System;
using Urkey.Core.Services;
namespace Urkey.WPF.ViewModels;

public class UserViewModel
{
    private UserService _userService;
    public UserViewModel(string enteredMasterPassword)
    {
        _userService = new UserService(enteredMasterPassword);
    }
    
    
}