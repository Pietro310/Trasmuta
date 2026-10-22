// macOS system notifications (Notification Center), for news the user should see even when Trasmuta only sits in the
// menu bar: a new version. UNUserNotificationCenter asks the user once whether Trasmuta may send them; until they
// agree nothing shows, and the app's own notice stays. Its completion handlers are Objective-C blocks: the one handed
// in is a global block (no captured state) built here, the ones handed out are called through their invoke pointer.
// Clicking a notification calls back into the app through a delegate made at run time, as MacOSServicesProvider does.
// Only usable from inside an .app bundle: UNUserNotificationCenter raises an Objective-C exception otherwise.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Filee.Platform.MacOS;

[SupportedOSPlatform("macos")]
public static class MacOSNotifications
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string UserNotifications = "/System/Library/Frameworks/UserNotifications.framework/UserNotifications";

    /// <summary>UNAuthorizationOptionAlert.</summary>
    private const nint AlertOption = 1 << 2;

    /// <summary>UNNotificationPresentationOptionList | UNNotificationPresentationOptionBanner: shown while Trasmuta is in front too.</summary>
    private const nint ShowWhileActive = (1 << 3) | (1 << 4);

    /// <summary>BLOCK_IS_GLOBAL: the block lives forever and copying it returns it as it is.</summary>
    private const int BlockIsGlobal = 1 << 28;

    private delegate void AuthorizationHandler(nint block, byte granted, nint error);
    private delegate void DelegateMethod(nint self, nint selector, nint center, nint item, nint completion);
    private delegate void PresentationCompletion(nint block, nint options);
    private delegate void Completion(nint block);

    // Kept alive for as long as Objective-C may call them.
    private static readonly AuthorizationHandler Authorized = OnAuthorized;
    private static readonly DelegateMethod WillPresent = OnWillPresent;
    private static readonly DelegateMethod DidReceive = OnDidReceive;
    private static readonly Lock Gate = new();
    private static nint _delegate, _authorizationBlock;
    private static Action? _clicked;
    private static (string Id, string Title, string Body)? _pending;

    /// <summary>
    /// Sets what clicking one of Trasmuta's notifications does (<paramref name="clicked"/> runs on a background thread).
    /// Call on the main thread at start, before <see cref="Show"/>.
    /// </summary>
    public static void Initialize(Action clicked)
    {
        _clicked = clicked;
        if (_delegate != 0)
            return;
        NativeLibrary.Load(UserNotifications);
        var type = objc_getClass("TrasmutaNotificationDelegate");
        if (type == 0)
        {
            type = objc_allocateClassPair(objc_getClass("NSObject"), "TrasmutaNotificationDelegate", 0);
            class_addMethod(type, sel_registerName("userNotificationCenter:willPresentNotification:withCompletionHandler:"),
                Marshal.GetFunctionPointerForDelegate(WillPresent), "v@:@@@?");
            class_addMethod(type, sel_registerName("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:"),
                Marshal.GetFunctionPointerForDelegate(DidReceive), "v@:@@@?");
            objc_registerClassPair(type);
        }
        _delegate = Send(Send(type, sel_registerName("alloc")), sel_registerName("init"));
        SendVoid(Center(), sel_registerName("setDelegate:"), _delegate);
    }

    /// <summary>
    /// Shows a notification; one with the same <paramref name="id"/> replaces the earlier one. The first time, macOS asks
    /// whether Trasmuta may send notifications and shows it once allowed.
    /// </summary>
    public static void Show(string id, string title, string body)
    {
        lock (Gate)
            _pending = (id, title, body);
        // Answers at once when the user already decided; the notification is posted from the answer.
        SendVoid(Center(), sel_registerName("requestAuthorizationWithOptions:completionHandler:"), AlertOption, AuthorizationBlock());
    }

    private static nint Center() =>
        Send(objc_getClass("UNUserNotificationCenter"), sel_registerName("currentNotificationCenter"));

    private static void OnAuthorized(nint block, byte granted, nint error)
    {
        try
        {
            (string Id, string Title, string Body)? pending;
            lock (Gate)
                (pending, _pending) = (_pending, null);
            if (granted != 0 && pending is { } notification)
                Post(notification.Id, notification.Title, notification.Body);
        }
        catch (Exception)
        {
            // Never into Objective-C: a notification that doesn't show is no reason to end the app.
        }
    }

    private static void Post(string id, string title, string body)
    {
        var pool = Send(Send(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")), sel_registerName("init"));
        try
        {
            var content = Send(Send(objc_getClass("UNMutableNotificationContent"), sel_registerName("alloc")), sel_registerName("init"));
            SendVoid(content, sel_registerName("setTitle:"), MacOSBundle.NSString(title));
            SendVoid(content, sel_registerName("setBody:"), MacOSBundle.NSString(body));
            var request = Send(objc_getClass("UNNotificationRequest"), sel_registerName("requestWithIdentifier:content:trigger:"),
                MacOSBundle.NSString(id), content, 0);
            SendVoid(Center(), sel_registerName("addNotificationRequest:withCompletionHandler:"), request, 0);
            SendVoid(content, sel_registerName("release"));
        }
        finally
        {
            SendVoid(pool, sel_registerName("drain"));
        }
    }

    private static void OnWillPresent(nint self, nint selector, nint center, nint notification, nint completion)
    {
        try
        {
            Invoke<PresentationCompletion>(completion)(completion, ShowWhileActive);
        }
        catch (Exception)
        {
            // See OnAuthorized.
        }
    }

    private static void OnDidReceive(nint self, nint selector, nint center, nint response, nint completion)
    {
        try
        {
            _clicked?.Invoke();
        }
        catch (Exception)
        {
            // See OnAuthorized.
        }
        finally
        {
            Invoke<Completion>(completion)(completion);
        }
    }

    /// <summary>The function of a block handed in by macOS (its invoke pointer follows isa, flags and reserved).</summary>
    private static T Invoke<T>(nint block) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(block, IntPtr.Size + 8));

    /// <summary>The completion handler of requestAuthorization…: a global block calling <see cref="OnAuthorized"/>.</summary>
    private static nint AuthorizationBlock()
    {
        if (_authorizationBlock != 0)
            return _authorizationBlock;
        // struct Block_descriptor { unsigned long reserved, size; }
        var descriptor = Marshal.AllocHGlobal(2 * IntPtr.Size);
        Marshal.WriteIntPtr(descriptor, 0, 0);
        Marshal.WriteIntPtr(descriptor, IntPtr.Size, 3 * IntPtr.Size + 8);
        // struct Block_literal { void *isa; int flags, reserved; void *invoke; Block_descriptor *descriptor; }
        var block = Marshal.AllocHGlobal(3 * IntPtr.Size + 8);
        var system = NativeLibrary.Load("/usr/lib/libSystem.B.dylib");
        Marshal.WriteIntPtr(block, 0, NativeLibrary.GetExport(system, "_NSConcreteGlobalBlock"));
        Marshal.WriteInt32(block, IntPtr.Size, BlockIsGlobal);
        Marshal.WriteInt32(block, IntPtr.Size + 4, 0);
        Marshal.WriteIntPtr(block, IntPtr.Size + 8, Marshal.GetFunctionPointerForDelegate(Authorized));
        Marshal.WriteIntPtr(block, 2 * IntPtr.Size + 8, descriptor);
        return _authorizationBlock = block;
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint objc_allocateClassPair(nint superclass, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, nint extraBytes);

    [DllImport(ObjC)]
    private static extern void objc_registerClassPair(nint type);

    [DllImport(ObjC)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(nint type, nint selector, nint implementation, [MarshalAs(UnmanagedType.LPUTF8Str)] string types);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint first, nint second, nint third);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint first, nint second);
}
