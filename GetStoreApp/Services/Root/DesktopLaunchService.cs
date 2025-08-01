﻿using GetStoreApp.Extensions.DataType.Classes;
using GetStoreApp.Extensions.DataType.Enums;
using GetStoreApp.Models;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.System;

namespace GetStoreApp.Services.Root
{
    /// <summary>
    /// 桌面应用启动服务
    /// </summary>
    public static class DesktopLaunchService
    {
        private static bool isLaunched = false;
        private static AppInstance currentInstance;

        private static List<TypeModel> TypeList { get; } =
        [
            new TypeModel { InternalName = "url", ShortName = "url" },
            new TypeModel { InternalName = "ProductId", ShortName = "pid" }
        ];

        private static List<ChannelModel> ChannelList { get; } =
        [
            new ChannelModel { InternalName = "WIF", ShortName = "wif" },
            new ChannelModel { InternalName = "WIS", ShortName = "wis" },
            new ChannelModel { InternalName = "RP", ShortName = "rp" },
            new ChannelModel { InternalName = "Retail", ShortName = "rt" }
        ];

        /// <summary>
        /// 处理桌面应用启动的方式
        /// </summary>
        public static async Task InitializeLaunchAsync(AppActivationArguments appActivationArguments)
        {
            currentInstance = AppInstance.FindOrRegisterForKey("GetStoreApp");
            isLaunched = !currentInstance.IsCurrent;

            // 正常参数启动
            if (appActivationArguments.Kind is ExtendedActivationKind.Launch)
            {
                List<string> argumentsList = [];
                List<string> dataList = [];
                AppLaunchArguments appLaunchArguments = new();
                string[] argumentsArray = Environment.GetCommandLineArgs();
                string executableFileName = Path.GetFileName(Environment.ProcessPath);

                if (argumentsArray.Length > 0 && string.Equals(Path.GetExtension(argumentsArray[0]), ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    argumentsArray[0] = argumentsArray[0].Replace(".dll", ".exe");
                }

                foreach (string arguments in argumentsArray)
                {
                    if (arguments.Contains(executableFileName) || string.IsNullOrEmpty(arguments))
                    {
                        continue;
                    }
                    else
                    {
                        argumentsList.Add(arguments);
                    }
                }

                // 正常启动
                if (argumentsList.Count is 0)
                {
                    appLaunchArguments.AppLaunchKind = AppLaunchKind.Launch;
                    appLaunchArguments.IsLaunched = isLaunched;
                    appLaunchArguments.SubParameters = [];
                }
                else if (argumentsList.Count is 1)
                {
                    // 应用程序重新启动
                    if (argumentsList[0] is "Restart")
                    {
                        appLaunchArguments.AppLaunchKind = AppLaunchKind.Launch;
                        appLaunchArguments.IsLaunched = isLaunched;
                        appLaunchArguments.SubParameters = ["Restart"];
                    }
                    // 带参数启动：只有一个参数，直接输入链接
                    else
                    {
                        appLaunchArguments.AppLaunchKind = AppLaunchKind.Console;
                        appLaunchArguments.IsLaunched = isLaunched;
                        appLaunchArguments.SubParameters = ["-1", "-1", argumentsList[0]];
                    }
                }
                else if (argumentsList.Count >= 2)
                {
                    // 从跳转列表启动或从辅助磁贴启动
                    if (argumentsList[0] is "JumpList" || argumentsList[0] is "SecondaryTile")
                    {
                        if (argumentsList[1] is "Web")
                        {
                            await Launcher.LaunchUriAsync(new Uri("getstoreappwebbrowser:"));
                            Environment.Exit(Environment.ExitCode);
                        }
                        else
                        {
                            if (argumentsList[0] is "JumpList")
                            {
                                appLaunchArguments.AppLaunchKind = AppLaunchKind.JumpList;
                            }
                            else if (argumentsList[0] is "SecondaryTile")
                            {
                                appLaunchArguments.AppLaunchKind = AppLaunchKind.SecondaryTile;
                            }

                            appLaunchArguments.IsLaunched = isLaunched;
                            appLaunchArguments.SubParameters = argumentsList[1..];
                        }
                    }
                    // 带参数启动：包含多个参数
                    else if (argumentsList.Count % 2 is 0)
                    {
                        int typeNameParameterIndex = argumentsList.FindIndex(item => string.Equals(item, "-t", StringComparison.OrdinalIgnoreCase) || string.Equals(item, "--type", StringComparison.OrdinalIgnoreCase));
                        int channelNameParameterIndex = argumentsList.FindIndex(item => string.Equals(item, "-c", StringComparison.OrdinalIgnoreCase) || string.Equals(item, "--channel", StringComparison.OrdinalIgnoreCase));
                        int linkParameterIndex = argumentsList.FindIndex(item => string.Equals(item, "-l", StringComparison.OrdinalIgnoreCase) || string.Equals(item, "--link", StringComparison.OrdinalIgnoreCase));

                        int typeNameIndex = typeNameParameterIndex is -1 ? -1 : TypeList.FindIndex(item => string.Equals(item.ShortName, argumentsList[typeNameParameterIndex + 1], StringComparison.OrdinalIgnoreCase));
                        int channelNameIndex = channelNameParameterIndex is -1 ? -1 : ChannelList.FindIndex(item => string.Equals(item.ShortName, argumentsList[channelNameParameterIndex + 1], StringComparison.OrdinalIgnoreCase));
                        string link = linkParameterIndex is -1 ? null : argumentsList[linkParameterIndex + 1];

                        appLaunchArguments.AppLaunchKind = AppLaunchKind.Console;
                        appLaunchArguments.IsLaunched = isLaunched;
                        appLaunchArguments.SubParameters = [Convert.ToString(typeNameIndex), Convert.ToString(channelNameIndex), string.IsNullOrEmpty(link) ? "PlaceHolderText" : argumentsList[5]];
                    }
                    else
                    {
                        Environment.Exit(Environment.ExitCode);
                    }
                }

                await AppLaunchService.SaveArgumentsAsync(appLaunchArguments);
            }
            // 通过共享目标启动
            else if (appActivationArguments.Kind is ExtendedActivationKind.ShareTarget)
            {
                ShareTargetActivatedEventArgs shareTargetActivatedEventArgs = appActivationArguments.Data as ShareTargetActivatedEventArgs;
                ShareOperation shareOperation = shareTargetActivatedEventArgs.ShareOperation;
                shareOperation.ReportCompleted();

                if (shareOperation.Data.Contains(StandardDataFormats.Uri))
                {
                    AppLaunchArguments appLaunchArguments = new()
                    {
                        AppLaunchKind = AppLaunchKind.ShareTarget,
                        IsLaunched = isLaunched,
                        SubParameters = ["-1", "-1", Convert.ToString(await shareOperation.Data.GetUriAsync())]
                    };

                    await AppLaunchService.SaveArgumentsAsync(appLaunchArguments);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            // 通过协议启动
            else if (appActivationArguments.Kind is ExtendedActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs protocolActivatedEventArgs = appActivationArguments.Data as ProtocolActivatedEventArgs;
                AppLaunchArguments appLaunchArguments = new()
                {
                    AppLaunchKind = AppLaunchKind.Protocol,
                    IsLaunched = isLaunched
                };

                if (protocolActivatedEventArgs.Data is ValueSet dataSet && dataSet.TryGetValue("Parameter", out object parameterobj))
                {
                    appLaunchArguments.SubParameters = [Convert.ToString(parameterobj)];
                }

                await AppLaunchService.SaveArgumentsAsync(appLaunchArguments);
            }
            // 应用通知启动
            else if (appActivationArguments.Kind is ExtendedActivationKind.ToastNotification)
            {
                ToastNotificationActivatedEventArgs toastNotificationActivatedEventArgs = appActivationArguments.Data as ToastNotificationActivatedEventArgs;
                await ToastNotificationService.HandleToastNotificationAsync(toastNotificationActivatedEventArgs.Argument, isLaunched);
            }
            // 暂不支持以其他方式启动应用
            else
            {
                Environment.Exit(Environment.ExitCode);
            }

            // 若应用程序已启动，将启动信息重定向到已启动的应用中
            if (isLaunched)
            {
                ApplicationData.Current.SignalDataChanged();

                try
                {
                    await currentInstance.RedirectActivationToAsync(appActivationArguments);
                }
                catch (Exception e)
                {
                    LogService.WriteLog(LoggingLevel.Warning, nameof(GetStoreApp), nameof(DesktopLaunchService), nameof(InitializeLaunchAsync), 1, e);
                }
                finally
                {
                    Environment.Exit(Environment.ExitCode);
                }
            }
        }
    }
}
