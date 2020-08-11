﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UpdatesClient.Core;
using UpdatesClient.Core.Enums;
using UpdatesClient.Modules.Configs;
using Yandex.Metrica;

namespace UpdatesClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Check file "SkyrimSE.exe"
        //Check file "skse64_loader.exe"
        //Base color: #FF04D9FF
        //War color: #FFFF7604
        //Er color: #FFFF0404

        private readonly BrushConverter converter = new BrushConverter();
        private BtnAction BtnAction = BtnAction.Check;

        public MainWindow()
        {
            InitializeComponent();
            TitleWindow.MouseLeftButtonDown += (s, e) => DragMove();

            Settings.Load();

            wind.Loaded += delegate {
                if (string.IsNullOrEmpty(Settings.PathToSkyrim))
                {
                    SetGameFolder();
                }

                ModVersion.Load();

                if (BtnAction != BtnAction.InstallSKSE)
                {
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                    CheckClient();
                }
            };

        }

        private void SetGameFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку с TES: Skyrim SE";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    if (!File.Exists(dialog.SelectedPath + "\\SkyrimSE.exe"))
                    {
                        MessageBox.Show("Skyrim SE не обнаружен", "Ошибка");
                        SetGameFolder();
                    }
                    else if (!File.Exists(dialog.SelectedPath + "\\skse64_loader.exe"))
                    {
                        if(MessageBox.Show("SKSE не обнаружен, установить?", "Ошибка", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Settings.PathToSkyrim = dialog.SelectedPath;
                            Settings.Save();
                            BtnAction = BtnAction.InstallSKSE;
                            DownloadLib();
                        }
                        else
                        {
                            SetGameFolder();
                        }
                    }
                    else
                    {
                        Settings.PathToSkyrim = dialog.SelectedPath;
                        Settings.Save();
                    }
                }
                else { Application.Current.Shutdown(); }
            }
        }

        private async void CheckClient()
        {
            MainBtn.IsIndeterminate = true;

            try
            {
                if (await Net.UpdateAvailable())
                {
                    MainBtn.StatusText = "Обновить";
                    BtnAction = BtnAction.Update;
                }
                else
                {
                    MainBtn.StatusText = "Играть";
                    BtnAction = BtnAction.Play;
                }
            }
            catch (Exception e)
            {
                YandexMetrica.ReportError("CheckClient", e);
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                MainBtn.StatusText = "Ошибка";
                await Task.Delay(1000);
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                MainBtn.StatusText = "Повторить";
            }
            MainBtn.IsIndeterminate = false;
            MainBtn.IsDisabled = false;
        }

        private void DownloadLib()
        {
            if(!File.Exists(Settings.PathToSkyrim + "\\tmp\\7z.dll"))
            {
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF7604");
                MainBtn.IsDisabled = true;
                MainBtn.IsIndeterminate = false;
                MainBtn.Value = 0;

                MainBtn.StatusText = "0%";

                string req = Net.URL_Lib;
                Downloader downloader = new Downloader($@"{Settings.PathToSkyrim}\tmp\{req.Substring(req.LastIndexOf('/'), req.Length - req.LastIndexOf('/'))}", req, "0.0.0.0");
                downloader.DownloadChanged += Downloader_DownloadChanged;
                downloader.DownloadComplete += Downloader_DownloadLibComplete;
                downloader.StartAsync();
            }
            else
            {
                InstallSKSE();
            }
        }
        private async void Downloader_DownloadLibComplete(string DestinationFile, string Vers)
        {
            if (DestinationFile == null)
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.Value = 100;
                    MainBtn.IsIndeterminate = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                    MainBtn.StatusText = "Ошибка";
                });
                await Task.Delay(1000);
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.IsDisabled = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF7604");
                    DownloadLib();
                });
            }
            else
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    InstallSKSE();
                });
            }
        }
        private async void InstallSKSE()
        {
            MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF7604");
            MainBtn.IsDisabled = true;
            MainBtn.IsIndeterminate = false;
            MainBtn.Value = 0;

            MainBtn.StatusText = "0%";

            string req = await Net.GetUrlToSKSE();
            Downloader downloader = new Downloader($@"{Settings.PathToSkyrim}\tmp\{req.Substring(req.LastIndexOf('/'), req.Length - req.LastIndexOf('/'))}", req, "0.0.0.0");
            downloader.DownloadChanged += Downloader_DownloadChanged;
            downloader.DownloadComplete += Downloader_DownloadSKSEComplete;
            downloader.StartAsync();
        }

        private async void Downloader_DownloadSKSEComplete(string DestinationFile, string Vers)
        {
            if (DestinationFile == null)
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.Value = 100;
                    MainBtn.IsIndeterminate = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                    MainBtn.StatusText = "Ошибка";
                });
                await Task.Delay(1000);
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.IsDisabled = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                    MainBtn.StatusText = "Повторить";
                });
            }
            else
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    ExtractSKSE(DestinationFile);
                });
            }
        }
        private async void ExtractSKSE(string file)
        {
            try
            {
                MainBtn.IsDisabled = true;
                MainBtn.IsIndeterminate = true;
                MainBtn.Value = 100;
                MainBtn.StatusText = "Распаковка";

                if (await Task.Run(() => Unpacker.SevenZUnpack(file, Settings.PathToSkyrim)))
                {
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                    CheckClient();
                }
                else
                {
                    MainBtn.StatusText = "Повторить";
                    BtnAction = BtnAction.InstallSKSE;
                }
                MainBtn.IsDisabled = false;
                MainBtn.IsIndeterminate = false;
                MainBtn.Value = 100;
            }
            catch (Exception e)
            {
                MainBtn.IsDisabled = false;
                MainBtn.IsIndeterminate = false;
                MainBtn.Value = 100;
                YandexMetrica.ReportError("ExtractSKSE", e);
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                MainBtn.StatusText = "Ошибка";
                await Task.Delay(1000);
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                MainBtn.StatusText = "Повторить";
            }
        }


        private void ImageButton_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainBtn_Click(object sender, EventArgs e)
        {
            switch (BtnAction)
            {
                case BtnAction.Play:
                    Play();
                    break;
                case BtnAction.Update:
                    Update();
                    break;
                case BtnAction.Check:
                    CheckClient();
                    break;
                case BtnAction.InstallSKSE:
                    InstallSKSE();
                    break;
            }
        }

        private async void Update()
        {
            MainBtn.IsDisabled = true;
            MainBtn.IsIndeterminate = false;
            MainBtn.Value = 0;

            MainBtn.StatusText = "0%";

            (string, string) req = await Net.GetUrlToClient();
            Downloader downloader = new Downloader($@"{Settings.PathToSkyrim}\tmp\client.zip", req.Item1, req.Item2);
            downloader.DownloadChanged += Downloader_DownloadChanged;
            downloader.DownloadComplete += Downloader_DownloadComplete;
            downloader.StartAsync();
        }

        private async void Downloader_DownloadComplete(string DestinationFile, string Vers)
        {
            if (DestinationFile == null)
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.Value = 100;
                    MainBtn.IsIndeterminate = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                    MainBtn.StatusText = "Ошибка";
                });
                await Task.Delay(1000);
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    MainBtn.IsDisabled = false;
                    MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                    MainBtn.StatusText = "Повторить";
                });
            }
            else
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
                {
                    Extract(DestinationFile, Vers);
                });
            }
        }

        private async void Extract(string file, string vers)
        {
            try
            {
                MainBtn.IsDisabled = true;
                MainBtn.IsIndeterminate = true;
                MainBtn.Value = 100;
                MainBtn.StatusText = "Распаковка";

                if(await Task.Run(() => Unpacker.Unpack(file, Settings.PathToSkyrim)))
                {
                    ModVersion.Version = vers;
                    ModVersion.Save();
                    MainBtn.StatusText = "Играть";
                    BtnAction = BtnAction.Play;
                }
                else
                {
                    MainBtn.StatusText = "Повторить";
                    BtnAction = BtnAction.Check;
                }
                MainBtn.IsDisabled = false;
                MainBtn.IsIndeterminate = false;
                MainBtn.Value = 100;
            }
            catch (Exception e)
            {
                YandexMetrica.ReportError("Extract", e);
                MainBtn.IsDisabled = false;
                MainBtn.IsIndeterminate = false;
                MainBtn.Value = 100;
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FFFF0404");
                MainBtn.StatusText = "Ошибка";
                await Task.Delay(1000);
                MainBtn.ColorBar = (Brush)converter.ConvertFrom("#FF04D9FF");
                MainBtn.StatusText = "Повторить";
            }
        }

        private void Downloader_DownloadChanged(double Value, double LenFile, double prDown)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Invoker)delegate
            {
                try
                {
                    MainBtn.Value = prDown;
                    MainBtn.StatusText = $"{prDown:0}%";
                }
                catch { }
            });
        }

        private void Play()
        {
            if(!File.Exists($"{Settings.PathToSkyrim}\\skse64_loader.exe"))
            {
                if (MessageBox.Show("SKSE не обнаружен, установить?", "Ошибка", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    InstallSKSE();
                return;
            }

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $"{Settings.PathToSkyrim}\\skse64_loader.exe",
                //Arguments = $"--UUID Launcher --Session TEST",
                WorkingDirectory = $"{Settings.PathToSkyrim}\\",
                Verb = "runas"
            };

            process.StartInfo = startInfo;
            process.Start();

            YandexMetrica.ReportEvent("StartGame");

            Close();
        }
    }
}
