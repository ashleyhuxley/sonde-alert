using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ElectricFox.SondeAlert
{
    public class UserProfiles
    {
        private readonly ILogger<UserProfiles> _logger;

        private readonly ConcurrentDictionary<long, UserProfile> _profiles = [];

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
            this._profiles.TryAdd(profile.ChatId, profile);
            this.SaveUserProfiles();
        }

        public void RemoveUserProfile(long chatId)
        {
            _profiles.TryRemove(chatId, out _);
            this.SaveUserProfiles();
        }

        public bool HasProfile(long chatId)
        {
            return this._profiles.ContainsKey(chatId);
        }

        public UserProfile? GetProfileByCallsign(string callsign)
        {
            return _profiles.Values.FirstOrDefault(p => p.Callsign == callsign);
        }

        public string[] GetAllCallsigns()
        {
            return _profiles
                .Values
                .Select(p => p.Callsign ?? string.Empty)
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();
        }

        public void LoadUserProfiles()
        {
            lock (_lock)
            {
                if (!_isLoaded)
                {
                    if (
                        _options.ProfilePath is null || !File.Exists(_options.ProfilePath)
                    )
                    {
                        _logger.LogWarning("Profiles file does not exist, skipping load.");
                        return;
                    }

                    _logger.LogInformation(
                        "Loading profiles from {path}",
                        _options.ProfilePath
                    );

                    try
                    {
                        var profilesJson = File.ReadAllText(_options.ProfilePath);
                        var profiles = JsonSerializer.Deserialize<IEnumerable<UserProfile>>(
                            profilesJson
                        ) ?? throw new JsonException("Profiles deserialized to null");
                        
                        _profiles.Clear();
                        foreach (var profile in profiles)
                        {
                            _profiles.TryAdd(profile.ChatId, profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(
                            ex,
                            "Exception saving profiles: {message}",
                            ex.Message
                        );
                        return;
                    }

                    _logger.LogInformation(
                        "{profilesCount} profiles loaded.",
                        _profiles.Count
                    );

                    _isLoaded = true;
                }
            }
        }

        private void SaveUserProfiles()
        {
            if (_options.ProfilePath is null)
            {
                return;
            }

            _logger.LogInformation(
                "Saving {profilesCount} user profiles to {path}",
                _profiles.Count,
                _options.ProfilePath
            );

            try
            {
                var json = JsonSerializer.Serialize(_profiles);
                File.WriteAllText(_options.ProfilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Exception saving profiles: {message}", ex.Message);
            }
        }

        internal IEnumerable<UserProfile> GetAllProfiles()
        {
            return _profiles.Values;
        }
    }
}
