﻿using GetStoreApp.Helpers;
using GetStoreApp.ViewModels.Pages;
using Microsoft.UI.Xaml.Controls;

namespace GetStoreApp.Views
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; } = IOCHelper.GetService<HomeViewModel>();

        public HomePage()
        {
            InitializeComponent();
        }
    }
}