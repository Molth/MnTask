#if UNITY_2021_3_OR_NEWER
#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    public enum PlayerLoopTiming
    {
        Time,
        UnscaledTime,

        Update,
        LateUpdate,
        FixedUpdate
    }
}
#endif