using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua;

public unsafe class Buffer<T> : IDisposable where T : unmanaged
{
    private T* _ptr;
    private readonly int _capacity;
    private int _count;
    private bool _disposed;
    private readonly IntPtr _handle;

    public int Capacity => _capacity;
    public int Count { get => _count; set => _count = Math.Clamp(value, 0, _capacity); }
    public IntPtr Pointer => (IntPtr)_ptr;

    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new IndexOutOfRangeException();
            return ref _ptr[index];
        }
    }

    public Buffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _handle = Marshal.AllocHGlobal(sizeof(T) * capacity);
        _ptr = (T*)_handle;
        new Span<byte>((void*)_handle, sizeof(T) * capacity).Clear();
    }

    public Span<T> AsSpan() => new Span<T>(_ptr, _count);

    public void BindToLua(LuaState state, string globalName)
    {
        state.SetGlobal(globalName, (IntPtr)_ptr);
        state.SetGlobal(globalName + "_count", _count);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Marshal.FreeHGlobal(_handle);
            _ptr = null;
            _disposed = true;
        }
    }
}
