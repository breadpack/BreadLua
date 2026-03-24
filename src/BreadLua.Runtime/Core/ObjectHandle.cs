using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua;

public static class ObjectHandle
{
    public static IntPtr Alloc(object obj)
    {
        var handle = GCHandle.Alloc(obj);
        return GCHandle.ToIntPtr(handle);
    }

    public static T? Get<T>(IntPtr ptr) where T : class
    {
        if (ptr == IntPtr.Zero) return null;
        var handle = GCHandle.FromIntPtr(ptr);
        return handle.Target as T;
    }

    public static void Free(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return;
        var handle = GCHandle.FromIntPtr(ptr);
        if (handle.IsAllocated) handle.Free();
    }
}
