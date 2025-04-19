#if UNITY_2021_3_OR_NEWER
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MnTaskSwitchToTaskPool : ICriticalNotifyCompletion
    {
        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskSwitchToTaskPool GetAwaiter() => this;

        public bool IsCompleted
        {
            [DebuggerHidden]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
        }

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            Debug.Assert(continuation != null);

            Task.Run(continuation);
        }
    }
}
#endif