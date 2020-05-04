﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UpdatesClient.Modules.SelfUpdater;
using SplashScreen = UpdatesClient.Modules.SelfUpdater.SplashScreen;

namespace UpdatesClient
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    internal delegate void Invoker();
    public partial class App : Application
    {
        private const string BeginUpdate = "begin";
        private const string EndUpdate = "end";

        internal delegate void ApplicationInitializeDelegate(SplashScreen splashWindow);
        internal ApplicationInitializeDelegate ApplicationInitialize;

        private SplashScreen SplashWindow;

        private string MasterHash;
        private string SelfHash;

        private string FullPathToSelfExe = Assembly.GetExecutingAssembly().Location;
        private string NameExeFile = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

        public static new App Current { get { return Application.Current as App; } }

        public App()
        {
            if (!Security.CheckEnvironment()) { ExitApp(); return; }
            if (!HandleCmdArgs()) { ExitApp(); return; }

            InitApp();
        }

        private bool HandleCmdArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                try
                {
                    switch (args[1])
                    {
                        case EndUpdate:
                            File.Delete($"{args[2]}.update.exe");
                            break;
                        case BeginUpdate:
                            File.Copy(FullPathToSelfExe, $"{args[2]}.exe", true);
                            Process.Start($"{args[2]}.exe", $"{EndUpdate} {args[2]}");
                            ExitApp();
                            return false;
                        default:
                            ExitApp();
                            return false;
                    }
                }
                catch { }
            }
            return true;
        }

        private void InitApp()
        {
            ApplicationInitialize = _applicationInitialize;
        }

        private void ExitApp()
        {
            Application.Current.Shutdown();
        }

        private async void _applicationInitialize(SplashScreen splashWindow)
        {
            try
            {
                SplashWindow = splashWindow;
#if (DEBUG || DeR) 
                StartLuancher();
#else
                SplashWindow.SetStatus("Проверка обновления лаунчера");

                MasterHash = await Updater.GetLauncherHash();
                SelfHash = Hashing.GetSHA512FromFile(File.OpenRead(FullPathToSelfExe));

                if (MasterHash == null || MasterHash == "") throw new Exception("Hash is empty");
                if (NameExeFile == null || NameExeFile == "") throw new Exception("Path is empty");

                if (!CheckFile(NameExeFile))
                {
                    SplashWindow.SetStatus("Обновление лаунчера");
                    SplashWindow.SetProgressMode(false);
                    bool downloaded = Update();

                    if (downloaded && CheckFile($"{NameExeFile}.update.exe"))
                    {
                        Process p = new Process();
                        p.StartInfo.FileName = $"{NameExeFile}.update.exe";
                        p.StartInfo.Arguments = $"{BeginUpdate} {NameExeFile}";
                        p.Start();
                    }
                    else
                    {
                        SplashWindow.SetStatus("Не удалось выполнить обновление лаунчера");
                        Thread.Sleep(1500);
                    }
                }
                else
                {
                    SplashWindow.SetStatus("Готово");
                    SplashWindow.SetProgressMode(false);
                    StartLuancher();
                }
#endif
            }
            catch (Exception e) { MessageBox.Show("Сведения: \n" + e.ToString(), "Критическая ошибка"); }
        }
        private void StartLuancher()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            });
        }
        //****************************************************************//
        private bool CheckFile(string pathToFile)
        {
            if (File.Exists(pathToFile) && MasterHash.ToUpper() == Hashing.GetSHA512FromFile(File.OpenRead(pathToFile)).ToUpper()) return true;
            else return false;
        }
        private bool Update()
        {
            Downloader downloader = new Downloader(Updater.AddressToLauncher, $"{NameExeFile}.update.exe", MasterHash);
            downloader.DownloadChanged += SplashWindow.SetProgress;
            return downloader.Download();
        }
    }
}
