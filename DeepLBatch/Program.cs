using Cocona;
using Cocona.Help;
using DeepL;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace DeepLBatch
{
    internal class Program
    {
        private const string UserSettingsFile = "userSettings.json";

        static void Main(string[] args)
        {
            CoconaApp.Run<CommandLineHandlers>();
        }

        public static string? GetApiKey()
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(environment) ||
                                environment.ToLower() == "development";

            var builder = new ConfigurationBuilder()
                .AddJsonFile(UserSettingsFile, true)
                .AddEnvironmentVariables();

            if (isDevelopment) builder.AddUserSecrets<Program>();

            var configurationRoot = builder.Build();

            string? apiKey = configurationRoot.GetSection("UserSettings:ApiKey").Value;

            return string.IsNullOrEmpty(apiKey) ? null : apiKey;
        }


        public static void SetApiKey(string apiKey)
        {
            //Currently api key is the only user setting used.
            SetUserSettings(new UserSettings() {  ApiKey = apiKey });
        }

        private static void SetUserSettings(UserSettings userSettings)
        {
            string userSettingsPath = Path.Combine(AppContext.BaseDirectory, UserSettingsFile);


            //string json = System.Text.Json.JsonSerializer.Serialize<UserSettings>(userSettings, new JsonSerializerOptions()
            string json = System.Text.Json.JsonSerializer.Serialize(new { UserSettings = userSettings }, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            File.WriteAllText(userSettingsPath, json);
        }


    }
}
