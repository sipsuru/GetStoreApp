﻿using GetStoreAppInstaller.Extensions.DataType.Constant;
using GetStoreAppInstaller.Services.Root;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;

namespace GetStoreAppInstaller.Services.Settings
{
    /// <summary>
    /// 应用主题设置服务
    /// </summary>
    public static class ThemeService
    {
        private static readonly string settingsKey = ConfigKey.ThemeKey;

        private static string defaultAppTheme;

        private static string _appTheme;

        public static string AppTheme
        {
            get { return _appTheme; }

            private set
            {
                if (!string.Equals(_appTheme, value))
                {
                    _appTheme = value;
                    PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(AppTheme)));
                }
            }
        }

        public static List<string> ThemeList { get; private set; }

        public static event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 应用在初始化前获取设置存储的主题值
        /// </summary>
        public static void InitializeTheme()
        {
            ThemeList = ResourceService.ThemeList;
            defaultAppTheme = ThemeList.Find(item => string.Equals(item, nameof(ElementTheme.Default), StringComparison.OrdinalIgnoreCase));
            AppTheme = GetTheme();
        }

        /// <summary>
        /// 获取设置存储的主题值，如果设置没有存储，使用默认值
        /// </summary>
        private static string GetTheme()
        {
            string theme = LocalSettingsService.ReadSetting<string>(settingsKey);

            if (string.IsNullOrEmpty(theme))
            {
                return defaultAppTheme;
            }

            string selectedTheme = ThemeList.Find(item => string.Equals(item, theme, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(selectedTheme) ? defaultAppTheme : selectedTheme;
        }
    }
}
