using System;
using System.IO;
using SFS.IO;
using ModLoader;

namespace SFSControl
{
    [Serializable]
    public class ModSettingsConfig
    {
        public int port = 27772;
        public bool allowScreenshot = false;
        public bool enableCORS = false;
        public string allowedOrigins = "*";
        public string allowedMethods = "GET,POST,PUT,DELETE,OPTIONS";
        public string allowedHeaders = "Content-Type,Authorization,X-Requested-With";
        public float simulationStepSize = 0.05f;
        public int simulationMaxSteps = 25000;
        public bool enableHeatingSimulation = true;
        public bool enableGlidingHeatshields = true;
    }

    public static class SettingsManager
    {
        public static string GetSettingsFolder()
        {
            string folder = "Mods/SFSControl";
            if (ModLoader.Loader.main != null)
            {
                foreach (var mod in ModLoader.Loader.main.GetAllMods())
                {
                    if (mod.ModNameID == "SFSControl")
                    {
                        folder = mod.ModFolder;
                        break;
                    }
                }
            }
            return folder;
        }

        public static string GetSettingsPath()
        {
            return Path.Combine(GetSettingsFolder(), "Settings.txt");
        }

        public static void Load()
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ModSettingsConfig>(json);
                }
                catch
                {
                    settings = new ModSettingsConfig();
                }
            }
            else
            {
                settings = new ModSettingsConfig();
            }
            if (settings == null)
                settings = new ModSettingsConfig();
        }

        public static void Save()
        {
            string path = GetSettingsPath();
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static ModSettingsConfig settings;
    }
}