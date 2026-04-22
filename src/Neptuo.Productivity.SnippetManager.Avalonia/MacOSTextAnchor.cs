using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Avalonia;

namespace Neptuo.Productivity.SnippetManager;

internal static class MacOSTextAnchor
{
    public static WindowPositionAnchor? TryGetForFocusedElement(string? frontmostAppLabel = null)
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        string context = string.IsNullOrEmpty(frontmostAppLabel) ? string.Empty : $" Frontmost app: {frontmostAppLabel}.";

        DiagnosticsLog.Info($"Attempting to resolve a macOS accessibility anchor for main window positioning.{context}");

        try
        {
            if (!NativeMethods.AXIsProcessTrusted())
                DiagnosticsLog.Info($"macOS accessibility reports the current process is not trusted. Focused-element placement may be unavailable.{context}");

            using CFObjectHandle? systemWide = CFObjectHandle.Own(NativeMethods.AXUIElementCreateSystemWide());
            if (systemWide == null || systemWide.IsInvalid)
            {
                DiagnosticsLog.Error($"Unable to create the system-wide macOS accessibility element.{context}");
                return null;
            }

            if (TryCopyAttributeValue(systemWide, "AXFocusedUIElement", out var focusedElement, context))
            {
                using (focusedElement)
                {
                    if (TryGetSelectedTextRangeBounds(focusedElement, out var selectedTextBounds, context))
                        return CreateAnchor(selectedTextBounds, "selected text range", context);

                    if (TryGetElementBounds(focusedElement, out var focusedElementBounds, context))
                        return CreateAnchor(focusedElementBounds, "focused element", context);

                    if (TryCopyAttributeValue(focusedElement, "AXWindow", out var elementWindow, context))
                    {
                        using (elementWindow)
                        {
                            if (TryGetElementBounds(elementWindow, out var elementWindowBounds, context))
                                return CreateAnchor(elementWindowBounds, "focused element window", context);
                        }
                    }
                }
            }

            if (TryCopyAttributeValue(systemWide, "AXFocusedWindow", out var focusedWindow, context))
            {
                using (focusedWindow)
                {
                    if (TryGetElementBounds(focusedWindow, out var focusedWindowBounds, context))
                        return CreateAnchor(focusedWindowBounds, "focused window", context);
                }
            }
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error($"Unable to resolve a macOS accessibility anchor for main window positioning.{context}", ex);
        }

