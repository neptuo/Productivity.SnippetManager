using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Avalonia;

namespace Neptuo.Productivity.SnippetManager;

internal static class MacOSTextAnchor
{
    public static WindowPositionAnchor? TryGetForFocusedElement()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        DiagnosticsLog.Info("Attempting to resolve a macOS accessibility anchor for main window positioning.");

        try
        {
            if (!NativeMethods.AXIsProcessTrusted())
                DiagnosticsLog.Info("macOS accessibility reports the current process is not trusted. Focused-element placement may be unavailable.");

            using CFObjectHandle? systemWide = CFObjectHandle.Own(NativeMethods.AXUIElementCreateSystemWide());
            if (systemWide == null || systemWide.IsInvalid)
            {
                DiagnosticsLog.Error("Unable to create the system-wide macOS accessibility element.");
                return null;
            }

            if (TryCopyAttributeValue(systemWide, "AXFocusedUIElement", out var focusedElement))
            {
                using (focusedElement)
                {
                    if (TryGetSelectedTextRangeBounds(focusedElement, out var selectedTextBounds))
                        return CreateAnchor(selectedTextBounds, "selected text range");

                    if (TryGetElementBounds(focusedElement, out var focusedElementBounds))
                        return CreateAnchor(focusedElementBounds, "focused element");

                    if (TryCopyAttributeValue(focusedElement, "AXWindow", out var elementWindow))
                    {
                        using (elementWindow)
                        {
                            if (TryGetElementBounds(elementWindow, out var elementWindowBounds))
                                return CreateAnchor(elementWindowBounds, "focused element window");
                        }
                    }
                }
            }

            if (TryCopyAttributeValue(systemWide, "AXFocusedWindow", out var focusedWindow))
            {
                using (focusedWindow)
                {
                    if (TryGetElementBounds(focusedWindow, out var focusedWindowBounds))
                        return CreateAnchor(focusedWindowBounds, "focused window");
                }
            }
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Unable to resolve a macOS accessibility anchor for main window positioning.", ex);
        }

