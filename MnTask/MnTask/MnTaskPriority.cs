using System;
using System.Runtime.InteropServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

namespace Erinn
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MnTaskPriority : IComparable<MnTaskPriority>
    {
        public float Timestamp;
        public uint SequenceNumber;

        public int CompareTo(MnTaskPriority other)
        {
            var timestampComparison = Timestamp.CompareTo(other.Timestamp);
            return timestampComparison != 0 ? timestampComparison : (int)(SequenceNumber - other.SequenceNumber);
        }
    }
}