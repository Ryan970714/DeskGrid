using Microsoft.Win32;

namespace DeskFolders.Tray;

/// <summary>
/// Starting at sign-in through the per-user Run key. Honors the switch in Task Manager's "Startup apps",
/// which keeps the Run value but flags it as disabled under StartupApproved.
/// </summary>
internal sealed class AutoStart(RegistryKey root, string command)
{
    public const string ValueName = "DeskFolders";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static AutoStart ForCurrentUser() => new(Registry.CurrentUser, $"\"{Environment.ProcessPath}\"");

    public bool IsEnabled => RunValue is not null && !IsDisabledInTaskManager;

    // An odd first byte (0x03) means "disabled"; 0x02 or no value means enabled.
    private bool IsDisabledInTaskManager =>
        ReadValue(ApprovedKey) is byte[] { Length: > 0 } flags && (flags[0] & 1) == 1;

    private string? RunValue => ReadValue(RunKey) as string;

    public void SetEnabled(bool enabled)
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
    }

    /// <summary>Points an existing entry at this exe, in case the exe was moved since it was enabled.</summary>
    public void UpdatePath()
    {
        if (RunValue is { } current && current != command)
        {
            using var run = root.CreateSubKey(RunKey);
            run.SetValue(ValueName, command);
        }
    }

    private object? ReadValue(string key)
    {
        using var subKey = root.OpenSubKey(key);
        return subKey?.GetValue(ValueName);
    }
}
