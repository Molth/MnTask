#if UNITY_2021_3_OR_NEWER
using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    public partial struct MnTask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MnTaskSwitchToMainThread SwitchToMainThread(PlayerLoopTiming type)
        {
            switch (type)
            {
                case PlayerLoopTiming.Update:
                    break;

                case PlayerLoopTiming.LateUpdate:
                    break;

                case PlayerLoopTiming.FixedUpdate:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return new MnTaskSwitchToMainThread(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MnTaskSwitchToTaskPool SwitchToTaskPool() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MnTask DelaySeconds(PlayerLoopTiming type, float seconds)
        {
            switch (type)
            {
                case PlayerLoopTiming.Time:
                    return PlayerLoopHelpers.TimeQueue.Create(PlayerLoopHelpers.ToNanoSeconds(seconds));

                case PlayerLoopTiming.UnscaledTime:
                    return PlayerLoopHelpers.UnscaledTimeQueue.Create(PlayerLoopHelpers.ToNanoSeconds(seconds));

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MnTask DelayFrames(PlayerLoopTiming type, ulong frames)
        {
            switch (type)
            {
                case PlayerLoopTiming.Update:
                    return PlayerLoopHelpers.UpdateQueue.Create(frames);

                case PlayerLoopTiming.LateUpdate:
                    return PlayerLoopHelpers.LateUpdateQueue.Create(frames);

                case PlayerLoopTiming.FixedUpdate:
                    return PlayerLoopHelpers.FixedUpdateQueue.Create(frames);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
#endif