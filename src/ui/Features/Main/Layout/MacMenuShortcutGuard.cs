using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Nikse.SubtitleEdit.Features.Main.Layout;

/// <summary>
/// Works around a macOS 27 AppKit bug that permanently kills a top-level menu (it highlights
/// when clicked, but never drops down again) when NSApp's main menu is replaced within ~100 ms
/// of one of that menu's key equivalents firing. Avalonia replaces the main menu on every
/// key-window change (the window losing key installs the app menu alone, the one gaining key
/// installs its own), so any menu shortcut that opens a dialog, a file picker, a new window,
/// or closes/minimizes a window wedges the menu it belongs to.
/// See https://github.com/AvaloniaUI/Avalonia/issues/22285 (measured: a swap 0-75 ms after the
/// key equivalent wedges the menu, 100 ms and later is clean; mouse clicks never wedge it).
///
/// Menu clicks that AppKit dispatched from a key equivalent are run a little later, so every
/// menu-bar swap they cause lands outside that window. Mouse clicks run immediately.
/// Uses the Objective-C runtime directly; any failure runs the action immediately (the
/// pre-existing behavior).
/// </summary>
internal static partial class MacMenuShortcutGuard
{
    private const string LibObjC = "/usr/lib/libobjc.A.dylib";

    // NSEventTypeKeyDown
    private const long EventTypeKeyDown = 10;

    public static readonly TimeSpan KeyEquivalentDelay = TimeSpan.FromMilliseconds(150);

    // Environment.TickCount64 of the last menu click that came from a key equivalent.
    private static long _lastKeyEquivalentTicks = long.MinValue / 2;

    /// <summary>
    /// How long a main-menu replacement must still wait so it lands outside the unsafe window
    /// after the most recent key equivalent; zero when it is safe now.
    /// </summary>
    public static TimeSpan RemainingUnsafeTime()
    {
        var elapsed = Environment.TickCount64 - _lastKeyEquivalentTicks;
        var remaining = (long)KeyEquivalentDelay.TotalMilliseconds - elapsed;
        return remaining > 0 ? TimeSpan.FromMilliseconds(remaining) : TimeSpan.Zero;
    }

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendPtr(IntPtr receiver, IntPtr sel);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static partial long SendLong(IntPtr receiver, IntPtr sel);

    /// <summary>
    /// Runs a native menu item's click action: deferred when the click came from the item's
    /// keyboard shortcut, immediately otherwise.
    /// </summary>
    public static void Run(Action action)
    {
        if (IsKeyEquivalentDispatch())
        {
            _lastKeyEquivalentTicks = Environment.TickCount64;
            DispatcherTimer.RunOnce(action, KeyEquivalentDelay);
        }
        else
        {
            action();
        }
    }

    // Avalonia raises NativeMenuItem.Click synchronously from the NSMenuItem action, so
    // NSApp.currentEvent is still the keyDown AppKit matched against the key equivalent.
    private static bool IsKeyEquivalentDispatch()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            var nsAppClass = objc_getClass("NSApplication");
            if (nsAppClass == IntPtr.Zero)
            {
                return false;
            }

            var nsApp = SendPtr(nsAppClass, sel_registerName("sharedApplication"));
            if (nsApp == IntPtr.Zero)
            {
                return false;
            }

            var currentEvent = SendPtr(nsApp, sel_registerName("currentEvent"));
            if (currentEvent == IntPtr.Zero)
            {
                return false;
            }

            return SendLong(currentEvent, sel_registerName("type")) == EventTypeKeyDown;
        }
        catch
        {
            return false;
        }
    }
}
