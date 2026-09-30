using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Nikse.SubtitleEdit.Features.Main.Layout;

/// <summary>
/// Works around macOS 27 not redrawing the menu bar after Avalonia installs a window's menu.
/// When a window without a menu (e.g. Find) closes, Avalonia first installs the application-only
/// bar and then the editor window's full bar; NSApp.mainMenu is correct, but macOS keeps drawing
/// the application-only bar until the pointer hovers it or the key window changes again.
/// See https://github.com/AvaloniaUI/Avalonia/issues/22285.
///
/// Shortly after an editor window becomes key, NSApp.mainMenu is cleared and the installed
/// menu assigned again, which makes AppKit redraw it (assigning the same NSMenu alone is a
/// no-op and does not). That is a menu replacement, so it is kept out of the window after a
/// key equivalent in which a replacement kills a menu (see <see cref="MacMenuShortcutGuard"/>).
/// Uses the Objective-C runtime directly; any failure leaves the bar as it was.
/// </summary>
internal static partial class MacMenuBarRefresh
{
    private const string LibObjC = "/usr/lib/libobjc.A.dylib";

    // Tested: the redraw takes when done 100 ms after activation.
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(100);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendPtr(IntPtr receiver, IntPtr sel);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static partial void SendVoidPtr(IntPtr receiver, IntPtr sel, IntPtr arg);

    /// <summary>Called after an editor window has become key and installed its menu bar.</summary>
    public static void AfterActivation()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        DispatcherTimer.RunOnce(Refresh, RefreshDelay);
    }

    private static void Refresh()
    {
        var wait = MacMenuShortcutGuard.RemainingUnsafeTime();
        if (wait > TimeSpan.Zero)
        {
            DispatcherTimer.RunOnce(Refresh, wait);
            return;
        }

        try
        {
            var nsAppClass = objc_getClass("NSApplication");
            if (nsAppClass == IntPtr.Zero)
            {
                return;
            }

            var nsApp = SendPtr(nsAppClass, sel_registerName("sharedApplication"));
            if (nsApp == IntPtr.Zero)
            {
                return;
            }

            var mainMenu = SendPtr(nsApp, sel_registerName("mainMenu"));
            if (mainMenu == IntPtr.Zero)
            {
                return;
            }

            // NSApp and the window's AvnWindow both hold the menu, so clearing NSApp's
            // reference does not free it.
            var setMainMenu = sel_registerName("setMainMenu:");
            SendVoidPtr(nsApp, setMainMenu, IntPtr.Zero);
            SendVoidPtr(nsApp, setMainMenu, mainMenu);
        }
        catch
        {
            // Best effort: the bar still redraws on the next hover or window switch.
        }
    }
}
