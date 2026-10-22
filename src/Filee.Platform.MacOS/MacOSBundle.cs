// Where the running app really is, and the few Finder-like things done to it: macOS runs an app opened from Downloads
// (still quarantined) from a read-only copy elsewhere (App Translocation), so moving it to Applications needs the
// original's path; the copy left behind goes to the Trash; the Dock icon shows a badge while an update waits.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Filee.Platform.MacOS;

[SupportedOSPlatform("macos")]
public static class MacOSBundle
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private static readonly nint AppKit = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

    /// <summary>
    /// The folder macOS ran <paramref name="bundle"/> from: the same path, or, for a translocated app, the original
    /// (e.g. ~/Downloads/Trasmuta.app). Null when a translocated app's original can't be found.
    /// </summary>
    public static string? OriginalPath(string bundle)
    {
        var url = FileUrl(bundle);
        if (url == 0)
            return null;
        try
        {
            if (!SecTranslocateIsTranslocatedURL(url, out var translocated, 0) || !translocated)
                return bundle;
            var original = SecTranslocateCreateOriginalPathForURL(url, 0);
            if (original == 0)
                return null;
            try
            {
                return PathOf(original);
            }
            finally
            {
                CFRelease(original);
            }
        }
        catch (EntryPointNotFoundException)
        {
            return bundle.Contains("/AppTranslocation/", StringComparison.Ordinal) ? null : bundle;
        }
        finally
        {
            CFRelease(url);
        }
    }

    /// <summary>Moves <paramref name="path"/> to the Trash, as Finder does. False when it couldn't.</summary>
    public static bool MoveToTrash(string path)
    {
        var pool = Send(Send(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")), sel_registerName("init"));
        try
        {
            var url = Send(objc_getClass("NSURL"), sel_registerName("fileURLWithPath:"), NSString(path));
            var manager = Send(objc_getClass("NSFileManager"), sel_registerName("defaultManager"));
            return SendBool(manager, sel_registerName("trashItemAtURL:resultingItemURL:error:"), url, 0, 0);
        }
        finally
        {
            SendVoid(pool, sel_registerName("drain"));
        }
    }

    /// <summary>Shows <paramref name="label"/> on the Dock icon ("1"), or nothing for null. Call on the main thread.</summary>
    public static void SetDockBadge(string? label)
    {
        GC.KeepAlive(AppKit);
        var pool = Send(Send(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")), sel_registerName("init"));
        try
        {
            var application = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            var tile = Send(application, sel_registerName("dockTile"));
            SendVoid(tile, sel_registerName("setBadgeLabel:"), label is null ? 0 : NSString(label));
        }
        finally
        {
            SendVoid(pool, sel_registerName("drain"));
        }
    }

    /// <summary>An autoreleased NSString.</summary>
    internal static nint NSString(string text)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(text);
        try // stringWithUTF8String: copies the bytes
        {
            return Send(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    private static nint FileUrl(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path);
        return CFURLCreateFromFileSystemRepresentation(0, bytes, bytes.Length, true);
    }

    private static string? PathOf(nint url)
    {
        var buffer = new byte[4096];
        return CFURLGetFileSystemRepresentation(url, true, buffer, buffer.Length)
            ? Encoding.UTF8.GetString(buffer, 0, Array.IndexOf(buffer, (byte)0) is var end and >= 0 ? end : buffer.Length)
            : null;
    }

    [DllImport(Security)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SecTranslocateIsTranslocatedURL(nint path, [MarshalAs(UnmanagedType.I1)] out bool isTranslocated, nint error);

    [DllImport(Security)]
    private static extern nint SecTranslocateCreateOriginalPathForURL(nint translocatedPath, nint error);

    [DllImport(CoreFoundation)]
    private static extern nint CFURLCreateFromFileSystemRepresentation(nint allocator, byte[] buffer, nint length,
        [MarshalAs(UnmanagedType.I1)] bool isDirectory);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFURLGetFileSystemRepresentation(nint url, [MarshalAs(UnmanagedType.I1)] bool resolveAgainstBase,
        byte[] buffer, nint maxLength);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(nint value);

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBool(nint receiver, nint selector, nint first, nint second, nint third);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint argument);
}
