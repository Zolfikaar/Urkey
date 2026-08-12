using System;
using System.IO;
using System.Text.Json;
using Urkey.Core.Models;
using Urkey.Core.Paths;

namespace Urkey.Core.Repository
{
  public class UserRepository
  {
    private readonly string _userDirectory;
    private readonly string _userFilePath;

    public UserRepository(string? customUserDirectory = null)
    {
      if (string.IsNullOrWhiteSpace(customUserDirectory))
      {
        AppDataPaths.EnsureInitialized();
        _userDirectory = AppDataPaths.RootDirectory;
        _userFilePath = AppDataPaths.UserFilePath;
      }
      else
      {
        _userDirectory = customUserDirectory;
        Directory.CreateDirectory(_userDirectory);
        _userFilePath = Path.Combine(_userDirectory, AppDataPaths.UserFileName);
      }
    }

    public bool UserExists()
        => File.Exists(_userFilePath);

    public User Load()
    {
      if (!UserExists())
        return new User();

      try
      {
        var json = File.ReadAllText(_userFilePath);
        return JsonSerializer.Deserialize<User>(json) ?? new User();
      }
      catch
      {
        return new User();
      }
    }

    public void Save(User user)
    {
      var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(_userFilePath, json);
    }

    public string GetUserDirectory() => _userDirectory;
    public string GetUserPath() => _userFilePath;
  }
}

