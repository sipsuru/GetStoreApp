﻿using GetStoreApp.Extensions.Backdrop;
using GetStoreApp.Services.Root;
using Microsoft.UI.Composition;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Numerics;
using Windows.Foundation.Diagnostics;

namespace GetStoreApp.Extensions.Behaviors
{
    /// <summary>
    /// 命令栏二级菜单浮出控件背景色行为类
    /// </summary>
    public class CommandBarSecondaryCommandsBackdropBehavior : DependencyObject, IBehavior
    {
        private bool isLoaded;
        private bool isConnected;
        private Grid visualGrid;
        private Grid secondaryItemsControlShadowWrapperGrid;
        private Popup secondaryCommandsPopup;
        private ContentExternalBackdropLink contentExternalBackdropLink;

        public DependencyObject AssociatedObject { get; private set; }

        /// <summary>
        /// 附加到指定的对象
        /// </summary>
        public void Attach(DependencyObject associatedObject)
        {
            if (!Equals(AssociatedObject, associatedObject) && AssociatedObject is null)
            {
                AssociatedObject = associatedObject;

                if (AssociatedObject is CommandBar commandBar)
                {
                    commandBar.Loaded += OnLoaded;
                }
            }
        }

        /// <summary>
        /// 将此实例与其关联对象分离
        /// </summary>
        public void Detach()
        {
            try
            {
                if (AssociatedObject is not null && AssociatedObject is CommandBar commandBar)
                {
                    commandBar.Loaded -= OnLoaded;

                    if (secondaryCommandsPopup is not null)
                    {
                        secondaryCommandsPopup.Opened -= OnOpened;
                    }

                    if (secondaryItemsControlShadowWrapperGrid is not null && isConnected)
                    {
                        secondaryItemsControlShadowWrapperGrid.SizeChanged -= OnSizeChanged;
                    }

                    AssociatedObject = null;
                    secondaryCommandsPopup = null;
                    contentExternalBackdropLink = null;
                    isConnected = false;
                    isLoaded = false;
                }
            }
            catch (Exception e)
            {
                LogService.WriteLog(LoggingLevel.Error, nameof(GetStoreApp), nameof(CommandBarSecondaryCommandsBackdropBehavior), nameof(Detach), 1, e);
            }
        }

        /// <summary>
        /// 命令栏初始化完成触发的事件
        /// </summary>
        public void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (!isLoaded && sender is CommandBar commandBar && FindDescendant<Popup>(commandBar, "OverflowPopup") is Popup popup && secondaryCommandsPopup is null)
            {
                isLoaded = true;
                secondaryCommandsPopup = popup;
                secondaryCommandsPopup.Opened += OnOpened;
            }
        }

        /// <summary>
        /// 弹出窗口打开后触发的事件
        /// </summary>
        private void OnOpened(object sender, object args)
        {
            if (secondaryCommandsPopup is not null && secondaryCommandsPopup.FindName("SecondaryItemsControlShadowWrapper") is Grid grid)
            {
                secondaryItemsControlShadowWrapperGrid = grid;
            }

            if (secondaryItemsControlShadowWrapperGrid is not null && !ControlBackdropController.IsClosed)
            {
                if (!isConnected)
                {
                    visualGrid = new();
                    Canvas.SetZIndex(visualGrid, -9999);
                    secondaryItemsControlShadowWrapperGrid.Children.Add(visualGrid);
                    secondaryItemsControlShadowWrapperGrid.SizeChanged += OnSizeChanged;
                    contentExternalBackdropLink = ControlBackdropController.CreateContentExternalBackdropLink();

                    if (contentExternalBackdropLink is not null)
                    {
                        contentExternalBackdropLink.ExternalBackdropBorderMode = CompositionBorderMode.Soft;
                        contentExternalBackdropLink.PlacementVisual.Size = secondaryItemsControlShadowWrapperGrid.ActualSize;
                        contentExternalBackdropLink.PlacementVisual.Clip = contentExternalBackdropLink.PlacementVisual.Compositor.CreateRectangleClip(0, 0, secondaryItemsControlShadowWrapperGrid.ActualSize.X, secondaryItemsControlShadowWrapperGrid.ActualSize.Y,
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopLeft), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopLeft)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopRight), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopRight)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomRight), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomRight)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomLeft), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomLeft)));
                        ElementCompositionPreview.SetElementChildVisual(visualGrid, contentExternalBackdropLink.PlacementVisual);
                    }
                }
                else
                {
                    if (contentExternalBackdropLink is not null)
                    {
                        contentExternalBackdropLink.PlacementVisual.Size = secondaryItemsControlShadowWrapperGrid.ActualSize;
                        contentExternalBackdropLink.PlacementVisual.Clip = contentExternalBackdropLink.PlacementVisual.Compositor.CreateRectangleClip(0, 0, secondaryItemsControlShadowWrapperGrid.ActualSize.X, secondaryItemsControlShadowWrapperGrid.ActualSize.Y,
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopLeft), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopLeft)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopRight), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.TopRight)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomRight), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomRight)),
                            new Vector2(Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomLeft), Convert.ToSingle(secondaryItemsControlShadowWrapperGrid.CornerRadius.BottomLeft)));
                    }
                }
            }
        }

        /// <summary>
        /// 目标控件大小发生变化时触发的事件
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (contentExternalBackdropLink is not null)
            {
                contentExternalBackdropLink.PlacementVisual.Size = secondaryItemsControlShadowWrapperGrid.ActualSize;
            }
        }

        /// <summary>
        /// 查找 CommandBar 的 OverflowPopup 子控件
        /// </summary>
        private static T FindDescendant<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent is not null)
            {
                for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, index);

                    if (child is T result && (childName is null || child is FrameworkElement frameworkElement && string.Equals(frameworkElement.Name, childName)))
                    {
                        return result;
                    }

                    T foundChild = FindDescendant<T>(child, childName);
                    if (foundChild is not null)
                    {
                        return foundChild;
                    }
                }
            }

            return null;
        }
    }
}
