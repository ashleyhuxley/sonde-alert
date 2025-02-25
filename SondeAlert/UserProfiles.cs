using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ElectricFox.SondeAlert
{
    public class UserProfiles
    {
        private readonly ILogger<UserProfiles> _logger;

        private readonly List<UserProfile> _profiles = [];

        private readonly SondeAlertOptions _options;

        private bool _isLoaded = false;

        private readonly object _lock = new();

        public UserProfiles(ILogger<UserProfiles> logger, IOptions<SondeAlertOptions> options)
        {
            this._logger = logger;
            this._options = options.Value;
        }

        public void AddUserProfile(UserProfile profile)
        {
            this._profiles.Add(profile);
            this.SaveUserProfiles();
        }

        public void RemoveUserProfile(long chatId)
        {
            var profile = this._profiles.SingleOrDefault(p => p.ChatId == chatId);
            if (profile is not null)
            {
                this._profiles.Remove(profile);
                this.SaveUserProfiles();
            }
        }

        public IEnumerable<UserProfile> GetAllProfiles()
        {
            return this._profiles;
        }

        public bool HasProfile(long chatId)
        {
            return this._profiles.Any(p => p.ChatId == chatId);
        }

        public void LoadUserProfiles()
        {
            lock (_lock)
            {
                if (!_isLoaded)
                {
                    if (
                        this._options.ProfilePath is null || !File.Exists(this._options.ProfilePath)
                    )
                    {
                        this._logger.LogWarning("Profiles file does not exist, skipping load.");
                        return;
                    }

                    this._logger.LogInformation(
                        "Loading profiles from {path}",
                        this._options.ProfilePath
                    );

                    try
                    {
                        var profilesJson = File.ReadAllText(this._options.ProfilePath);
                        var profiles = JsonSerializer.Deserialize<IEnumerable<UserProfile>>(
                            profilesJson
                        ) ?? throw new JsonException("Profiles deserialized to null");
                        this._profiles.Clear();
                        this._profiles.AddRange(profiles);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogCritical(
                            ex,
                            "Exception saving profiles: {message}",
                            ex.Message
                        );
                        return;
                    }

                    this._logger.LogInformation(
                        "{profilesCount} profiles loaded.",
                        _profiles.Count
                    );

                    _isLoaded = true;
                }
            }
        }

        private void SaveUserProfiles()
        {
            if (this._options.ProfilePath is null)
            {
                return;
            }

            this._logger.LogInformation(
                "Saving {profilesCount} user profiles to {path}",
                _profiles.Count,
                _options.ProfilePath
            );

            try
            {
                var json = JsonSerializer.Serialize(this._profiles);
                File.WriteAllText(this._options.ProfilePath, json);
            }
            catch (Exception ex)
            {
                this._logger.LogCritical(ex, "Exception saving profiles: {message}", ex.Message);
            }
        }
    }
}
