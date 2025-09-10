﻿using GetStoreApp.Services.Download;
using GetStoreApp.Services.Root;
using GetStoreApp.Views.Windows;
using GetStoreApp.WindowsAPI.PInvoke.User32;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.UI.StartScreen;

// 抑制 CA1822 警告
#pragma warning disable CA1822

namespace GetStoreApp
{
    /// <summary>
    /// 获取商店应用程序
    /// </summary>
    public partial class WinUIApp : Application, IDisposable
    {
        private bool isDisposed;

        public Window Window { get; private set; }

        public WinUIApp()
        {
            InitializeComponent();
            DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
            UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// 启动应用程序时调用，初始化应用主窗口
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            Window = new MainWindow();
            Window.Activate();
            SetAppIcon(Window.AppWindow);

            if (JumpList.IsSupported())
            {
                Task.Run(async () =>
                {
                    JumpList jumpList = await JumpList.LoadCurrentAsync();
                    jumpList.Items.Clear();
                    jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

                    JumpListItem storeItem = JumpListItem.CreateWithArguments("JumpList Store", ResourceService.GetLocalized("Application/Store"));
                    storeItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/Store.png");
                    jumpList.Items.Add(storeItem);

                    JumpListItem appUpdateItem = JumpListItem.CreateWithArguments("JumpList AppUpdate", ResourceService.GetLocalized("Application/AppUpdate"));
                    appUpdateItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/AppUpdate.png");
                    jumpList.Items.Add(appUpdateItem);

                    jumpList.Items.Add(JumpListItem.CreateSeparator());

                    JumpListItem wingetItem = JumpListItem.CreateWithArguments("JumpList WinGet", ResourceService.GetLocalized("Application/WinGet"));
                    wingetItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/WinGet.png");
                    jumpList.Items.Add(wingetItem);

                    JumpListItem appManagerItem = JumpListItem.CreateWithArguments("JumpList AppManager", ResourceService.GetLocalized("Application/AppManager"));
                    appManagerItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/AppManager.png");
                    jumpList.Items.Add(appManagerItem);

                    jumpList.Items.Add(JumpListItem.CreateSeparator());

                    JumpListItem downloadItem = JumpListItem.CreateWithArguments("JumpList Download", ResourceService.GetLocalized("Application/Download"));
                    downloadItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/Download.png");
                    jumpList.Items.Add(downloadItem);

                    //JumpListItem webItem = JumpListItem.CreateWithArguments("JumpList Web", ResourceService.GetLocalized("Application/Web"));
                    //webItem.Logo = new Uri("ms-appx:///Assets/Icon/Control/Web.png");
                    //jumpList.Items.Add(webItem);

                    await jumpList.SaveAsync();
                });
            }
        }

        /// <summary>
        /// 处理桌面应用程序未知异常处理
        /// </summary>
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
        {
            args.Handled = true;
            LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(WinUIApp), nameof(OnUnhandledException), 1, args.Exception);
        }

        /// <summary>
        /// 设置应用窗口图标
        /// </summary>
        private void SetAppIcon(AppWindow appWindow)
        {
            // 选中文件中的图标总数
            int iconTotalCount = User32Library.PrivateExtractIcons(Environment.ProcessPath, 0, 0, 0, null, null, 0, 0);

            // 用于接收获取到的图标指针
            nint[] hIcons = new nint[iconTotalCount];

            // 对应的图标id
            int[] ids = new int[iconTotalCount];

            // 成功获取到的图标个数
            int successCount = User32Library.PrivateExtractIcons(Environment.ProcessPath, 0, 256, 256, hIcons, ids, iconTotalCount, 0);

            // GetStoreApp.exe 应用程序只有一个图标
            if (successCount >= 1 && hIcons[0] != nint.Zero)
            {
                appWindow.SetIcon(Win32Interop.GetIconIdFromIcon(hIcons[0]));
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WinUIApp()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                GlobalNotificationService.SendNotification();
                DownloadSchedulerService.CloseDownloadScheduler();
                LogService.CloseLog();
                isDisposed = true;
            }

            Exit();
        }
    }
}
