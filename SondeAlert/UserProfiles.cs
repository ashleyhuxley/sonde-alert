using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ElectricFox.SondeAlert
{
    public class UserProfiles
    {
        private readonly ILogger<UserProfiles> logger;

        private readonly List<UserProfile> Profiles = new();

        private readonly SondeAlertOptions options;

        public UserProfiles(ILogger<UserProfiles> logger, IOptions<SondeAlertOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
        }

        public void AddUserProfile(UserProfile profile)
        {
            this.Profiles.Add(profile);
            this.SaveUserProfiles();
        }

        public void RemoveUserProfile(long chatId)
        {
            var profile = this.Profiles.SingleOrDefault(p => p.ChatId == chatId);
            if (profile is not null)
            {
                this.Profiles.Remove(profile);
                this.SaveUserProfiles();
            }
        }

        public IEnumerable<UserProfile> GetAllProfiles()
        {
            return this.Profiles;
        }

        public bool HasProfile(long chatId)
        {
            return this.Profiles.Any(p => p.ChatId == chatId);
        }

        public void LoadUserProfiles()
        {
            if (this.options.ProfilePath is null || !File.Exists(this.options.ProfilePath))
            {
                this.logger.LogWarning("Profiles file does not exist, skipping load.");
                return;
            }

            this.logger.LogInformation($"Loading profiles from {this.options.ProfilePath}");

            try
            {
                var profilesJson = File.ReadAllText(this.options.ProfilePath);
                var profiles = JsonSerializer.Deserialize<IEnumerable<UserProfile>>(profilesJson);

                if (profiles is null)
                {
                    throw new JsonException("Profiles deserialized to null");
                }

                this.Profiles.Clear();
                this.Profiles.AddRange(profiles);
            }
            catch (Exception ex)
            {
                this.logger.LogCritical(ex, $"Exception saving profiles: {ex.Message}");
                return;
            }

            this.logger.LogInformation($"{this.Profiles.Count} profiles loaded.");
        }

        private void SaveUserProfiles()
        {
            if (this.options.ProfilePath is null)
            {
                return;
            }

            this.logger.LogInformation(
                $"Saving {this.Profiles.Count} user profiles to {this.options.ProfilePath}"
            );

            try
            {
                var json = JsonSerializer.Serialize(this.Profiles);
                File.WriteAllText(this.options.ProfilePath, json);
            }
            catch (Exception ex)
            {
                this.logger.LogCritical(ex, $"Exception saving profiles: {ex.Message}");
            }
        }
    }
}
