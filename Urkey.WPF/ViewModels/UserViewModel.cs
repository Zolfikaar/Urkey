using Urkey.Core.Services;

namespace Urkey.WPF.ViewModels;

public class UserViewModel
{
    private readonly UserService _userService;

    public UserViewModel()
    {
        _userService = new UserService();
    }

    public UserService.UnlockResult Unlock(string enteredMasterPassword)
        => _userService.Unlock(enteredMasterPassword);
}
