﻿using GetStoreApp.Extensions.DataType.Constant;
using GetStoreApp.Services.Root;
using System;
using System.Collections.Generic;

namespace GetStoreApp.Services.Settings
{
    /// <summary>
    /// 应用安装方式设置服务
    /// </summary>
    public static class InstallModeService
    {
        private static readonly string settingsKey = ConfigKey.InstallModeKey;

        private static KeyValuePair<string, string> defaultInstallMode;

        public static KeyValuePair<string, string> InstallMode { get; private set; }

        public static List<KeyValuePair<string, string>> InstallModeList { get; private set; }

        /// <summary>
        /// 应用在初始化前获取设置存储的应用安装方式值
        /// </summary>
        public static void InitializeInstallMode()
        {
            InstallModeList = ResourceService.InstallModeList;
            defaultInstallMode = InstallModeList.Find(item => string.Equals(item.Key, "AppInstall", StringComparison.OrdinalIgnoreCase));
            InstallMode = GetInstallMode();
        }

        /// <summary>
        /// 获取设置存储的应用安装方式值，如果设置没有存储，使用默认值
        /// </summary>
        private static KeyValuePair<string, string> GetInstallMode()
        {
            string installMode = LocalSettingsService.ReadSetting<string>(settingsKey);

            if (string.IsNullOrEmpty(installMode))
            {
                SetInstallMode(defaultInstallMode);
                return InstallModeList.Find(item => string.Equals(item.Key, defaultInstallMode.Key, StringComparison.OrdinalIgnoreCase));
            }

            KeyValuePair<string, string> selectedInstallMode = InstallModeList.Find(item => string.Equals(item.Key, installMode, StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrEmpty(selectedInstallMode.Key) ? defaultInstallMode : selectedInstallMode;
        }

        /// <summary>
        /// 应用安装方式发生修改时修改设置存储的应用安装方式值
        /// </summary>
        public static void SetInstallMode(KeyValuePair<string, string> installMode)
        {
            InstallMode = installMode;

            LocalSettingsService.SaveSetting(settingsKey, installMode.Key);
        }
    }
}
