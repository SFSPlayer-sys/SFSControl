using System;
using SFS.IO;
using SFS.Parsers.Json;
using ModLoader;

namespace SFSControl
{
    [Serializable]
    public class ModSettingsConfig
    {
        public int port = 27772; // 端口
        public bool allowScreenshot = false; // 是否允许截屏（默认关闭）
    }

    public static class SettingsManager
    {
        public static readonly FilePath Path = new FolderPath("Mods/SFSControl").ExtendToFile("Settings.txt");
        public static ModSettingsConfig settings;

        public static void Load()
        {
            // 重新读取最新的设置文件
            if (!JsonWrapper.TryLoadJson(Path, out settings))
            {
                // 如果文件不存在或读取失败，使用默认设置
                settings = new ModSettingsConfig();
                // 只在首次创建时保存默认设置
                Save();
            }
        }

        public static void Save()
        {
            Path.WriteText(JsonWrapper.ToJson(settings, true));
        }

        // 获取Mod文件夹路径（保留原有方法以兼容）
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