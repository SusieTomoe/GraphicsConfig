//Class taken from http://github.com/KazWolfe/XIVBatteryGauge/ - Thank you!

using System;
using System.Runtime.InteropServices;

namespace BatteryGauge.Battery;

public static class SystemPower
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);

    private enum ACLineStatus : byte
    {
        Offline = 0,
        Online = 1,
        Unknown = 255
    }

    private enum BatteryFlag : byte
    {
        High = 1,
        Low = 2,
        Critical = 4,
        Charging = 8,
        NoSystemBattery = 128,
        Unknown = 255
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public readonly ACLineStatus LineStatus;
        public readonly BatteryFlag flgBattery;
        public readonly byte BatteryLifePercent;
        public readonly byte Reserved1;
        public readonly int BatteryLifeTime;
        public readonly int BatteryFullLifeTime;
    }

    private static SystemPowerStatus GetPowerStatus()
    {
        GetSystemPowerStatus(out var status);

        return status;
    }

    public static int ChargePercentage => GetPowerStatus().BatteryLifePercent;
    public static int LifetimeSeconds => GetPowerStatus().BatteryLifeTime;

    public static bool HasBattery =>
        GetPowerStatus().flgBattery is not (BatteryFlag.NoSystemBattery or BatteryFlag.Unknown);

    public static bool IsCharging => GetPowerStatus().LineStatus == ACLineStatus.Online;
}