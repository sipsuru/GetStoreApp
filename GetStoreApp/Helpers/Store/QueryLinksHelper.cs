﻿using GetStoreApp.Helpers.Root;
using GetStoreApp.Models;
using GetStoreApp.Services.Root;
using GetStoreApp.Services.Settings;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Diagnostics;
using Windows.Security.Cryptography;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace GetStoreApp.Helpers.Store
{
    /// <summary>
    /// 查询链接辅助类
    /// </summary>
    public static class QueryLinksHelper
    {
        private static readonly Uri cookieUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        private static readonly Uri fileListXmlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        private static readonly Uri urlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");

        /// <summary>
        /// 解析输入框输入的字符串
        /// </summary>
        public static string ParseRequestContent(string requestContent)
        {
            if (requestContent.Contains('/'))
            {
                requestContent = requestContent[(requestContent.LastIndexOf('/') + 1)..];
            }
            if (requestContent.Contains('?'))
            {
                requestContent = requestContent[..requestContent.IndexOf('?')];
            }
            return requestContent;
        }

        /// <summary>
        /// 获取微软商店服务器储存在用户本地终端上的数据
        /// </summary>
        public static async Task<string> GetCookieAsync()
        {
            string cookieResult = string.Empty;

            try
            {
                byte[] cookieByteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/cookie.xml");

                HttpStringContent httpStringContent = new(CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(cookieByteArray)));
                httpStringContent.TryComputeLength(out ulong length);
                httpStringContent.Headers.Expires = DateTime.Now;
                httpStringContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpStringContent.Headers.ContentLength = length;
                httpStringContent.Headers.ContentType.CharSet = "utf-8";

                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestResult httpRequestResult = await httpClient.TryPostAsync(cookieUri, httpStringContent);
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetCookieAsync), 1, responseDict);
                    string responseString = await httpRequestResult.ResponseMessage.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(responseString))
                    {
                        XmlDocument responseStringDocument = new();
                        responseStringDocument.LoadXml(responseString);

                        XmlNodeList encryptedDataList = responseStringDocument.GetElementsByTagName("EncryptedData");
                        if (encryptedDataList.Count > 0)
                        {
                            cookieResult = encryptedDataList[0].InnerText;
                        }
                    }
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetCookieAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetCookieAsync), 3, e);
            }

            return cookieResult;
        }

        /// <summary>
        /// 获取应用信息
        /// </summary>
        /// <param name="productId">应用的产品 ID</param>
        /// <returns>打包应用：有，非打包应用：无</returns>
        public static async Task<(bool requestResult, AppInfoModel appInfo)> GetAppInformationAsync(string productId)
        {
            bool requestResult = false;
            AppInfoModel appInfo = new();

            try
            {
                string categoryIDAPI = string.Format("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/{0}?market={1}&locale={2}&deviceFamily=Windows.Desktop", productId, StoreRegionService.StoreRegion.CodeTwoLetter, LanguageService.AppLanguage.Key);

                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestResult httpRequestResult = await httpClient.TryGetAsync(new(categoryIDAPI));
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    requestResult = true;
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppInformationAsync), 1, responseDict);
                    string responseString = await httpRequestResult.ResponseMessage.Content.ReadAsStringAsync();

                    if (JsonObject.TryParse(responseString, out JsonObject responseStringObject))
                    {
                        JsonObject payLoadObject = responseStringObject.GetNamedValue("Payload").GetObject();

                        appInfo.Name = payLoadObject.GetNamedString("Title");
                        appInfo.Publisher = payLoadObject.GetNamedString("PublisherName");
                        appInfo.Description = payLoadObject.GetNamedString("Description");
                        appInfo.CategoryID = string.Empty;
                        appInfo.ProductID = productId;

                        JsonArray skusArray = payLoadObject.GetNamedArray("Skus");

                        if (skusArray.Count > 0)
                        {
                            appInfo.CategoryID = string.Empty;
                            JsonObject jsonObject = skusArray[0].GetObject();
                            if (jsonObject.TryGetValue("FulfillmentData", out IJsonValue jsonValue))
                            {
                                string fulfillmentData = jsonValue.GetString();
                                if (JsonObject.TryParse(fulfillmentData, out JsonObject fulfillmentDataObject))
                                {
                                    appInfo.CategoryID = fulfillmentDataObject.GetNamedString("WuCategoryId");
                                }
                            }
                        }
                    }
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppInformationAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppInformationAsync), 3, e);
            }

            return ValueTuple.Create(requestResult, appInfo);
        }

        /// <summary>
        /// 获取文件信息字符串
        /// </summary>
        /// <param name="cookie">cookie 数据</param>
        /// <param name="categoryId">category ID</param>
        /// <param name="ring">通道</param>
        /// <returns>文件信息的字符串</returns>
        public static async Task<string> GetFileListXmlAsync(string cookie, string categoryId, string ring)
        {
            string fileListXmlResult = string.Empty;

            try
            {
                byte[] wubyteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/wu.xml");
                string fileListXml = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(wubyteArray)).Replace("{1}", cookie).Replace("{2}", categoryId).Replace("{3}", ring);

                HttpStringContent httpStringContent = new(fileListXml);
                httpStringContent.TryComputeLength(out ulong length);
                httpStringContent.Headers.Expires = DateTime.Now;
                httpStringContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpStringContent.Headers.ContentLength = length;
                httpStringContent.Headers.ContentType.CharSet = "utf-8";

                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestResult httpRequestResult = await httpClient.TryPostAsync(fileListXmlUri, httpStringContent);
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetFileListXmlAsync), 1, responseDict);
                    string responseString = await httpRequestResult.ResponseMessage.Content.ReadAsStringAsync();
                    fileListXmlResult = responseString.Replace("&lt;", "<").Replace("&gt;", ">");
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetFileListXmlAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetFileListXmlAsync), 3, e);
            }

            return fileListXmlResult;
        }

        /// <summary>
        /// 获取商店应用安装包
        /// </summary>
        /// <param name="fileListXml">文件信息的字符串</param>
        /// <param name="ring">通道</param>
        /// <returns>带解析后文件信息的列表</returns>
        public static async Task<List<QueryLinksModel>> GetAppxPackagesAsync(string fileListXml, string ring)
        {
            List<QueryLinksModel> appxPackagesList = [];

            try
            {
                XmlDocument fileListDocument = new();
                fileListDocument.LoadXml(fileListXml);

                Dictionary<string, (string extension, string size, string digest)> appxPackagesInfoDict = [];
                XmlNodeList fileList = fileListDocument.GetElementsByTagName("File");

                foreach (IXmlNode fileNode in fileList)
                {
                    if (fileNode.Attributes.GetNamedItem("InstallerSpecificIdentifier") is IXmlNode installerSpecificIdentifierNode)
                    {
                        string name = installerSpecificIdentifierNode.InnerText;
                        string extension = fileNode.Attributes.GetNamedItem("FileName").InnerText[fileNode.Attributes.GetNamedItem("FileName").InnerText.LastIndexOf('.')..];
                        string size = fileNode.Attributes.GetNamedItem("Size").InnerText;
                        string digest = fileNode.Attributes.GetNamedItem("Digest").InnerText;

                        if (!appxPackagesInfoDict.ContainsKey(name))
                        {
                            appxPackagesInfoDict.Add(name, ValueTuple.Create(extension, size, digest));
                        }
                    }
                }

                Lock appxPackagesLock = new();
                XmlNodeList securedFragmentList = fileListDocument.DocumentElement.GetElementsByTagName("SecuredFragment");
                List<Task> taskList = [];

                foreach (IXmlNode securedFragmentNode in securedFragmentList)
                {
                    IXmlNode securedFragmentCloneNode = securedFragmentNode;
                    taskList.Add(Task.Run(async () =>
                    {
                        IXmlNode xmlNode = securedFragmentCloneNode.ParentNode.ParentNode;

                        if (xmlNode.GetElementsByName("ApplicabilityRules").GetElementsByName("Metadata").GetElementsByName("AppxPackageMetadata").GetElementsByName("AppxMetadata").Attributes.GetNamedItem("PackageMoniker") is IXmlNode packageMonikerNode)
                        {
                            string name = packageMonikerNode.InnerText;

                            if (appxPackagesInfoDict.TryGetValue(name, out (string extension, string size, string digest) value))
                            {
                                string fileName = name + value.extension;
                                string fileSize = value.size;
                                string digest = value.digest;
                                string revisionNumber = xmlNode.GetElementsByName("UpdateIdentity").Attributes.GetNamedItem("RevisionNumber").InnerText;
                                string updateID = xmlNode.GetElementsByName("UpdateIdentity").Attributes.GetNamedItem("UpdateID").InnerText;
                                string uri = await GetAppxUrlAsync(updateID, revisionNumber, ring, digest);
                                string fileSizeString = VolumeSizeHelper.ConvertVolumeSizeToString(double.TryParse(fileSize, out double size) ? size : 0);

                                appxPackagesLock.Enter();

                                try
                                {
                                    appxPackagesList.Add(new QueryLinksModel()
                                    {
                                        FileName = fileName,
                                        FileLink = uri,
                                        FileSize = fileSizeString,
                                        IsSelected = false,
                                        IsSelectMode = false
                                    });
                                }
                                catch (Exception e)
                                {
                                    ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                }
                                finally
                                {
                                    appxPackagesLock.Exit();
                                }
                            }
                        }
                    }));
                }

                await Task.WhenAll(taskList);
            }
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppxPackagesAsync), 1, e);
            }

            return appxPackagesList;
        }

        /// <summary>
        /// 获取商店应用安装包对应的下载链接
        /// </summary>
        /// <returns>商店应用安装包对应的下载链接</returns>
        private static async Task<string> GetAppxUrlAsync(string updateID, string revisionNumber, string ring, string digest)
        {
            string urlResult = string.Empty;

            try
            {
                byte[] urlbyteArray = ResourceService.GetEmbeddedData("Files/Assets/Embed/url.xml");
                string url = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, CryptographicBuffer.CreateFromByteArray(ResourceService.GetEmbeddedData("Files/Assets/Embed/url.xml"))).Replace("{1}", updateID).Replace("{2}", revisionNumber).Replace("{3}", ring);

                HttpStringContent httpContent = new(url);
                httpContent.TryComputeLength(out ulong length);
                httpContent.Headers.Expires = DateTime.Now;
                httpContent.Headers.ContentType = new HttpMediaTypeHeaderValue("application/soap+xml");
                httpContent.Headers.ContentLength = length;
                httpContent.Headers.ContentType.CharSet = "utf-8";

                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestResult httpRequestResult = await httpClient.TryPostAsync(urlUri, httpContent);
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppxUrlAsync), 1, responseDict);
                    string responseString = await httpRequestResult.ResponseMessage.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(responseString))
                    {
                        XmlDocument responseStringDocument = new();
                        responseStringDocument.LoadXml(responseString);

                        XmlNodeList fileLocationList = responseStringDocument.DocumentElement.GetElementsByTagName("FileLocation");

                        foreach (IXmlNode fileLocationNode in fileLocationList)
                        {
                            if (string.Equals(fileLocationNode.GetElementsByName("FileDigest").InnerText, digest))
                            {
                                urlResult = fileLocationNode.GetElementsByName("Url").InnerText;
                                break;
                            }
                        }
                    }
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppxUrlAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetAppxUrlAsync), 3, e);
            }

            return urlResult;
        }

        /// <summary>
        /// 获取非商店应用的安装包
        /// </summary>
        /// <param name="productId">产品 ID</param>
        /// <returns>带解析后文件信息的列表</returns>
        public static async Task<List<QueryLinksModel>> GetNonAppxPackagesAsync(string productId)
        {
            List<QueryLinksModel> nonAppxPackagesList = [];

            try
            {
                string url = string.Format("https://storeedgefd.dsx.mp.microsoft.com/v9.0/packageManifests/{0}?market={1}", productId, StoreRegionService.StoreRegion.CodeTwoLetter);

                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestResult httpRequestResult = await httpClient.TryGetAsync(new(url));
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackagesAsync), 1, responseDict);
                    string responseString = await httpRequestResult.ResponseMessage.Content.ReadAsStringAsync();

                    if (JsonObject.TryParse(responseString, out JsonObject responseStringObject))
                    {
                        JsonObject dataObject = responseStringObject.GetNamedValue("Data").GetObject();
                        JsonObject versionsObject = dataObject.GetNamedValue("Versions").GetArray()[0].GetObject();
                        JsonArray installersArray = versionsObject.GetNamedValue("Installers").GetArray();

                        Lock nonAppxPackagesLock = new();
                        List<Task> taskList = [];

                        foreach (IJsonValue installer in installersArray)
                        {
                            taskList.Add(Task.Run(async () =>
                            {
                                JsonObject installerObject = installer.GetObject();

                                string installerType = installerObject.GetNamedString("InstallerType");
                                string installerUrl = installerObject.GetNamedString("InstallerUrl");
                                string fileSize = await GetNonAppxPackageFileSizeAsync(installerUrl);
                                string fileSizeString = VolumeSizeHelper.ConvertVolumeSizeToString(double.TryParse(fileSize, out double size) ? size : 0);

                                if (string.IsNullOrEmpty(installerType) || installerUrl.ToLower().EndsWith(".exe") || installerUrl.ToLower().EndsWith(".msi"))
                                {
                                    nonAppxPackagesLock.Enter();

                                    try
                                    {
                                        nonAppxPackagesList.Add(new QueryLinksModel()
                                        {
                                            FileName = installerUrl[..installerUrl.LastIndexOf('.')][(installerUrl.LastIndexOf('/') + 1)..],
                                            FileLink = installerUrl,
                                            FileSize = fileSizeString,
                                            IsSelected = false,
                                            IsSelectMode = false,
                                        });
                                    }
                                    catch (Exception e)
                                    {
                                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                    }
                                    finally
                                    {
                                        nonAppxPackagesLock.Exit();
                                    }
                                }
                                else
                                {
                                    string name = installerUrl.Split('/')[^1];

                                    nonAppxPackagesLock.Enter();

                                    try
                                    {
                                        nonAppxPackagesList.Add(new QueryLinksModel()
                                        {
                                            FileName = string.Format("{0} ({1}).{2}", name, installerObject.GetNamedString("InstallerLocale"), installerType),
                                            FileLink = installerUrl,
                                            FileSize = fileSizeString,
                                            IsSelected = false,
                                            IsSelectMode = false,
                                        });
                                    }
                                    catch (Exception e)
                                    {
                                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                                    }
                                    finally
                                    {
                                        nonAppxPackagesLock.Exit();
                                    }
                                }
                            }));
                        }

                        await Task.WhenAll(taskList);
                    }
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackagesAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }
            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackagesAsync), 3, e);
            }

            return nonAppxPackagesList;
        }

        /// <summary>
        /// 获取非商店应用下载文件的大小
        /// </summary>
        private static async Task<string> GetNonAppxPackageFileSizeAsync(string url)
        {
            string fileSizeResult = "0";

            try
            {
                // 默认超时时间是 20 秒
                HttpClient httpClient = new();
                HttpRequestMessage requestMessage = new(HttpMethod.Head, new(url));
                HttpRequestResult httpRequestResult = await httpClient.TrySendRequestAsync(requestMessage);
                httpClient.Dispose();

                // 请求成功
                if (httpRequestResult.Succeeded && httpRequestResult.ResponseMessage.IsSuccessStatusCode)
                {
                    Dictionary<string, string> responseDict = new()
                    {
                        { "Status code", Convert.ToString(httpRequestResult.ResponseMessage.StatusCode) },
                        { "Headers", httpRequestResult.ResponseMessage.Headers is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                        { "Response message:", httpRequestResult.ResponseMessage.RequestMessage is null ? string.Empty : Convert.ToString(httpRequestResult.ResponseMessage.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
                    };

                    LogService.WriteLog(LoggingLevel.Information, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackageFileSizeAsync), 1, responseDict);
                    fileSizeResult = Convert.ToString(httpRequestResult.ResponseMessage.Content.Headers.ContentLength);
                }
                // 请求失败
                else
                {
                    LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackageFileSizeAsync), 2, httpRequestResult.ExtendedError);
                }

                httpRequestResult.Dispose();
            }

            // 其他异常
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(QueryLinksHelper), nameof(GetNonAppxPackageFileSizeAsync), 3, e);
            }

            return fileSizeResult;
        }

        private static IXmlNode GetElementsByName(this IXmlNode xmlNode, string name)
        {
            foreach (IXmlNode node in xmlNode.ChildNodes)
            {
                if (string.Equals(node.NodeName, name))
                {
                    return node;
                }
            }

            return null;
        }
    }
}
