﻿using ABI.Microsoft.UI.Composition;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

// 抑制 IDE0130 警告
#pragma warning disable IDE0130

namespace ABI.Microsoft.UI.Content
{
    public static class IContentExternalBackdropLinkStaticsMethods
    {
        public static ref readonly Guid IID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.AsRef<Guid>([0xFB, 0xC6, 0xCA, 0x46, 0x51, 0xBB, 0xA, 0x51, 0x95, 0x8D, 0xE0, 0xEB, 0x41, 0x60, 0xF6, 0x78]);
        }

        public static unsafe global::Microsoft.UI.Content.ContentExternalBackdropLink Create(IObjectReference obj, global::Microsoft.UI.Composition.Compositor compositor)
        {
            nint thisPtr = obj.ThisPtr;

            ObjectReferenceValue compositorAbi = default;
            nint retval = default;
            try
            {
                compositorAbi = Compositor.CreateMarshaler2(compositor);
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, nint, nint*, int>**)thisPtr)[6](thisPtr, MarshalInspectable<object>.GetAbi(compositorAbi), &retval));
                GC.KeepAlive(obj);
                return ContentExternalBackdropLink.FromAbi(retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(compositorAbi);
                ContentExternalBackdropLink.DisposeAbi(retval);
            }
        }
    }
}