        DiagnosticsLog.Info($"Unable to resolve a macOS accessibility anchor. The main window will fall back to centered placement.{context}");
        return null;
    }

    private static bool TryGetSelectedTextRangeBounds(CFObjectHandle element, out PixelRect bounds, string context = "")
    {
        bounds = default;

        if (!TryCopyAttributeValue(element, "AXSelectedTextRange", out var selectedTextRange, context))
            return false;

        using (selectedTextRange)
        {
            if (!TryCopyParameterizedAttributeValue(element, "AXBoundsForRange", selectedTextRange, out var selectedTextBounds, context))
                return false;

            using (selectedTextBounds)
            {
                if (!TryGetRectValue(selectedTextBounds, out var selectedRangeRect, context))
                    return false;

                bounds = ToPixelRect(selectedRangeRect);
                DiagnosticsLog.Info($"Resolved macOS selected text bounds to {FormatPixelRect(bounds)}.{context}");

                PixelRect? screenBounds = TryGetDisplayBoundsForPoint(bounds);
                if (!IsSelectedTextBoundsPlausible(bounds, screenBounds))
                {
                    DiagnosticsLog.Info($"Rejected macOS selected text bounds {FormatPixelRect(bounds)} as implausible (degenerate size at screen edge). Falling back to alternative anchoring.{context}");
                    bounds = default;
                    return false;
                }

                return true;
            }
        }
    }

    internal static bool IsSelectedTextBoundsPlausible(PixelRect bounds, PixelRect? screenBounds)
    {
        if (bounds.Width > 1 || bounds.Height > 1)
            return true;

        // A 1×1 (or smaller) rect is only suspicious when it sits at a screen edge,
        // which indicates the application returned a degenerate placeholder rather than
        // a real caret position (observed with Chromium-based browsers' address bar).
        if (screenBounds is not PixelRect screen)
            return true;

        if (bounds.X <= screen.X || bounds.Y <= screen.Y)
            return false;

        if (bounds.X + bounds.Width >= screen.X + screen.Width || bounds.Y + bounds.Height >= screen.Y + screen.Height)
            return false;

        return true;
    }

    private static PixelRect? TryGetDisplayBoundsForPoint(PixelRect bounds)
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        try
        {
            var point = new CGPoint { X = bounds.X + bounds.Width / 2.0, Y = bounds.Y + bounds.Height / 2.0 };
            int error = NativeMethods.CGGetDisplaysWithPoint(point, 1, out uint displayId, out uint matchingCount);
            if (error != 0 || matchingCount == 0)
                return null;

            CGRect displayRect = NativeMethods.CGDisplayBounds(displayId);
            return ToPixelRect(displayRect);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetElementBounds(CFObjectHandle element, out PixelRect bounds, string context = "")
    {
        bounds = default;

        if (!TryCopyAttributeValue(element, "AXPosition", out var positionValue, context))
            return false;

        using (positionValue)
        {
            if (!TryGetPointValue(positionValue, out var position, context))
                return false;

            if (!TryCopyAttributeValue(element, "AXSize", out var sizeValue, context))
                return false;

            using (sizeValue)
            {
                if (!TryGetSizeValue(sizeValue, out var size, context))
                    return false;

                bounds = ToPixelRect(position, size);
                DiagnosticsLog.Info($"Resolved macOS accessibility element bounds to {FormatPixelRect(bounds)}.{context}");
                return true;
            }
        }
    }

    private static WindowPositionAnchor CreateAnchor(PixelRect bounds, string source, string context = "")
    {
        DiagnosticsLog.Info($"Resolved macOS accessibility anchor from {source}: {FormatPixelRect(bounds)}.{context}");
        return new WindowPositionAnchor(bounds, source);
    }

    private static bool TryGetPointValue(CFObjectHandle value, out CGPoint point, string context = "")
    {
        point = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGPoint)
        {
            DiagnosticsLog.Info($"The macOS accessibility value did not contain a CGPoint.{context}");
            return false;
        }

        if (!NativeMethods.AXValueGetCGPoint(value, AXValueType.CGPoint, out point))
        {
            DiagnosticsLog.Info($"Unable to read a CGPoint from the macOS accessibility value.{context}");
            return false;
        }

        return true;
    }

    private static bool TryGetSizeValue(CFObjectHandle value, out CGSize size, string context = "")
    {
        size = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGSize)
        {
            DiagnosticsLog.Info($"The macOS accessibility value did not contain a CGSize.{context}");
            return false;
        }

        if (!NativeMethods.AXValueGetCGSize(value, AXValueType.CGSize, out size))
        {
            DiagnosticsLog.Info($"Unable to read a CGSize from the macOS accessibility value.{context}");
            return false;
        }

        return true;
    }

    private static bool TryGetRectValue(CFObjectHandle value, out CGRect rect, string context = "")
    {
        rect = default;
        if (NativeMethods.AXValueGetType(value) != AXValueType.CGRect)
        {
            DiagnosticsLog.Info($"The macOS accessibility value did not contain a CGRect.{context}");
            return false;
        }

        if (!NativeMethods.AXValueGetCGRect(value, AXValueType.CGRect, out rect))
        {
            DiagnosticsLog.Info($"Unable to read a CGRect from the macOS accessibility value.{context}");
            return false;
        }

        return true;
    }

    private static bool TryCopyAttributeValue(CFObjectHandle element, string attributeName, [NotNullWhen(true)] out CFObjectHandle? value, string context = "")
    {
        value = null;

        using CFObjectHandle? attribute = CreateString(attributeName);
        if (attribute == null || attribute.IsInvalid)
        {
            DiagnosticsLog.Error($"Unable to create the macOS accessibility attribute string '{attributeName}'.{context}");
            return false;
        }

        AXError error = NativeMethods.AXUIElementCopyAttributeValue(element, attribute, out IntPtr rawValue);
        if (error != AXError.Success || rawValue == IntPtr.Zero)
        {
            DiagnosticsLog.Info($"macOS accessibility attribute '{attributeName}' is unavailable (error={error}).{context}");
            return false;
        }

        value = CFObjectHandle.Own(rawValue);
        return value != null && !value.IsInvalid;
    }

    private static bool TryCopyParameterizedAttributeValue(CFObjectHandle element, string attributeName, CFObjectHandle parameter, [NotNullWhen(true)] out CFObjectHandle? value, string context = "")
    {
        value = null;

        using CFObjectHandle? attribute = CreateString(attributeName);
        if (attribute == null || attribute.IsInvalid)
        {
            DiagnosticsLog.Error($"Unable to create the macOS accessibility parameterized attribute string '{attributeName}'.{context}");
            return false;
        }

        AXError error = NativeMethods.AXUIElementCopyParameterizedAttributeValue(element, attribute, parameter, out IntPtr rawValue);
        if (error != AXError.Success || rawValue == IntPtr.Zero)
        {
            DiagnosticsLog.Info($"macOS parameterized accessibility attribute '{attributeName}' is unavailable (error={error}).{context}");
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
        internal static extern int CGGetDisplaysWithPoint(CGPoint point, uint maxDisplays, out uint display, out uint matchingDisplayCount);

        [DllImport(CoreGraphicsLib)]
        internal static extern CGRect CGDisplayBounds(uint display);
    }
}
