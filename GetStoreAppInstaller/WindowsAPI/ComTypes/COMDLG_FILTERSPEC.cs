﻿using System.Runtime.InteropServices;

namespace GetStoreAppInstaller.WindowsAPI.ComTypes
{
    /// <summary>
    /// 一般用于筛选元素。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct COMDLG_FILTERSPEC
    {
        /// <summary>
        /// 指向包含筛选器友好名称的缓冲区的指针。
        /// </summary>
        public char* pszName;

        /// <summary>
        /// 指向包含筛选器模式的缓冲区的指针。
        /// </summary>
        public char* pszSpec;
    }
}