﻿using GetStoreApp.Extensions.Console;
using GetStoreApp.Helpers.Controls.Store;
using GetStoreApp.Models.Controls.Store;
using GetStoreApp.Services.Controls.Settings;
using GetStoreApp.Services.Root;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GetStoreApp.Services.Shell
{
    /// <summary>
    /// 控制台请求服务
    /// </summary>
    public static class RequestService
    {
        private static string selectedType;
        private static string selectedChannel;
        private static string linkText;

        private static readonly List<string> TypeList = ["url", "ProductId",];
        private static readonly List<string> ChannelList = ["WIF", "WIS", "RP", "Retail"];

        /// <summary>
        /// 初始化请求的数据
        /// </summary>
        public static void InitializeQueryData(int typeIndex, int channelIndex, string link)
        {
            selectedType = TypeList[typeIndex];
            selectedChannel = ChannelList[channelIndex];
            linkText = link;
        }

        /// <summary>
        /// 获取链接
        /// </summary>
        public static async Task GetLinksAsync()
        {
            // 解析链接对应的产品 ID
            string productId = selectedType.Equals(TypeList[0], StringComparison.OrdinalIgnoreCase) ? QueryLinksHelper.ParseRequestContent(linkText) : linkText;
            InfoBarSeverity state = InfoBarSeverity.Informational;
            bool requestState = true;
            Console.WriteLine(ResourceService.GetLocalized("Console/GettingNow"));

            if (QueryLinksModeService.QueryLinksMode.Equals(QueryLinksModeService.QueryLinksModeList[0]))
            {
                while (requestState)
                {
                    string cookie = await QueryLinksHelper.GetCookieAsync();

                    List<QueryLinksModel> queryLinksList = [];
                    AppInfoModel appInfo = null;

                    // 获取应用信息
                    (bool requestResult, AppInfoModel appInfoModelItem) appInformationResult = await QueryLinksHelper.GetAppInformationAsync(productId);

                    if (appInformationResult.requestResult)
                    {
                        // 解析非商店应用数据
                        if (string.IsNullOrEmpty(appInformationResult.appInfoModelItem.CategoryID))
                        {
                            List<QueryLinksModel> nonAppxPackagesList = await QueryLinksHelper.GetNonAppxPackagesAsync(productId);
                            foreach (QueryLinksModel nonAppxPackage in nonAppxPackagesList)
                            {
                                queryLinksList.Add(nonAppxPackage);
                            }
                            state = queryLinksList.Count is 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
                        }
                        // 解析商店应用数据
                        else
                        {
                            string fileListXml = await QueryLinksHelper.GetFileListXmlAsync(cookie, appInformationResult.Item2.CategoryID, selectedChannel);

                            if (!string.IsNullOrEmpty(fileListXml))
                            {
                                List<QueryLinksModel> appxPackagesList = await QueryLinksHelper.GetAppxPackagesAsync(fileListXml, selectedChannel);
                                foreach (QueryLinksModel appxPackage in appxPackagesList)
                                {
                                    queryLinksList.Add(appxPackage);
                                }
                                state = queryLinksList.Count is 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
                            }
                        }

                        appInfo = appInformationResult.Item2;
                    }
                    else
                    {
                        state = InfoBarSeverity.Error;
                    }

                    Console.Write(Environment.NewLine);
                    Console.WriteLine(ResourceService.GetLocalized("Console/GetCompleted"));

                    switch (state)
                    {
                        case InfoBarSeverity.Success:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestSuccessfully"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                requestState = false;
                                await ParseService.ParseDataAsync(appInfo, queryLinksList);
                                break;
                            }
                        case InfoBarSeverity.Warning:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestFailed"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                PrintRequestFailedData();
                                Console.WriteLine(ResourceService.GetLocalized("Console/AskContinue"));
                                string regainString = Console.ReadLine();
                                requestState = regainString is "Y" or "y";
                                break;
                            }
                        case InfoBarSeverity.Error:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestError"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                Console.WriteLine(ResourceService.GetLocalized("Console/AskContinue"));
                                string regainString = Console.ReadLine();
                                requestState = regainString is "Y" or "y";
                                break;
                            }
                    }
                }
            }
            // 第三方接口
            else if (QueryLinksModeService.QueryLinksMode.Equals(QueryLinksModeService.QueryLinksModeList[1]))
            {
                while (requestState)
                {
                    string generateContent = HtmlRequestHelper.GenerateRequestContent(selectedType, linkText, selectedChannel);

                    // 获取网页反馈回的原始数据
                    RequestModel httpRequestData = await HtmlRequestHelper.HttpRequestAsync(generateContent);

                    Console.Write(Environment.NewLine);
                    Console.WriteLine(ResourceService.GetLocalized("Console/GetCompleted"));

                    state = HtmlRequestHelper.CheckRequestState(httpRequestData);

                    switch (state)
                    {
                        case InfoBarSeverity.Success:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestSuccessfully"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                requestState = false;
                                HtmlParseHelper.InitializeParseData(httpRequestData);
                                List<QueryLinksModel> queryLinksList = HtmlParseHelper.HtmlParsePackagedAppLinks();
                                await ParseService.ParseDataAsync(null, queryLinksList);
                                break;
                            }
                        case InfoBarSeverity.Warning:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestFailed"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                PrintRequestFailedData();
                                Console.WriteLine(ResourceService.GetLocalized("Console/AskContinue"));
                                string regainString = Console.ReadLine();
                                requestState = regainString is "Y" or "y";
                                break;
                            }
                        case InfoBarSeverity.Error:
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine(ResourceService.GetLocalized("Console/RequestError"));
                                Console.Write(Environment.NewLine);
                                Console.ResetColor();
                                Console.WriteLine(ResourceService.GetLocalized("Console/AskContinue"));
                                string regainString = Console.ReadLine();
                                requestState = regainString is "Y" or "y";
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// 获取失败时，打印获取失败时的数据
        /// </summary>
        private static void PrintRequestFailedData()
        {
            string SerialNumberHeader = ResourceService.GetLocalized("Console/SerialNumber");
            string FileNameHeader = ResourceService.GetLocalized("Console/FileName");
            string FileSizeHeader = ResourceService.GetLocalized("Console/FileSize");
            string None = ResourceService.GetLocalized("Console/None");

            int SerialNumberHeaderLength = CharExtension.GetStringDisplayLengthEx(SerialNumberHeader);
            int FileNameHeaderLength = CharExtension.GetStringDisplayLengthEx(FileNameHeader);
            int FileSizeHeaderLength = CharExtension.GetStringDisplayLengthEx(FileSizeHeader);
            int NoneLength = CharExtension.GetStringDisplayLengthEx(None);

            int SerialNumberColumnLength = (SerialNumberHeaderLength > "1".Length ? SerialNumberHeaderLength : "1".Length) + 3;
            int FileNameColumnLength = (FileNameHeaderLength > NoneLength ? FileNameHeaderLength : NoneLength) + 3;

            Console.Write(Environment.NewLine);
            Console.WriteLine(ResourceService.GetLocalized("Console/FileInfoList"));

            // 打印标题
            Console.Write(SerialNumberHeader + new string(ConsoleLaunchService.RowSplitCharacter, SerialNumberColumnLength - SerialNumberHeaderLength));
            Console.Write(FileNameHeader + new string(ConsoleLaunchService.RowSplitCharacter, FileNameColumnLength - FileNameHeaderLength));
            Console.Write(FileSizeHeader + Environment.NewLine);

            // 打印标题与内容的分割线
            Console.Write(new string(ConsoleLaunchService.ColumnSplitCharacter, SerialNumberHeaderLength).PadRight(SerialNumberColumnLength));
            Console.Write(new string(ConsoleLaunchService.ColumnSplitCharacter, FileNameHeaderLength).PadRight(FileNameColumnLength));
            Console.Write(new string(ConsoleLaunchService.ColumnSplitCharacter, FileSizeHeaderLength) + Environment.NewLine);

            // 输出内容
            Console.Write("1" + new string(ConsoleLaunchService.RowSplitCharacter, SerialNumberColumnLength - 1));
            Console.Write(None + new string(ConsoleLaunchService.RowSplitCharacter, FileNameColumnLength - NoneLength));
            Console.Write(None + Environment.NewLine);
        }
    }
}
