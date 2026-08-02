using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// Makes exact magenta pixels see-through to the desktop on Windows standalone builds.
    /// Requires Direct3D11 and Player Settings → Use DXGI Flip Model Swapchain = OFF.
    /// Does nothing in the Editor (upper band stays magenta there — expected).
    /// </summary>
    public static class DesktopTransparency
    {
        public static readonly Color32 KeyColor = new Color32(255, 0, 255, 255);
        public static Color KeyColorUnity => new Color(1f, 0f, 1f, 1f);

        // COLORREF is 0x00bbggrr → pure magenta RGB(255,0,255)
        private const uint KeyColorRef = 0x00FF00FF;

        private static bool enabled;
        private static bool loggedApi;
        private static float nextRetryTime;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlExStyle = -20;
        private const uint WsExLayered = 0x00080000;
        private const uint LwaColorKey = 0x00000001;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, value) : SetWindowLongPtr32(hWnd, nIndex, value);
        }
#endif

        public static bool IsEnabled => enabled;

        public static void Enable()
        {
            enabled = true;
            LogApiOnce();
            Apply();
        }

        public static void Disable()
        {
            enabled = false;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr hwnd = ResolveHwnd();
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            long ex = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            if ((ex & WsExLayered) == 0)
            {
                return;
            }

            // Clear color-key, then drop layered style.
            SetLayeredWindowAttributes(hwnd, 0, 255, 0x00000002); // LWA_ALPHA full opaque
            ex &= ~WsExLayered;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(ex));
#endif
        }

        public static void Pulse()
        {
            if (!enabled)
            {
                return;
            }

            // Re-apply periodically — Unity/resolution/focus can drop WS_EX_LAYERED.
            if (Time.unscaledTime >= nextRetryTime)
            {
                nextRetryTime = Time.unscaledTime + 0.5f;
                Apply();
            }
        }

        private static void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr hwnd = ResolveHwnd();
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            long ex = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            ex |= WsExLayered;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(ex));
            SetLayeredWindowAttributes(hwnd, KeyColorRef, 0, LwaColorKey);
#endif
        }

        private static void LogApiOnce()
        {
            if (loggedApi)
            {
                return;
            }

            loggedApi = true;

#if UNITY_EDITOR
            Debug.Log(
                "[DesktopTransparency] Editor: upper band stays magenta. " +
                "Desktop see-through only works in a Windows standalone build (D3D11, Flip Model OFF).");
#else
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
            {
                Debug.LogWarning(
                    "[DesktopTransparency] Needs Direct3D11 + Flip Model OFF. " +
                    $"Current API={SystemInfo.graphicsDeviceType}. Launch with -force-d3d11 if upper band stays magenta.");
            }
            else
            {
                Debug.Log("[DesktopTransparency] Color-key enabled (magenta → desktop).");
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static IntPtr ResolveHwnd()
        {
            IntPtr hwnd = FindWindow("UnityWndClass", Application.productName);
            if (hwnd != IntPtr.Zero)
            {
                return hwnd;
            }

            // productName can differ from window title in some builds
            hwnd = FindWindow("UnityWndClass", null);
            if (hwnd != IntPtr.Zero)
            {
                return hwnd;
            }

            return GetActiveWindow();
        }
#endif
    }
}
