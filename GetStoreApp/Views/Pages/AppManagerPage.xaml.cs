using GetStoreApp.Extensions.DataType.Classes;
using GetStoreApp.Extensions.DataType.Enums;
using GetStoreApp.Helpers.Root;
using GetStoreApp.Models.Controls.AppManager;
using GetStoreApp.Services.Root;
using GetStoreApp.UI.TeachingTips;
using GetStoreApp.Views.Windows;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store.Preview;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.System;
using Windows.UI.Shell;
using Windows.UI.StartScreen;
using Windows.UI.Text;

// 抑制 CA1822，IDE0060 警告
#pragma warning disable CA1822,IDE0060

namespace GetStoreApp.Views.Pages
{
    /// <summary>
    /// 应用管理页面
    /// </summary>
    public sealed partial class AppManagerPage : Page, INotifyPropertyChanged
    {
        private readonly string Unknown = ResourceService.GetLocalized("AppManager/Unknown");
        private readonly string Yes = ResourceService.GetLocalized("AppManager/Yes");
        private readonly string No = ResourceService.GetLocalized("AppManager/No");
        private readonly string PackageCountInfo = ResourceService.GetLocalized("AppManager/PackageCountInfo");
        private bool isInitialized;
        private bool needToRefreshData;
        private readonly PackageManager packageManager = new();

        private int _selectedIndex;

        public int SelectedIndex
        {
            get { return _selectedIndex; }

            set
            {
                if (!Equals(_selectedIndex, value))
                {
                    _selectedIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
                }
            }
        }

        private string _searchText = string.Empty;

        public string SearchText
        {
            get { return _searchText; }

            set
            {
                if (!Equals(_searchText, value))
                {
                    _searchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                }
            }
        }

        private bool _isLoadedCompleted;

