﻿using GetStoreApp.Contracts.Services.App;
using GetStoreApp.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.UI.Notifications
{
    public sealed partial class ResultCopyNotification : UserControl
    {
        public IResourceService ResourceService { get; } = IOCHelper.GetService<IResourceService>();

        public ResultCopyNotification()
        {
            this.InitializeComponent();
        }
    }
}