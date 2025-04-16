using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

namespace Erinn
{
    internal sealed class MnTaskPromise
    {
        public readonly MnTaskQueue Queue;
        public uint SequenceNumber;
        public MnTaskResult State;
        public Action? Callback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskPromise(MnTaskQueue queue) => Queue = queue;
    }
}