﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GetStoreApp.Contracts.Services.App;
using GetStoreApp.Contracts.Services.History;
using GetStoreApp.Contracts.Services.Settings;
using GetStoreApp.Contracts.Services.Shell;
using GetStoreApp.Helpers;
using GetStoreApp.Messages;
using GetStoreApp.Models;
using GetStoreApp.ViewModels.Pages;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GetStoreApp.ViewModels.Controls.Home
{
    public class HistoryLiteViewModel : ObservableRecipient
    {
        // 临界区资源访问互斥锁
        private readonly object HistoryLiteDataListLock = new object();

        private HistoryLiteNumModel HistoryLiteItem { get; set; }

        private IResourceService ResourceService { get; } = IOCHelper.GetService<IResourceService>();

        private IHistoryDBService HistoryDBService { get; } = IOCHelper.GetService<IHistoryDBService>();

        private IHistoryLiteNumService HistoryLiteNumService { get; } = IOCHelper.GetService<IHistoryLiteNumService>();

        private INavigationService NavigationService { get; } = IOCHelper.GetService<INavigationService>();

        public List<GetAppTypeModel> TypeList => ResourceService.TypeList;

        public List<GetAppChannelModel> ChannelList => ResourceService.ChannelList;

        public ObservableCollection<HistoryModel> HistoryLiteDataList { get; } = new ObservableCollection<HistoryModel>();

        // List列表初始化，可以从数据库获得的列表中加载
        public IAsyncRelayCommand LoadedCommand => new AsyncRelayCommand(GetHistoryLiteDataListAsync);

        public IAsyncRelayCommand ViewAllCommand => new AsyncRelayCommand(async () =>
        {
            NavigationService.NavigateTo(typeof(HistoryViewModel).FullName, null, new DrillInNavigationTransitionInfo());
            await Task.CompletedTask;
        });

        public IAsyncRelayCommand CopyCommand => new AsyncRelayCommand<HistoryModel>(async (param) =>
        {
            string CopyContent = string.Format("{0}\t{1}\t{2}",
                TypeList.Find(item => item.InternalName.Equals(param.HistoryType)).DisplayName,
                ChannelList.Find(item => item.InternalName.Equals(param.HistoryChannel)).DisplayName,
                param.HistoryLink);
            CopyPasteHelper.CopyToClipBoard(CopyContent);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage("HistoryCopySuccessfully"));
            await Task.CompletedTask;
        });

        public IAsyncRelayCommand FillinCommand => new AsyncRelayCommand<HistoryModel>(async (param) =>
        {
            WeakReferenceMessenger.Default.Send(new FillinMessage(param));
            await Task.CompletedTask;
        });

        public HistoryLiteViewModel()
        {
            HistoryLiteItem = HistoryLiteNumService.HistoryLiteNum;

            WeakReferenceMessenger.Default.Register<HistoryLiteViewModel, HistoryMessage>(this, async (historyLiteViewModel, historyMessage) =>
            {
                if (historyMessage.Value)
                {
                    await GetHistoryLiteDataListAsync();
                }
            });

            WeakReferenceMessenger.Default.Register<HistoryLiteViewModel, HistoryLiteNumMessage>(this, async (historyLiteViewModel, historyLiteNumMessage) =>
            {
                HistoryLiteItem = historyLiteNumMessage.Value;
                await GetHistoryLiteDataListAsync();
            });
        }

        /// <summary>
        /// UI加载完成时/或者是数据库数据发生变化时，从数据库中异步加载数据
        /// </summary>
        private async Task GetHistoryLiteDataListAsync()
        {
            // 获取数据库的原始记录数据
            List<HistoryModel> HistoryRawList = await HistoryDBService.QueryAsync(HistoryLiteItem.HistoryLiteNumValue);

            lock (HistoryLiteDataListLock)
            {
                HistoryLiteDataList.Clear();
            }

            lock (HistoryLiteDataListLock)
            {
                foreach (HistoryModel historyRawData in HistoryRawList)
                {
                    HistoryLiteDataList.Add(historyRawData);
                }
            }
        }
    }
}