        DiagnosticsLog.Info("Unable to resolve a macOS accessibility anchor. The main window will fall back to centered placement.");
        return null;
    }

    private static bool TryGetSelectedTextRangeBounds(CFObjectHandle element, out PixelRect bounds)
    {
        bounds = default;

        if (!TryCopyAttributeValue(element, "AXSelectedTextRange", out var selectedTextRange))
            return false;

        using (selectedTextRange)
        {
            if (!TryCopyParameterizedAttributeValue(element, "AXBoundsForRange", selectedTextRange, out var selectedTextBounds))
                return false;

            using (selectedTextBounds)
            {
                if (!TryGetRectValue(selectedTextBounds, out var selectedRangeRect))
                    return false;

                bounds = ToPixelRect(selectedRangeRect);
                DiagnosticsLog.Info($"Resolved macOS selected text bounds to {FormatPixelRect(bounds)}.");

                if (!IsSelectedTextBoundsPlausible(bounds))
                {
                    DiagnosticsLog.Info($"Rejected macOS selected text bounds {FormatPixelRect(bounds)} as implausible (degenerate size at screen edge). Falling back to alternative anchoring.");
                    bounds = default;
                    return false;
                }

                return true;
            }
        }
    }

    internal static bool IsSelectedTextBoundsPlausible(PixelRect bounds)
    {
        if (bounds.Width > 1 || bounds.Height > 1)
            return true;

        // A 1×1 (or smaller) rect is only suspicious when it sits at a screen edge,
        // which indicates the application returned a degenerate placeholder rather than
        // a real caret position (observed with Chromium-based browsers' address bar).
        if (bounds.X <= 0 || bounds.Y <= 0)
            return false;

        if (OperatingSystem.IsMacOS())
        {
            try
            {
                uint displayId = NativeMethods.CGMainDisplayID();
                int displayWidth = (int)NativeMethods.CGDisplayPixelsWide(displayId);
                int displayHeight = (int)NativeMethods.CGDisplayPixelsHigh(displayId);

                if (bounds.X + bounds.Width >= displayWidth || bounds.Y + bounds.Height >= displayHeight)
                    return false;
            }
            catch
            {
                // If we can't query display dimensions, accept the bounds.
            }
        }

        return true;
    }

    private static bool TryGetElementBounds(CFObjectHandle element, out PixelRect bounds)
    {
        bounds = default;

        if (!TryCopyAttributeValue(element, "AXPosition", out var positionValue))
            return false;

        using (positionValue)
        {
            if (!TryGetPointValue(positionValue, out var position))
                return false;

            if (!TryCopyAttributeValue(element, "AXSize", out var sizeValue))
                return false;

            using (sizeValue)
            {
                if (!TryGetSizeValue(sizeValue, out var size))
                    return false;

                bounds = ToPixelRect(position, size);
                DiagnosticsLog.Info($"Resolved macOS accessibility element bounds to {FormatPixelRect(bounds)}.");
                return true;
            }
        }
    }

    private static WindowPositionAnchor CreateAnchor(PixelRect bounds, string source)
    {
        DiagnosticsLog.Info($"Resolved macOS accessibility anchor from {source}: {FormatPixelRect(bounds)}.");
        return new WindowPositionAnchor(bounds, source);
    }

    private static bool TryGetPointValue(CFObjectHandle value, out CGPoint point)
    {
        point = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGPoint)
        {
            DiagnosticsLog.Info("The macOS accessibility value did not contain a CGPoint.");
            return false;
        }

        if (!NativeMethods.AXValueGetCGPoint(value, AXValueType.CGPoint, out point))
        {
            DiagnosticsLog.Info("Unable to read a CGPoint from the macOS accessibility value.");
            return false;
        }

        return true;
    }

    private static bool TryGetSizeValue(CFObjectHandle value, out CGSize size)
    {
        size = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGSize)
        {
            DiagnosticsLog.Info("The macOS accessibility value did not contain a CGSize.");
            return false;
        }

        if (!NativeMethods.AXValueGetCGSize(value, AXValueType.CGSize, out size))
        {
            DiagnosticsLog.Info("Unable to read a CGSize from the macOS accessibility value.");
            return false;
        }

        return true;
    }

    private static bool TryGetRectValue(CFObjectHandle value, out CGRect rect)
    {
        rect = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGRect)
        {
            DiagnosticsLog.Info("The macOS accessibility value did not contain a CGRect.");
            return false;
        }

        if (!NativeMethods.AXValueGetCGRect(value, AXValueType.CGRect, out rect))
        {
            DiagnosticsLog.Info("Unable to read a CGRect from the macOS accessibility value.");
            return false;
        }

        return true;
    }

    private static bool TryCopyAttributeValue(CFObjectHandle element, string attributeName, [NotNullWhen(true)] out CFObjectHandle? value)
    {
        value = null;

        using CFObjectHandle? attribute = CreateString(attributeName);
        if (attribute == null || attribute.IsInvalid)
        {
            DiagnosticsLog.Error($"Unable to create the macOS accessibility attribute string '{attributeName}'.");
            return false;
        }

        AXError error = NativeMethods.AXUIElementCopyAttributeValue(element, attribute, out IntPtr rawValue);
        if (error != AXError.Success || rawValue == IntPtr.Zero)
        {
            DiagnosticsLog.Info($"macOS accessibility attribute '{attributeName}' is unavailable (error={error}).");
            return false;
        }

        value = CFObjectHandle.Own(rawValue);
        return value != null && !value.IsInvalid;
    }

    private static bool TryCopyParameterizedAttributeValue(CFObjectHandle element, string attributeName, CFObjectHandle parameter, [NotNullWhen(true)] out CFObjectHandle? value)
    {
        value = null;

        using CFObjectHandle? attribute = CreateString(attributeName);
        if (attribute == null || attribute.IsInvalid)
        {
            DiagnosticsLog.Error($"Unable to create the macOS accessibility parameterized attribute string '{attributeName}'.");
            return false;
        }

        AXError error = NativeMethods.AXUIElementCopyParameterizedAttributeValue(element, attribute, parameter, out IntPtr rawValue);
        if (error != AXError.Success || rawValue == IntPtr.Zero)
        {
            DiagnosticsLog.Info($"macOS parameterized accessibility attribute '{attributeName}' is unavailable (error={error}).");
            return false;
        }

        value = CFObjectHandle.Own(rawValue);
        return value != null && !value.IsInvalid;
    }

    private static CFObjectHandle? CreateString(string value)
        => CFObjectHandle.Own(NativeMethods.CFStringCreateWithCString(IntPtr.Zero, value, CFStringEncoding.Utf8));

    private static PixelRect ToPixelRect(CGRect rect)
        => ToPixelRect(rect.Origin, rect.Size);

    private static PixelRect ToPixelRect(CGPoint point, CGSize size)
    {
        int x = (int)Math.Floor(point.X);
        int y = (int)Math.Floor(point.Y);
        int width = Math.Max(1, (int)Math.Ceiling(size.Width));
        int height = Math.Max(1, (int)Math.Ceiling(size.Height));
        return new PixelRect(x, y, width, height);
    }

    private static string FormatPixelRect(PixelRect rect)
        => $"({rect.X}, {rect.Y}, {rect.Width}, {rect.Height})";

    private enum AXError
    {
        Success = 0,
        Failure = -25200,
        IllegalArgument = -25201,
        InvalidUIElement = -25202,
        InvalidUIElementObserver = -25203,
        CannotComplete = -25204,
        AttributeUnsupported = -25205,
        ActionUnsupported = -25206,
        NotificationUnsupported = -25207,
        NotImplemented = -25208,
        NotificationAlreadyRegistered = -25209,
        NotificationNotRegistered = -25210,
        ApiDisabled = -25211,
        NoValue = -25212,
        ParameterizedAttributeUnsupported = -25213,
        NotEnoughPrecision = -25214
    }

    private enum AXValueType : uint
    {
        Illegal = 0,
        CGPoint = 1,
        CGSize = 2,
        CGRect = 3,
        CFRange = 4,
        AXError = 5
    }

    private enum CFStringEncoding : uint
    {
        Utf8 = 0x08000100
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint
    {
        public double X;
        public double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize
    {
        public double Width;
        public double Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public CGPoint Origin;
        public CGSize Size;
    }

    private sealed class CFObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private CFObjectHandle() : base(ownsHandle: true)
        {
        }

        private CFObjectHandle(IntPtr handle) : base(ownsHandle: true)
        {
            SetHandle(handle);
        }

        public static CFObjectHandle? Own(IntPtr handle)
            => handle == IntPtr.Zero ? null : new CFObjectHandle(handle);

        protected override bool ReleaseHandle()
        {
            NativeMethods.CFRelease(handle);
            return true;
        }
    }

    private static class NativeMethods
    {
        private const string ApplicationServicesLib = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(ApplicationServicesLib)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool AXIsProcessTrusted();

        [DllImport(ApplicationServicesLib)]
        internal static extern IntPtr AXUIElementCreateSystemWide();

        [DllImport(ApplicationServicesLib)]
        internal static extern AXError AXUIElementCopyAttributeValue(CFObjectHandle element, CFObjectHandle attribute, out IntPtr value);

        [DllImport(ApplicationServicesLib)]
        internal static extern AXError AXUIElementCopyParameterizedAttributeValue(CFObjectHandle element, CFObjectHandle attribute, CFObjectHandle parameter, out IntPtr value);

        [DllImport(ApplicationServicesLib)]
        internal static extern AXValueType AXValueGetType(CFObjectHandle value);

        [DllImport(ApplicationServicesLib, EntryPoint = "AXValueGetValue")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool AXValueGetCGPoint(CFObjectHandle value, AXValueType type, out CGPoint result);

        [DllImport(ApplicationServicesLib, EntryPoint = "AXValueGetValue")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool AXValueGetCGSize(CFObjectHandle value, AXValueType type, out CGSize result);

        [DllImport(ApplicationServicesLib, EntryPoint = "AXValueGetValue")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool AXValueGetCGRect(CFObjectHandle value, AXValueType type, out CGRect result);

        [DllImport(CoreFoundationLib)]
        internal static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, CFStringEncoding encoding);

        [DllImport(CoreFoundationLib)]
        internal static extern void CFRelease(IntPtr value);

        private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

        [DllImport(CoreGraphicsLib)]
        internal static extern uint CGMainDisplayID();

        [DllImport(CoreGraphicsLib)]
        internal static extern nuint CGDisplayPixelsWide(uint display);

        [DllImport(CoreGraphicsLib)]
        internal static extern nuint CGDisplayPixelsHigh(uint display);
    }
}
