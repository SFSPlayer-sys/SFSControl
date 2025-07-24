using System;
using SFS.IO;
using SFS.Parsers.Json;
using ModLoader;

namespace SFSControl
{
    [Serializable]
    public class ModSettingsConfig
    {
        public int port = 27772; //端口
    }

    public static class SettingsManager
    {
        // 获取Mod文件夹路径
        private static string GetSettingsPath()
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
            return System.IO.Path.Combine(folder, "Settings.txt");
        }

        public static void SaveSettings(ModSettingsConfig settings)
        {
            var settingsPath = new FilePath(GetSettingsPath());
            JsonWrapper.SaveAsJson(settingsPath, settings, true);
        }

        public static ModSettingsConfig LoadSettings()
        {
            var settingsPath = new FilePath(GetSettingsPath());
            ModSettingsConfig settings;
            if (!JsonWrapper.TryLoadJson(settingsPath, out settings))
                settings = new ModSettingsConfig();
            return settings;
        }
    }
}
