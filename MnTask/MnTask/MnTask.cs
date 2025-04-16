using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

namespace Erinn
{
    [StructLayout(LayoutKind.Sequential)]
    [AsyncMethodBuilder(typeof(MnTaskAsyncMethodBuilder))]
    public readonly struct MnTask : ICriticalNotifyCompletion
    {
        internal readonly float Delay;
        internal readonly uint SequenceNumber;
        internal readonly MnTaskPromise? Promise;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MnTask(float delay, uint sequenceNumber, MnTaskPromise promise)
        {
            Delay = delay;
            SequenceNumber = sequenceNumber;
            Promise = promise;
        }

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTask GetAwaiter() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start(Action continuation) => UnsafeOnCompleted(continuation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            var promise = Promise;
            if (promise != null && promise.SequenceNumber == SequenceNumber)
            {
                promise.Callback = null;

                switch (promise.State)
                {
                    case MnTaskResult.Running:
                        promise.State = MnTaskResult.Stopped;
                        break;

                    case MnTaskResult.Pending:
                        promise.State = MnTaskResult.NotStarted;
                        promise.Queue.Push(promise);
                        break;

                    case MnTaskResult.NotStarted:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            var promise = Promise;
            if (promise != null && promise.SequenceNumber == SequenceNumber)
            {
                var callback = promise.Callback;
                promise.Callback = null;

                switch (promise.State)
                {
                    case MnTaskResult.Running:
                        promise.State = MnTaskResult.Canceled;

                        callback?.Invoke();
                        break;

                    case MnTaskResult.Pending:
                        promise.State = MnTaskResult.NotStarted;
                        promise.Queue.Push(promise);
                        break;

                    case MnTaskResult.NotStarted:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsCompleted
        {
            [DebuggerHidden]
            [EditorBrowsable(EditorBrowsableState.Never)]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskResult GetResult()
        {
            var promise = Promise;

            if (promise == null || promise.SequenceNumber != SequenceNumber)
                throw new InvalidOperationException();

            return promise.State;
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
            if (continuation == null)
                throw new NullReferenceException();

            var promise = Promise;

            if (promise == null || promise.SequenceNumber != SequenceNumber || promise.State != MnTaskResult.Pending)
                throw new InvalidOperationException();

            promise.Queue.Enqueue(Delay, SequenceNumber, promise, continuation);
        }
    }
}