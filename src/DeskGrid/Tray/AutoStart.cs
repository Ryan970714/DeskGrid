using System.IO;
using System.Security;
using DeskGrid.Diagnostics;
using Microsoft.Win32;

namespace DeskGrid.Tray;

/// <summary>
/// Starting at sign-in through the per-user Run key. Honors the switch in Task Manager's "Startup apps",
/// which keeps the Run value but flags it as disabled under StartupApproved. The command carries
/// <see cref="Switch"/>, so a start by Windows can tell itself from one by the user: at sign-in the desktop is still
/// being put together, and DeskGrid waits for it before it places its widgets.
/// </summary>
internal sealed class AutoStart(RegistryKey root, string command)
{
    public const string ValueName = "DeskGrid";
    public const string Switch = "--autostart";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static AutoStart ForCurrentUser() => new(Registry.CurrentUser, CommandFor(Environment.ProcessPath!));

    /// <summary>What the Run key starts for the exe at <paramref name="exePath"/>.</summary>
    public static string CommandFor(string exePath) => $"\"{exePath}\" {Switch}";

    /// <summary>Whether <paramref name="args"/> are those of a start by Windows at sign-in.</summary>
    public static bool WasStartedByWindows(IReadOnlyList<string> args) =>
        args.Any(arg => string.Equals(arg, Switch, StringComparison.OrdinalIgnoreCase));

    public bool IsEnabled => RunValue is not null && !IsDisabledInTaskManager;

    // An odd first byte (0x03) means "disabled"; 0x02 or no value means enabled.
    private bool IsDisabledInTaskManager =>
        ReadValue(ApprovedKey) is byte[] { Length: > 0 } flags && (flags[0] & 1) == 1;

    private string? RunValue => ReadValue(RunKey) as string;

    /// <summary>Turns the entry on or off. Returns <see langword="false"/> if the registry refused (e.g. by group policy).</summary>
    public bool SetEnabled(bool enabled) => Try(() =>
    {
        using (var run = root.CreateSubKey(RunKey))
        {
            if (enabled)
            {
                run.SetValue(ValueName, command);
            }
            else
            {
                run.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        // Our switch is the truth from now on, so drop any Task Manager flag.
        using var approved = root.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    });

    /// <summary>Points an existing entry at this exe, in case the exe was moved since it was enabled.</summary>
    public void UpdatePath() => Try(() =>
    {
        if (RunValue is { } current && current != command)
        {
            using var run = root.CreateSubKey(RunKey);
            run.SetValue(ValueName, command);
        }
    });

    // Registry access can be denied; that must not take the app down.
    private static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception exception) when (exception is SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Error("Changing the auto-start entry failed", exception);
            return false;
        }
    }

    private object? ReadValue(string key)
    {
        using var subKey = root.OpenSubKey(key);
        return subKey?.GetValue(ValueName);
    }
}
