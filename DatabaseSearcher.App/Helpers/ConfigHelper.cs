using DatabaseSearcher.App.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DatabaseSearcher.App.Helpers
{
    static class ConfigHelper
    {
        static readonly string _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "configs.json");
        
        public static async Task<SavedConfiguration?> LoadConfigurations()
        {
            if (File.Exists(_configFilePath))
            {
                using var fs = new FileStream(_configFilePath, FileMode.Open, FileAccess.Read);
                var configs = await JsonSerializer.DeserializeAsync<SavedConfiguration>(fs);
                return configs;
            }

            return null;
        }

        public static async Task SaveConfigurations(SavedConfiguration configuration)
        {
            using var fs = new FileStream(_configFilePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fs, configuration, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
