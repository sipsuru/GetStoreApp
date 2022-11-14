﻿using GetStoreApp.Contracts.Services.Controls.Settings.Common;
using GetStoreAppCore.Settings;
using System;
using System.Threading.Tasks;

namespace GetStoreApp.Services.Controls.Settings.Common
{
    /// <summary>
    /// 使用说明按钮显示服务
    /// </summary>
    public class UseInstructionService : IUseInstructionService
    {
        private string SettingsKey { get; init; } = ConfigStorage.ConfigKey["UseInstructionKey"];

        private bool DefaultUseInsVisValue => true;

        public bool UseInsVisValue { get; set; }

        /// <summary>
        /// 应用在初始化前获取设置存储的使用说明按钮显示值
        /// </summary>
        public async Task InitializeUseInsVisValueAsync()
        {
            UseInsVisValue = await GetUseInsVisValueAsync();
        }

        /// <summary>
        /// 获取设置存储的使用说明按钮显示值，如果设置没有存储，使用默认值
        /// </summary>
        private async Task<bool> GetUseInsVisValueAsync()
        {
            bool? useInsVisValue = await ConfigStorage.ReadSettingAsync<bool?>(SettingsKey);

            if (!useInsVisValue.HasValue)
            {
                return DefaultUseInsVisValue;
            }

            return Convert.ToBoolean(useInsVisValue);
        }

        /// <summary>
        /// 使用说明按钮显示发生修改时修改设置存储的使用说明按钮显示值
        /// </summary>
        public async Task SetUseInsVisValueAsync(bool useInsVisValue)
        {
            UseInsVisValue = useInsVisValue;

            await ConfigStorage.SaveSettingAsync(SettingsKey, useInsVisValue);
        }
    }
}