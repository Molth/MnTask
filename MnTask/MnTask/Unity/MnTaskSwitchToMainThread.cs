#if UNITY_2021_3_OR_NEWER
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MnTaskSwitchToMainThread : ICriticalNotifyCompletion
    {
        private readonly PlayerLoopTiming _type;

        internal MnTaskSwitchToMainThread(PlayerLoopTiming type) => _type = type;

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskSwitchToMainThread GetAwaiter() => this;

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

            switch (_type)
            {
                case PlayerLoopTiming.Update:
                    PlayerLoopHelpers.SwitchToMainThreadUpdateQueue.Enqueue(continuation);
                    break;

                case PlayerLoopTiming.LateUpdate:
                    PlayerLoopHelpers.SwitchToMainThreadLateUpdateQueue.Enqueue(continuation);
                    break;

                case PlayerLoopTiming.FixedUpdate:
                    PlayerLoopHelpers.SwitchToMainThreadFixedUpdateQueue.Enqueue(continuation);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
#endif