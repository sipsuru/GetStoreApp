﻿using GetStoreAppWebView.Extensions.DataType.Constant;
using GetStoreAppWebView.Helpers.Root;
using GetStoreAppWebView.Services.Root;
using System.Collections.Generic;

namespace GetStoreAppWebView.Services.Controls.Settings
{
    /// <summary>
    /// 网页浏览器内核选择设置服务
    /// </summary>
    public static class WebKernelService
    {
        private static string WebKernelSettingsKey = ConfigKey.WebKernelKey;

        private static object DefaultWebKernel;

        public static object WebKernel { get; set; }

        public static List<object> WebKernelList { get; private set; } = new List<object>()
        {
            "IE",
            "WebView2",
        };

        /// <summary>
        /// 应用在初始化前获取设置存储的网页浏览器内核选择值
        /// </summary>
        public static void InitializeWebKernel()
        {
            DefaultWebKernel = WebKernelList.Find(item => item is "WebView2");

            WebKernel = GetWebKernel();
        }

        /// <summary>
        /// 获取设置存储的网页浏览器内核选择值，如果设置没有存储，使用默认值
        /// </summary>
        private static object GetWebKernel()
        {
            if (RuntimeHelper.IsWebView2Installed)
            {
                object webKernelValue = LocalSettingsService.ReadSetting<object>(WebKernelSettingsKey);

                if (webKernelValue is null)
                {
                    return DefaultWebKernel;
                }

                return WebKernelList.Find(item => item.Equals(webKernelValue));
            }
            else
            {
                return WebKernelList.Find(item => item.Equals("IE"));
            }
        }
    }
}