using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class WindowTransparent : MonoBehaviour
{
    [DllImport("user32.dll")]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint LWA_COLORKEY = 0x00000001;
    private IntPtr hWnd;

    [Header("窗口设置")]
    [Tooltip("是否始终置顶")]
    public bool alwaysOnTop = true;

    [Tooltip("点击穿透窗口")]
    public bool clickThrough = false;

    private void Start()
    {
#if !UNITY_EDITOR
        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        uint windowStyle = WS_EX_LAYERED;
        if (clickThrough)
        {
            windowStyle |= WS_EX_TRANSPARENT;
        }
        SetWindowLong(hWnd, GWL_EXSTYLE, windowStyle);

        SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

        if (alwaysOnTop)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
        }
#endif

        Application.runInBackground = true;
    }

    public void SetAlwaysOnTop(bool onTop)
    {
        alwaysOnTop = onTop;
#if !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            IntPtr insertAfter = onTop ? HWND_TOPMOST : new IntPtr(-2);
            SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0, 0);
        }
#endif
    }

    public void SetClickThrough(bool through)
    {
        clickThrough = through;
#if !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            uint currentStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            if (through)
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle & ~WS_EX_TRANSPARENT);
            }
        }
#endif
    }
}
