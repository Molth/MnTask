using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    internal sealed class MnTaskPromise : IComparable<MnTaskPromise>
    {
        public readonly MnTaskQueue Queue;
        public ulong Timestamp;
        public uint SequenceNumber;
        public MnTaskResult State;
        public Action? Callback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskPromise(MnTaskQueue queue) => Queue = queue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(MnTaskPromise? other)
        {
            var timestampComparison = Timestamp.CompareTo(other!.Timestamp);

            if (timestampComparison != 0)
                return timestampComparison;

            var difference = (int)(SequenceNumber - other.SequenceNumber);

            Debug.Assert(difference != 0);

            return difference;
        }
    }
}