        public bool IsLoadedCompleted
        {
            get { return _isLoadedCompleted; }

            set
            {
                if (!Equals(_isLoadedCompleted, value))
                {
                    _isLoadedCompleted = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadedCompleted)));
                }
            }
        }

        private bool _isPackageEmpty = true;

        public bool IsPackageEmpty
        {
            get { return _isPackageEmpty; }

            set
            {
                if (!Equals(_isPackageEmpty, value))
                {
                    _isPackageEmpty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPackageEmpty)));
                }
            }
        }

        private bool _isIncrease = true;

        public bool IsIncrease
        {
            get { return _isIncrease; }

            set
            {
                if (!Equals(_isIncrease, value))
                {
                    _isIncrease = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsIncrease)));
                }
            }
        }

        private bool _isAppFramework;

        public bool IsAppFramework
        {
            get { return _isAppFramework; }

            set
            {
                if (!Equals(_isAppFramework, value))
                {
                    _isAppFramework = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAppFramework)));
                }
            }
        }

        private AppSortRuleKind _selectedRule = AppSortRuleKind.DisplayName;

        public AppSortRuleKind SelectedRule
        {
            get { return _selectedRule; }

            set
            {
                if (!Equals(_selectedRule, value))
                {
                    _selectedRule = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRule)));
                }
            }
        }

        private bool _isStoreSignatureSelected = true;

        public bool IsStoreSignatureSelected
        {
            get { return _isStoreSignatureSelected; }

            set
            {
                if (!Equals(_isStoreSignatureSelected, value))
                {
                    _isStoreSignatureSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStoreSignatureSelected)));
                }
            }
        }

        private bool _isSystemSignatureSelected;

        public bool IsSystemSignatureSelected
        {
            get { return _isSystemSignatureSelected; }

            set
            {
                if (!Equals(_isSystemSignatureSelected, value))
                {
                    _isSystemSignatureSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSystemSignatureSelected)));
                }
            }
        }

        private bool _isEnterpriseSignatureSelected;

        public bool IsEnterpriseSignatureSelected
        {
            get { return _isEnterpriseSignatureSelected; }

            set
            {
                if (!Equals(_isEnterpriseSignatureSelected, value))
                {
                    _isEnterpriseSignatureSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnterpriseSignatureSelected)));
                }
            }
        }

        private bool _isDeveloperSignatureSelected;

        public bool IsDeveloperSignatureSelected
        {
            get { return _isDeveloperSignatureSelected; }

            set
            {
                if (!Equals(_isDeveloperSignatureSelected, value))
                {
                    _isDeveloperSignatureSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDeveloperSignatureSelected)));
                }
            }
        }

        private bool _isNoneSignatureSelected;

        public bool IsNoneSignatureSelected
        {
            get { return _isNoneSignatureSelected; }

            set
            {
                if (!Equals(_isNoneSignatureSelected, value))
                {
                    _isNoneSignatureSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNoneSignatureSelected)));
                }
            }
        }

        private string _displayName = string.Empty;

        public string DisplayName
        {
            get { return _displayName; }

            set
            {
                if (!Equals(_displayName, value))
                {
                    _displayName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }

        private string _familyName = string.Empty;

        public string FamilyName
        {
            get { return _familyName; }

            set
            {
                if (!Equals(_familyName, value))
                {
                    _familyName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FamilyName)));
                }
            }
        }

        private string _fullName = string.Empty;

        public string FullName
        {
            get { return _fullName; }

            set
            {
                if (!Equals(_fullName, value))
                {
                    _fullName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
                }
            }
        }

        private string _description = string.Empty;

        public string Description
        {
            get { return _description; }

            set
            {
                if (!Equals(_description, value))
                {
                    _description = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                }
            }
        }

        private string _publisherDisplayName = string.Empty;

        public string PublisherDisplayName
        {
            get { return _publisherDisplayName; }

            set
            {
                if (!Equals(_publisherDisplayName, value))
                {
                    _publisherDisplayName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PublisherDisplayName)));
                }
            }
        }

        private string _publisherId = string.Empty;

        public string PublisherId
        {
            get { return _publisherId; }

            set
            {
                if (!Equals(_publisherId, value))
                {
                    _publisherId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PublisherId)));
                }
            }
        }

        private string _version;

        public string Version
        {
            get { return _version; }

            set
            {
                if (!Equals(_version, value))
                {
                    _version = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
                }
            }
        }

        private string _installedDate;

        public string InstalledDate
        {
            get { return _installedDate; }

            set
            {
                if (!Equals(_installedDate, value))
                {
                    _installedDate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstalledDate)));
                }
            }
        }

        private string _architecture;

        public string Architecture
        {
            get { return _architecture; }

            set
            {
                if (!Equals(_architecture, value))
                {
                    _architecture = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Architecture)));
                }
            }
        }

        private string _signatureKind;

        private string SignatureKind
        {
            get { return _signatureKind; }

            set
            {
                if (!Equals(_signatureKind, value))
                {
                    _signatureKind = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureKind)));
                }
            }
        }

        private string _resourceId;

        public string ResourceId
        {
            get { return _resourceId; }

            set
            {
                if (!Equals(_resourceId, value))
                {
                    _resourceId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResourceId)));
                }
            }
        }

        private string _isBundle;

        public string IsBundle
        {
            get { return _isBundle; }

            set
            {
                if (!Equals(_isBundle, value))
                {
                    _isBundle = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBundle)));
                }
            }
        }

        private string _isDevelopmentMode;

        public string IsDevelopmentMode
        {
            get { return _isDevelopmentMode; }

            set
            {
                if (!Equals(_isDevelopmentMode, value))
                {
                    _isDevelopmentMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDevelopmentMode)));
                }
            }
        }

        private string _isFramework;

        public string IsFramework
        {
            get { return _isFramework; }

            set
            {
                if (!Equals(_isFramework, value))
                {
                    _isFramework = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFramework)));
                }
            }
        }

        private string _isOptional;

        public string IsOptional
        {
            get { return _isOptional; }

            set
            {
                if (!Equals(_isOptional, value))
                {
                    _isOptional = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOptional)));
                }
            }
        }

        private string _isResourcePackage;

        public string IsResourcePackage
        {
            get { return _isResourcePackage; }

            set
            {
                if (!Equals(_isResourcePackage, value))
                {
                    _isResourcePackage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsResourcePackage)));
                }
            }
        }

        private string _isStub;

        public string IsStub
        {
            get { return _isStub; }

            set
            {
                if (!Equals(_isStub, value))
                {
                    _isStub = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStub)));
                }
            }
        }

        private string _vertifyIsOK;

        public string VertifyIsOK
        {
            get { return _vertifyIsOK; }

            set
            {
                if (!Equals(_vertifyIsOK, value))
                {
                    _vertifyIsOK = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VertifyIsOK)));
                }
            }
        }

        private readonly List<Package> MatchResultList = [];

        private ObservableCollection<PackageModel> AppManagerDataCollection { get; } = [];

        private ObservableCollection<AppListEntryModel> AppListEntryCollection { get; } = [];

        private ObservableCollection<PackageModel> DependenciesCollection { get; } = [];

        public ObservableCollection<ContentLinkInfo> BreadCollection { get; } =
        [
            new ContentLinkInfo()
            {
                DisplayText = ResourceService.GetLocalized("AppManager/AppList"),
                SecondaryText = "AppList"
            }
        ];

        public event PropertyChangedEventHandler PropertyChanged;

        public AppManagerPage()
        {
            InitializeComponent();
        }

        #region 第一部分：XamlUICommand 命令调用时挂载的事件

        /// <summary>
        /// 复制应用入口的应用程序用户模型 ID
        /// </summary>
        private async void OnCopyAUMIDExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is string aumid && !string.IsNullOrEmpty(aumid))
            {
                bool copyResult = CopyPasteHelper.CopyTextToClipBoard(aumid);
                await MainWindow.Current.ShowNotificationAsync(new MainDataCopyTip(DataCopyKind.AppUserModelId, copyResult));
            }
        }

        /// <summary>
        /// 复制依赖包信息
        /// </summary>
        private async void OnCopyDependencyInformationExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                List<string> copyDependencyInformationCopyStringList = [];

                await Task.Run(() =>
                {
                    try
                    {
                        copyDependencyInformationCopyStringList.Add(package.DisplayName);
                        copyDependencyInformationCopyStringList.Add(package.Id.FamilyName);
                        copyDependencyInformationCopyStringList.Add(package.Id.FullName);
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, "App information copy failed", e);
                    }
                });

                bool copyResult = CopyPasteHelper.CopyTextToClipBoard(string.Join(Environment.NewLine, copyDependencyInformationCopyStringList));
                await MainWindow.Current.ShowNotificationAsync(new MainDataCopyTip(DataCopyKind.DependencyInformation, copyResult));
            }
        }

        /// <summary>
        /// 复制依赖包名称
        /// </summary>
        private async void OnCopyDependencyNameExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is string displayName && !string.IsNullOrEmpty(displayName))
            {
                bool copyResult = CopyPasteHelper.CopyTextToClipBoard(displayName);
                await MainWindow.Current.ShowNotificationAsync(new MainDataCopyTip(DataCopyKind.DependencyName, copyResult));
            }
        }

        /// <summary>
        /// 启动对应入口的应用
        /// </summary>
        private void OnLaunchExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is AppListEntryModel appListEntryItem)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await appListEntryItem.AppListEntry.LaunchAsync();
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, string.Format("Open app {0} failed", appListEntryItem.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开应用
        /// </summary>
        private void OnOpenAppExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await package.GetAppListEntries()[0].LaunchAsync();
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, string.Format("Open app {0} failed", package.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开应用缓存目录
        /// </summary>
        private void OnOpenCacheFolderExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (ApplicationDataManager.CreateForPackageFamily(package.Id.FamilyName) is ApplicationData applicationData)
                        {
                            await Launcher.LaunchFolderAsync(applicationData.LocalFolder);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Information, "Open app cache folder failed.", e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开安装目录
        /// </summary>
        private void OnOpenFolderExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Launcher.LaunchFolderPathAsync(package.InstalledPath);
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, string.Format("{0} app installed folder open failed", package.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开应用安装目录
        /// </summary>
        private void OnOpenInstalledFolderExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Launcher.LaunchFolderPathAsync(package.InstalledPath);
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Warning, string.Format("{0} app installed folder open failed", package.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开应用清单文件
        /// </summary>
        private void OnOpenManifestExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (await StorageFile.GetFileFromPathAsync(Path.Combine(package.InstalledPath, "AppxManifest.xml")) is StorageFile file)
                        {
                            await Launcher.LaunchFileAsync(file);
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, string.Format("{0}'s AppxManifest.xml file open failed", package.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 打开商店
        /// </summary>
        private void OnOpenStoreExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?PFN={package.Id.FamilyName}"));
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, string.Format("Open microsoft store {0} failed", package.DisplayName), e);
                    }
                });
            }
        }

        /// <summary>
        /// 固定应用到桌面
        /// </summary>
        private async void OnPinToDesktopExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            bool isPinnedSuccessfully = false;

            await Task.Run(() =>
            {
                try
                {
                    if (StoreConfiguration.IsPinToDesktopSupported())
                    {
                        StoreConfiguration.PinToDesktop(FamilyName);
                        isPinnedSuccessfully = true;
                    }
                }
                catch (Exception e)
                {
                    LogService.WriteLog(LoggingLevel.Error, "Create desktop shortcut failed.", e);
                }
            });

            await MainWindow.Current.ShowNotificationAsync(new QuickOperationTip(QuickOperationKind.Desktop, isPinnedSuccessfully));
        }

        /// <summary>
        /// 固定应用入口到开始“屏幕”
        /// </summary>
        private async void OnPinToStartScreenExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is AppListEntryModel appListEntryItem)
            {
                bool isPinnedSuccessfully = false;

                await Task.Run(async () =>
                {
                    try
                    {
                        StartScreenManager startScreenManager = StartScreenManager.GetDefault();

                        isPinnedSuccessfully = await startScreenManager.RequestAddAppListEntryAsync(appListEntryItem.AppListEntry);
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, "Pin app to startscreen failed.", e);
                    }
                });

                await MainWindow.Current.ShowNotificationAsync(new QuickOperationTip(QuickOperationKind.StartScreen, isPinnedSuccessfully));
            }
        }

        /// <summary>
        /// 固定应用入口到任务栏
        /// </summary>
        private void OnPinToTaskbarExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is AppListEntryModel appListEntryItem)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri("getstoreapppinner:"), new LauncherOptions() { TargetApplicationPackageFamilyName = Package.Current.Id.FamilyName }, new ValueSet()
                        {
                            {"Type", nameof(TaskbarManager) },
                            { "AppUserModelId", appListEntryItem.AppUserModelId },
                            { "PackageFullName", appListEntryItem.PackageFullName },
                        });
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, "Use TaskbarManager api to pin app to taskbar failed.", e);
                    }
                });
            }
        }

        /// <summary>
        /// 更多按钮点击时显示菜单
        /// </summary>
        private void OnShowMoreExecuteRequested(object sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is HyperlinkButton hyperlinkButton)
            {
                FlyoutBase.ShowAttachedFlyout(hyperlinkButton);
            }
        }

        /// <summary>
        /// 卸载应用
        /// </summary>
        private void OnUnInstallExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Package package)
            {
                foreach (PackageModel packageItem in AppManagerDataCollection)
                {
                    if (packageItem.Package.Id.FullName == package.Id.FullName)
                    {
                        packageItem.IsUnInstalling = true;
                        break;
                    }
                }

                try
                {
                    Task.Run(() =>
                    {
                        IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> uninstallProgress = packageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.None);

                        uninstallProgress.Completed = (result, progress) => OnUninstallCompleted(result, progress, package);
                    });
                }
                catch (Exception e)
                {
                    LogService.WriteLog(LoggingLevel.Information, string.Format("UnInstall app {0} failed", package.Id.FullName), e);
                }
            }
        }

        /// <summary>
        /// 查看应用信息
        /// </summary>
        private async void OnViewInformationExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is PackageModel packageItem)
            {
                AppInformation appInformation = new();

                await Task.Run(() =>
                {
                    appInformation.DisplayName = packageItem.DisplayName;

                    try
                    {
                        appInformation.PackageFamilyName = string.IsNullOrEmpty(packageItem.Package.Id.FamilyName) ? Unknown : packageItem.Package.Id.FamilyName;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.PackageFamilyName = Unknown;
                    }

                    try
                    {
                        appInformation.PackageFullName = string.IsNullOrEmpty(packageItem.Package.Id.FullName) ? Unknown : packageItem.Package.Id.FullName;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.PackageFullName = Unknown;
                    }

                    try
                    {
                        appInformation.Description = string.IsNullOrEmpty(packageItem.Package.Description) ? Unknown : packageItem.Package.Description;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.Description = Unknown;
                    }

                    appInformation.PublisherDisplayName = packageItem.PublisherDisplayName;

                    try
                    {
                        appInformation.PublisherId = string.IsNullOrEmpty(packageItem.Package.Id.PublisherId) ? Unknown : packageItem.Package.Id.PublisherId;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.PublisherId = Unknown;
                    }

                    appInformation.Version = packageItem.Version;
                    appInformation.InstallDate = packageItem.InstallDate;

                    try
                    {
                        appInformation.Architecture = string.IsNullOrEmpty(packageItem.Package.Id.Architecture.ToString()) ? Unknown : packageItem.Package.Id.Architecture.ToString();
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.Architecture = Unknown;
                    }

                    appInformation.SignatureKind = ResourceService.GetLocalized(string.Format("AppManager/Signature{0}", packageItem.SignatureKind.ToString()));

                    try
                    {
                        appInformation.ResourceId = string.IsNullOrEmpty(packageItem.Package.Id.ResourceId) ? Unknown : packageItem.Package.Id.ResourceId;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.ResourceId = Unknown;
                    }

                    try
                    {
                        appInformation.IsBundle = packageItem.Package.IsBundle ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.IsBundle = Unknown;
                    }

                    try
                    {
                        appInformation.IsDevelopmentMode = packageItem.Package.IsDevelopmentMode ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.IsDevelopmentMode = Unknown;
                    }

                    appInformation.IsFramework = packageItem.IsFramework ? Yes : No;

                    try
                    {
                        appInformation.IsOptional = packageItem.Package.IsOptional ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.IsOptional = Unknown;
                    }

                    try
                    {
                        appInformation.IsResourcePackage = packageItem.Package.IsResourcePackage ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.IsResourcePackage = Unknown;
                    }

                    try
                    {
                        appInformation.IsStub = packageItem.Package.IsStub ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.IsStub = Unknown;
                    }

                    try
                    {
                        appInformation.VertifyIsOK = packageItem.Package.Status.VerifyIsOK() ? Yes : No;
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                        appInformation.VertifyIsOK = Unknown;
                    }

                    try
                    {
                        IReadOnlyList<AppListEntry> appListEntriesList = packageItem.Package.GetAppListEntries();
                        for (int index = 0; index < appListEntriesList.Count; index++)
                        {
                            appInformation.AppListEntryList.Add(new AppListEntryModel()
                            {
                                DisplayName = appListEntriesList[index].DisplayInfo.DisplayName,
                                Description = appListEntriesList[index].DisplayInfo.Description,
                                AppUserModelId = appListEntriesList[index].AppUserModelId,
                                AppListEntry = appListEntriesList[index],
                                PackageFullName = packageItem.Package.Id.FullName
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                    }

                    try
                    {
                        IReadOnlyList<Package> dependcies = packageItem.Package.Dependencies;

                        if (dependcies.Count > 0)
                        {
                            for (int index = 0; index < dependcies.Count; index++)
                            {
                                try
                                {
                                    appInformation.DependenciesList.Add(new PackageModel()
                                    {
                                        DisplayName = dependcies[index].DisplayName,
                                        PublisherDisplayName = dependcies[index].PublisherDisplayName,
                                        Version = new Version(dependcies[index].Id.Version.Major, dependcies[index].Id.Version.Minor, dependcies[index].Id.Version.Build, dependcies[index].Id.Version.Revision).ToString(),
                                        Package = dependcies[index]
                                    });
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }

                        appInformation.DependenciesList.Sort((item1, item2) => item1.DisplayName.CompareTo(item2.DisplayName));
                    }
                    catch (Exception e)
                    {
                        ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                    }
                });

                InitializeAppInfo(appInformation);
                BreadCollection.Add(new ContentLinkInfo()
                {
                    DisplayText = ResourceService.GetLocalized("AppManager/AppInformation"),
                    SecondaryText = "AppInformation"
                });
            }
        }

        #endregion 第一部分：XamlUICommand 命令调用时挂载的事件

        #region 第二部分：应用管理页面——挂载的事件

        /// <summary>
        /// 应用管理页面初始化完成后触发的事件
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                await GetInstalledAppsAsync();
                await InitializeDataAsync();
            }
        }

        /// <summary>
        /// 打开设置中的安装的应用
        /// </summary>
        private async void OnInstalledAppsClicked(object sender, RoutedEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures"));
        }

        /// <summary>
        /// 单击痕迹栏条目时发生的事件
        /// </summary>
        private void OnItemClicked(object sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ContentLinkInfo breadItem = args.Item as ContentLinkInfo;
            if (BreadCollection.Count is 2)
            {
                if (breadItem.SecondaryText.Equals(BreadCollection[0].SecondaryText))
                {
                    BackToAppList();
                }
            }
        }

        /// <summary>
        /// 根据输入的内容检索应用
        /// </summary>
        private async void OnQuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                await InitializeDataAsync(true);
            }
        }

        /// <summary>
        /// 文本输入框内容为空时，复原原来的内容
        /// </summary>
        private async void OnTextChanged(object sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                SearchText = autoSuggestBox.Text;
                if (string.IsNullOrEmpty(SearchText))
                {
                    await InitializeDataAsync();
                }
            }
        }

        /// <summary>
        /// 根据排序方式对列表进行排序
        /// </summary>
        private async void OnSortWayClicked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioMenuFlyoutItem radioMenuFlyoutItem && radioMenuFlyoutItem.Tag is not null)
            {
                IsIncrease = Convert.ToBoolean(radioMenuFlyoutItem.Tag);
                await InitializeDataAsync();
            }
        }

        /// <summary>
        /// 根据排序规则对列表进行排序
        /// </summary>
        private async void OnSortRuleClicked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioMenuFlyoutItem radioMenuFlyoutItem && radioMenuFlyoutItem.Tag is not null)
            {
                SelectedRule = (AppSortRuleKind)radioMenuFlyoutItem.Tag;
                await InitializeDataAsync();
            }
        }

        /// <summary>
        /// 根据过滤方式对列表进行过滤
        /// </summary>
        private void OnFilterWayClicked(object sender, RoutedEventArgs args)
        {
            IsAppFramework = !IsAppFramework;
            needToRefreshData = true;
        }

        /// <summary>
        /// 根据签名规则进行过滤
        /// </summary>
        private void OnSignatureRuleClicked(object sender, RoutedEventArgs args)
        {
            if (sender is ToggleButton toggleButton && toggleButton.Tag is not null)
            {
                PackageSignatureKind signatureKind = (PackageSignatureKind)toggleButton.Tag;

                if (signatureKind is PackageSignatureKind.Store)
                {
                    IsStoreSignatureSelected = !IsStoreSignatureSelected;
                }
                else if (signatureKind is PackageSignatureKind.System)
                {
                    IsSystemSignatureSelected = !IsSystemSignatureSelected;
                }
                else if (signatureKind is PackageSignatureKind.Enterprise)
                {
                    IsEnterpriseSignatureSelected = !IsEnterpriseSignatureSelected;
                }
                else if (signatureKind is PackageSignatureKind.Developer)
                {
                    IsDeveloperSignatureSelected = !IsDeveloperSignatureSelected;
                }
                else if (signatureKind is PackageSignatureKind.None)
                {
                    IsNoneSignatureSelected = !IsNoneSignatureSelected;
                }

                needToRefreshData = true;
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async void OnRefreshClicked(object sender, RoutedEventArgs args)
        {
            MatchResultList.Clear();
            IsLoadedCompleted = false;
            SearchText = string.Empty;
            await GetInstalledAppsAsync();
            await InitializeDataAsync();
        }

        /// <summary>
        /// 浮出菜单关闭后更新数据
        /// </summary>
        private async void OnClosed(object sender, object args)
        {
            if (needToRefreshData)
            {
                await InitializeDataAsync();
            }

            needToRefreshData = false;
        }

        /// <summary>
        /// 复制应用信息
        /// </summary>
        private async void OnCopyClicked(object sender, RoutedEventArgs args)
        {
            List<string> copyStringList = [];
            await Task.Run(() =>
            {
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/DisplayName"), DisplayName));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/FamilyName"), FamilyName));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/FullName"), FullName));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/Description"), Description));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/PublisherDisplayName"), PublisherDisplayName));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/PublisherId"), PublisherId));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/Version"), Version));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/InstalledDate"), InstalledDate));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/Architecture"), Architecture));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/SignatureKind"), SignatureKind));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/ResourceId"), ResourceId));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsBundle"), IsBundle));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsDevelopmentMode"), IsDevelopmentMode));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsFramework"), IsFramework));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsOptional"), IsOptional));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsResourcePackage"), IsResourcePackage));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/IsStub"), IsStub));
                copyStringList.Add(string.Format("{0}:\t{1}", ResourceService.GetLocalized("AppManager/VertifyIsOK"), VertifyIsOK));
            });

            bool copyResult = CopyPasteHelper.CopyTextToClipBoard(string.Join(Environment.NewLine, copyStringList));
            await MainWindow.Current.ShowNotificationAsync(new MainDataCopyTip(DataCopyKind.PackageInformation, copyResult));
        }

        #endregion 第二部分：应用管理页面——挂载的事件

        #region 第三部分：应用管理页面——自定义事件

        private void OnUninstallCompleted(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> result, AsyncStatus status, Package package)
        {
            // 卸载成功
            if (result.Status is AsyncStatus.Completed)
            {
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    foreach (PackageModel pacakgeItem in AppManagerDataCollection)
                    {
                        if (pacakgeItem.Package.Id.FullName == package.Id.FullName)
                        {
                            // 显示 UWP 应用卸载成功通知
                            AppNotificationBuilder appNotificationBuilder = new();
                            appNotificationBuilder.AddArgument("action", "OpenApp");
                            appNotificationBuilder.AddText(string.Format(ResourceService.GetLocalized("Notification/UWPUnInstallSuccessfully"), pacakgeItem.Package.DisplayName));
                            ToastNotificationService.Show(appNotificationBuilder.BuildNotification());

                            AppManagerDataCollection.Remove(pacakgeItem);
                            break;
                        }
                    }
                });
            }

            // 卸载失败
            else if (result.Status is AsyncStatus.Error)
            {
                DeploymentResult uninstallResult = result.GetResults();

                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    foreach (PackageModel pacakgeItem in AppManagerDataCollection)
                    {
                        if (pacakgeItem.Package.Id.FullName == package.Id.FullName)
                        {
                            // 显示 UWP 应用卸载失败通知
                            AppNotificationBuilder appNotificationBuilder = new();
                            appNotificationBuilder.AddArgument("action", "OpenApp");
                            appNotificationBuilder.AddText(string.Format(ResourceService.GetLocalized("Notification/UWPUnInstallFailed1"), pacakgeItem.Package.DisplayName));
                            appNotificationBuilder.AddText(ResourceService.GetLocalized("Notification/UWPUnInstallFailed2"));

                            appNotificationBuilder.AddText(string.Join(Environment.NewLine, new string[]
                            {
                                ResourceService.GetLocalized("Notification/UWPUnInstallFailed3"),
                                string.Format(ResourceService.GetLocalized("Notification/UWPUnInstallFailed4"), uninstallResult.ExtendedErrorCode is not null ? uninstallResult.ExtendedErrorCode.HResult : Unknown),
                                string.Format(ResourceService.GetLocalized("Notification/UWPUnInstallFailed5"), uninstallResult.ErrorText)
                            }));
                            AppNotificationButton openSettingsButton = new(ResourceService.GetLocalized("Notification/OpenSettings"));
                            openSettingsButton.Arguments.Add("action", "OpenSettings");
                            appNotificationBuilder.AddButton(openSettingsButton);
                            ToastNotificationService.Show(appNotificationBuilder.BuildNotification());
                            LogService.WriteLog(LoggingLevel.Information, string.Format("UnInstall app {0} failed", pacakgeItem.Package.DisplayName), uninstallResult.ExtendedErrorCode is not null ? uninstallResult.ExtendedErrorCode : new Exception());
                            pacakgeItem.IsUnInstalling = false;
                            break;
                        }
                    }
                });
            }

            result.Close();
        }

        #endregion 第三部分：应用管理页面——自定义事件

        /// <summary>
        /// 返回到应用信息页面
        /// </summary>
        public void BackToAppList()
        {
            if (BreadCollection.Count is 2)
            {
                BreadCollection.RemoveAt(1);
            }
        }

        /// <summary>
        /// 加载系统已安装的应用信息
        /// </summary>
        private async Task GetInstalledAppsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    IEnumerable<Package> findResultList = packageManager.FindPackagesForUser(string.Empty);
                    foreach (Package packageItem in findResultList)
                    {
                        MatchResultList.Add(packageItem);
                    }
                }
                catch (Exception e)
                {
                    LogService.WriteLog(LoggingLevel.Error, "Find current user packages failed", e);
                }
            });

            IsPackageEmpty = MatchResultList.Count is 0;
        }

        /// <summary>
        /// 初始化列表数据
        /// </summary>
        private async Task InitializeDataAsync(bool hasSearchText = false)
        {
            IsLoadedCompleted = false;
            AppManagerDataCollection.Clear();

            if (MatchResultList.Count > 0)
            {
                List<PackageModel> packageList = [];
                await Task.Run(() =>
                {
                    // 备份数据
                    List<Package> backupList = MatchResultList;
                    List<Package> appTypesList = [];

                    try
                    {
                        // 根据选项是否筛选包含框架包的数据
                        if (IsAppFramework)
                        {
                            foreach (Package packageItem in backupList)
                            {
                                if (packageItem.IsFramework == IsAppFramework)
                                {
                                    appTypesList.Add(packageItem);
                                }
                            }
                        }
                        else
                        {
                            appTypesList = backupList;
                        }

                        List<Package> filteredList = [];
                        foreach (Package packageItem in appTypesList)
                        {
                            if (packageItem.SignatureKind.Equals(PackageSignatureKind.Store) && IsStoreSignatureSelected)
                            {
                                filteredList.Add(packageItem);
                            }
                            else if (packageItem.SignatureKind.Equals(PackageSignatureKind.System) && IsSystemSignatureSelected)
                            {
                                filteredList.Add(packageItem);
                            }
                            else if (packageItem.SignatureKind.Equals(PackageSignatureKind.Enterprise) && IsEnterpriseSignatureSelected)
                            {
                                filteredList.Add(packageItem);
                            }
                            else if (packageItem.SignatureKind.Equals(PackageSignatureKind.Developer) && IsDeveloperSignatureSelected)
                            {
                                filteredList.Add(packageItem);
                            }
                            else if (packageItem.SignatureKind.Equals(PackageSignatureKind.None) && IsNoneSignatureSelected)
                            {
                                filteredList.Add(packageItem);
                            }
                        }

                        // 对过滤后的列表数据进行排序
                        switch (SelectedRule)
                        {
                            case AppSortRuleKind.DisplayName:
                                {
                                    if (IsIncrease)
                                    {
                                        filteredList.Sort((item1, item2) => item1.DisplayName.CompareTo(item2.DisplayName));
                                    }
                                    else
                                    {
                                        filteredList.Sort((item1, item2) => item2.DisplayName.CompareTo(item1.DisplayName));
                                    }
                                    break;
                                }
                            case AppSortRuleKind.PublisherName:
                                {
                                    if (IsIncrease)
                                    {
                                        filteredList.Sort((item1, item2) => item1.PublisherDisplayName.CompareTo(item2.PublisherDisplayName));
                                    }
                                    else
                                    {
                                        filteredList.Sort((item1, item2) => item2.PublisherDisplayName.CompareTo(item1.PublisherDisplayName));
                                    }
                                    break;
                                }
                            case AppSortRuleKind.InstallDate:
                                {
                                    if (IsIncrease)
                                    {
                                        filteredList.Sort((item1, item2) => item1.InstalledDate.CompareTo(item2.InstalledDate));
                                    }
                                    else
                                    {
                                        filteredList.Sort((item1, item2) => item2.InstalledDate.CompareTo(item1.InstalledDate));
                                    }
                                    break;
                                }
                        }

                        // 根据搜索条件对搜索符合要求的数据
                        if (hasSearchText)
                        {
                            for (int index = filteredList.Count - 1; index >= 0; index--)
                            {
                                if (!(filteredList[index].DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || filteredList[index].Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || filteredList[index].PublisherDisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                                {
                                    filteredList.RemoveAt(index);
                                }
                            }
                        }

                        foreach (Package packageItem in filteredList)
                        {
                            packageList.Add(new PackageModel()
                            {
                                IsFramework = GetIsFramework(packageItem),
                                AppListEntryCount = GetAppListEntriesCount(packageItem),
                                DisplayName = GetDisplayName(packageItem),
                                InstallDate = GetInstallDate(packageItem),
                                PublisherDisplayName = GetPublisherDisplayName(packageItem),
                                Version = GetVersion(packageItem),
                                SignatureKind = GetSignatureKind(packageItem),
                                InstalledDate = GetInstalledDate(packageItem),
                                Package = packageItem,
                                IsUnInstalling = false
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        LogService.WriteLog(LoggingLevel.Error, "Get local app data failed", e);
                    }
                });

                foreach (PackageModel packageItem in packageList)
                {
                    AppManagerDataCollection.Add(packageItem);
                }
            }

            IsLoadedCompleted = true;
        }

        /// <summary>
        /// 初始化应用信息
        /// </summary>
        private void InitializeAppInfo(AppInformation appInformation)
        {
            DisplayName = appInformation.DisplayName;
            FamilyName = appInformation.PackageFamilyName;
            FullName = appInformation.PackageFullName;
            Description = appInformation.Description;
            PublisherDisplayName = appInformation.PublisherDisplayName;
            PublisherId = appInformation.PublisherId;
            Version = appInformation.Version;
            InstalledDate = appInformation.InstallDate;
            Architecture = appInformation.Architecture;
            SignatureKind = appInformation.SignatureKind;
            ResourceId = appInformation.ResourceId;
            IsBundle = appInformation.IsBundle;
            IsDevelopmentMode = appInformation.IsDevelopmentMode;
            IsFramework = appInformation.IsFramework;
            IsOptional = appInformation.IsOptional;
            IsResourcePackage = appInformation.IsResourcePackage;
            IsStub = appInformation.IsStub;
            VertifyIsOK = appInformation.VertifyIsOK;

            AppListEntryCollection.Clear();
            foreach (AppListEntryModel appListEntry in appInformation.AppListEntryList)
            {
                AppListEntryCollection.Add(appListEntry);
            }

            DependenciesCollection.Clear();
            foreach (PackageModel packageItem in appInformation.DependenciesList)
            {
                DependenciesCollection.Add(packageItem);
            }
        }

        /// <summary>
        /// 获取应用包是否为框架包
        /// </summary>
        private static bool GetIsFramework(Package package)
        {
            try
            {
                return package.IsFramework;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return false;
            }
        }

        /// <summary>
        /// 获取应用包的入口数
        /// </summary>
        private static int GetAppListEntriesCount(Package package)
        {
            try
            {
                return package.GetAppListEntries().Count;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return 0;
            }
        }

        /// <summary>
        /// 获取应用的显示名称
        /// </summary>
        private string GetDisplayName(Package package)
        {
            try
            {
                return string.IsNullOrEmpty(package.DisplayName) ? Unknown : package.DisplayName;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return Unknown;
            }
        }

        /// <summary>
        /// 获取应用的发布者显示名称
        /// </summary>
        private string GetPublisherDisplayName(Package package)
        {
            try
            {
                return string.IsNullOrEmpty(package.PublisherDisplayName) ? Unknown : package.PublisherDisplayName;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return Unknown;
            }
        }

        /// <summary>
        /// 获取应用的版本信息
        /// </summary>
        private static string GetVersion(Package package)
        {
            try
            {
                return new Version(package.Id.Version.Major, package.Id.Version.Minor, package.Id.Version.Build, package.Id.Version.Revision).ToString();
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return new Version().ToString();
            }
        }

        /// <summary>
        /// 获取应用的安装日期
        /// </summary>
        private static string GetInstallDate(Package package)
        {
            try
            {
                return package.InstalledDate.ToString("yyyy/MM/dd HH:mm");
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return DateTimeOffset.FromUnixTimeSeconds(0).ToString("yyyy/MM/dd HH:mm");
            }
        }

        /// <summary>
        /// 获取应用包签名方式
        /// </summary>
        private static PackageSignatureKind GetSignatureKind(Package package)
        {
            try
            {
                return package.SignatureKind;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return PackageSignatureKind.None;
            }
        }

        /// <summary>
        /// 获取应用包安装日期
        /// </summary>
        private static DateTimeOffset GetInstalledDate(Package package)
        {
            try
            {
                return package.InstalledDate;
            }
            catch (Exception e)
            {
                ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
                return DateTimeOffset.FromUnixTimeSeconds(0);
            }
        }
    }
}
