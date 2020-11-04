﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UpdatesClient.Modules.Configs.Models;
using Yandex.Metrica;

namespace UpdatesClient.Modules.Configs
{
    internal class Settings
    {
        private static SettingsFileModel model;

        public static readonly string VersionFile = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        public static readonly string VersionAssembly = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static readonly string PathToLocal = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\UpdatesClient\\";
        public static readonly string PathToLocalSkyrim = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Skyrim Special Edition\\";
        public static readonly string PathToLocalTmp = $"{PathToLocal}tmp\\";
        public static readonly string PathToLocalDlls = $"{PathToLocal}dlls\\";
        public static readonly string PathToSettingsFile = $"{PathToLocal}{VersionAssembly}.json";
        public static readonly string PathToSavedServerList = $"{PathToLocalTmp}\\Servers.json";
        public static string PathToSkympClientSettings => $"{PathToSkyrim}\\Data\\Platform\\Plugins\\skymp5-client-settings.txt";

        public static string PathToSkyrim { get { return model.PathToSkyrim; } set { model.PathToSkyrim = value; } }
        public static string PathToSkyrimTmp => PathToSkyrim + "\\tmp\\";
        public static string LastVersion { get { return model.LastVersion; } private set { model.LastVersion = value; } }
        public static int UserId { get { return model.UserId; } set { model.UserId = value; } }
        public static string UserName { get; set; }
        public static bool RememberMe { get; set; } = true;
        private static string userToken;
        public static string UserToken
        {
            get { return Security.FromAes256Base64(RememberMe ? model.UserToken : userToken); }
            set { if (RememberMe) model.UserToken = Security.ToAes256Base64(value); else userToken = Security.ToAes256Base64(value); }
        }
        public static int LastServerID { get { return model.LastServerID; } set { model.LastServerID = value; } }

        internal static bool Load()
        {
            try
            {
                if (File.Exists(PathToSettingsFile))
                {
                    model = JsonConvert.DeserializeObject<SettingsFileModel>(File.ReadAllText(PathToSettingsFile));
                    return true;
                }
                else
                {
                    model = new SettingsFileModel();
                }
            }
            catch (Exception e)
            {
                YandexMetrica.ReportError("Settings_Load", e);
            }
            return false;
        }
        internal static void Save()
        {
            try
            {
                if (!Directory.Exists(PathToLocal)) Directory.CreateDirectory(PathToLocal);
                File.WriteAllText(PathToSettingsFile, JsonConvert.SerializeObject(model));
            }
            catch (Exception e)
            {
                YandexMetrica.ReportError("Settings_Save", e);
            }
        }
        internal static void Reset()
        {
            try
            {
                if (File.Exists(PathToSettingsFile)) File.Delete(PathToSettingsFile);
                model = new SettingsFileModel();
            }
            catch (Exception e)
            {
                YandexMetrica.ReportError("Settings_Reset", e);
            }
        }
    }
}
