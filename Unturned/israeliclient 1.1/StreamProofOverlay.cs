using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace gatyware
{
    public static class StreamProofOverlay
    {
        #region Win32

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowExA(
            uint exStyle, string className, string windowName,
            uint style, int x, int y, int w, int h,
            IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int cmd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowA(string cls, string title);

        [DllImport("user32.dll")]
        private static extern bool PeekMessageA(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessageA(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern short RegisterClassExA(ref WNDCLASSEX wcx);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProcA(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam, lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra, cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon, hCursor, hbrBackground;
            public string lpszMenuName, lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth, biHeight;
            public ushort biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
        }

        #endregion

        #region GDI

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int style, int width, uint color);

        [DllImport("gdi32.dll")]
        private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr prev);

        [DllImport("gdi32.dll")]
        private static extern bool LineTo(IntPtr hdc, int x, int y);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, uint color);

        [DllImport("gdi32.dll")]
        private static extern bool TextOutA(IntPtr hdc, int x, int y, string str, int len);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateFontA(int h, int w, int esc, int ori, int weight, uint ital, uint under, uint strike, uint charset, uint outPrec, uint clipPrec, uint quality, uint pitch, string face);

        [DllImport("gdi32.dll")]
        private static extern bool SetPixel(IntPtr hdc, int x, int y, uint color);

        #endregion

        #region Constants

        private const uint WS_EX_LAYERED = 0x80000;
        private const uint WS_EX_TRANSPARENT = 0x20;
        private const uint WS_EX_TOPMOST = 0x8;
        private const uint WS_EX_TOOLWINDOW = 0x80;
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint ULW_ALPHA = 0x02;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;
        private const int SW_SHOWNOACTIVATE = 4;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x10;
        private const uint SWP_NOMOVE = 0x02;
        private const uint SWP_NOSIZE = 0x01;

        #endregion

        // Render primitives pushed from Unity thread
        public struct RenderLine
        {
            public int x1, y1, x2, y2;
            public uint color;
            public int thick;
        }

        public struct RenderBox
        {
            public int x, y, w, h;
            public uint color;
            public int thick;
            public bool filled;
            public uint fillColor;
        }

        public struct RenderText
        {
            public int x, y;
            public string text;
            public uint color;
            public int fontSize;
        }

        // Double-buffered render list
        private static List<RenderLine> _linesA = new List<RenderLine>(256);
        private static List<RenderBox> _boxesA = new List<RenderBox>(128);
        private static List<RenderText> _textsA = new List<RenderText>(256);
        private static List<RenderLine> _linesB = new List<RenderLine>(256);
        private static List<RenderBox> _boxesB = new List<RenderBox>(128);
        private static List<RenderText> _textsB = new List<RenderText>(256);
        private static readonly object _swapLock = new object();
        private static bool _useA = true;

        // Overlay state
        private static Thread _thread;
        private static volatile bool _running;
        private static IntPtr _hwnd;
        private static IntPtr _gameHwnd;
        private static int _width, _height;
        private static int _posX, _posY;
        private static WndProcDelegate _wndProc;
        private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

        public static bool IsActive => _running && _hwnd != IntPtr.Zero;

        public static void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(OverlayThread);
            _thread.IsBackground = true;
            _thread.Start();
            Runtime.Trace("streamproof: starting overlay thread");
        }

        public static void Stop()
        {
            _running = false;
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(2000);
            }
            _thread = null;
            _hwnd = IntPtr.Zero;
            Runtime.Trace("streamproof: stopped");
        }

        // Called from Unity thread: begin building frame
        public static void BeginFrame()
        {
            var lines = _useA ? _linesA : _linesB;
            var boxes = _useA ? _boxesA : _boxesB;
            var texts = _useA ? _textsA : _textsB;
            lines.Clear();
            boxes.Clear();
            texts.Clear();
        }

        // Called from Unity thread: push primitives
        public static void PushLine(int x1, int y1, int x2, int y2, Color c, int thick = 1)
        {
            var lines = _useA ? _linesA : _linesB;
            lines.Add(new RenderLine { x1 = x1, y1 = y1, x2 = x2, y2 = y2, color = ToGDI(c), thick = thick });
        }

        public static void PushBox(int x, int y, int w, int h, Color c, int thick = 1, bool filled = false, Color fill = default)
        {
            var boxes = _useA ? _boxesA : _boxesB;
            boxes.Add(new RenderBox { x = x, y = y, w = w, h = h, color = ToGDI(c), thick = thick, filled = filled, fillColor = ToGDI(fill) });
        }

        public static void PushText(int x, int y, string text, Color c, int fontSize = 12)
        {
            var texts = _useA ? _textsA : _textsB;
            texts.Add(new RenderText { x = x, y = y, text = text, color = ToGDI(c), fontSize = fontSize });
        }

        // Called from Unity thread: swap buffers
        public static void EndFrame()
        {
            lock (_swapLock)
            {
                _useA = !_useA;
            }
        }

        private static uint ToGDI(Color c)
        {
            byte r = (byte)(Mathf.Clamp01(c.r) * 255);
            byte g = (byte)(Mathf.Clamp01(c.g) * 255);
            byte b = (byte)(Mathf.Clamp01(c.b) * 255);
            return (uint)(r | (g << 8) | (b << 16));
        }

        private static byte ToAlpha(Color c)
        {
            return (byte)(Mathf.Clamp01(c.a) * 255);
        }

        private static void OverlayThread()
        {
            try
            {
                // Find game window
                _gameHwnd = FindGameWindow();
                if (_gameHwnd == IntPtr.Zero)
                {
                    Runtime.Trace("streamproof: game window not found");
                    _running = false;
                    return;
                }

                // Register window class
                _wndProc = WndProc;
                var wcx = new WNDCLASSEX();
                wcx.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX));
                wcx.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc);
                wcx.lpszClassName = "GW_SP_OVL";
                wcx.hInstance = IntPtr.Zero;
                RegisterClassExA(ref wcx);

                // Get game rect
                RECT gr;
                GetWindowRect(_gameHwnd, out gr);
                _posX = gr.left;
                _posY = gr.top;
                _width = gr.right - gr.left;
                _height = gr.bottom - gr.top;

                // Create overlay window
                uint exStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                _hwnd = CreateWindowExA(exStyle, "GW_SP_OVL", "", WS_POPUP,
                    _posX, _posY, _width, _height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                if (_hwnd == IntPtr.Zero)
                {
                    Runtime.Trace("streamproof: CreateWindow failed");
                    _running = false;
                    return;
                }

                // Apply stream-proof: WDA_EXCLUDEFROMCAPTURE
                bool affinityOk = SetWindowDisplayAffinity(_hwnd, WDA_EXCLUDEFROMCAPTURE);
                Runtime.Trace("streamproof: affinity set = " + affinityOk);

                ShowWindow(_hwnd, SW_SHOWNOACTIVATE);

                // Render loop
                while (_running)
                {
                    // Pump messages
                    MSG msg;
                    while (PeekMessageA(out msg, _hwnd, 0, 0, 1))
                    {
                        TranslateMessage(ref msg);
                        DispatchMessageA(ref msg);
                    }

                    // Track game window position
                    TrackGameWindow();

                    // Render frame
                    RenderFrame();

                    Thread.Sleep(16); // ~60fps
                }

                // Cleanup
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Runtime.Trace("streamproof thread err: " + ex.Message);
                _running = false;
            }
        }

        private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
        {
            return DefWindowProcA(hwnd, msg, wp, lp);
        }

        private static IntPtr FindGameWindow()
        {
            // Try Unturned window titles
            IntPtr h = FindWindowA(null, "Unturned");
            if (h != IntPtr.Zero) return h;
            h = FindWindowA("UnityWndClass", null);
            return h;
        }

        private static void TrackGameWindow()
        {
            if (_gameHwnd == IntPtr.Zero) return;
            RECT gr;
            if (!GetWindowRect(_gameHwnd, out gr)) return;

            int nx = gr.left, ny = gr.top;
            int nw = gr.right - gr.left, nh = gr.bottom - gr.top;

            if (nx != _posX || ny != _posY || nw != _width || nh != _height)
            {
                _posX = nx; _posY = ny; _width = nw; _height = nh;
                SetWindowPos(_hwnd, HWND_TOPMOST, _posX, _posY, _width, _height, SWP_NOACTIVATE);
            }
            else
            {
                // Keep on top
                SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        private static void RenderFrame()
        {
            if (_width <= 0 || _height <= 0) return;

            // Get render data snapshot
            List<RenderLine> lines;
            List<RenderBox> boxes;
            List<RenderText> texts;
            lock (_swapLock)
            {
                // Read from the buffer NOT currently being written
                lines = _useA ? _linesB : _linesA;
                boxes = _useA ? _boxesB : _boxesA;
                texts = _useA ? _textsB : _textsA;
            }

            // Create DIB for rendering
            IntPtr screenDC = GetDC(IntPtr.Zero);
            IntPtr memDC = CreateCompatibleDC(screenDC);

            var bmi = new BITMAPINFOHEADER();
            bmi.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.biWidth = _width;
            bmi.biHeight = -_height; // top-down
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = 0;

            IntPtr bits;
            IntPtr hBmp = CreateDIBSection(screenDC, ref bmi, 0, out bits, IntPtr.Zero, 0);
            IntPtr oldBmp = SelectObject(memDC, hBmp);

            // Clear to transparent (already zero from DIB creation)
            SetBkMode(memDC, 1); // TRANSPARENT

            // Draw boxes
            for (int i = 0; i < boxes.Count; i++)
            {
                var b = boxes[i];
                if (b.filled)
                {
                    DrawFilledRect(memDC, bits, b.x, b.y, b.w, b.h, b.fillColor, 80);
                }
                DrawRect(memDC, b.x, b.y, b.w, b.h, b.color, b.thick);
            }

            // Draw lines
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                DrawLine(memDC, l.x1, l.y1, l.x2, l.y2, l.color, l.thick);
            }

            // Draw text
            IntPtr font = CreateFontA(12, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
            IntPtr oldFont = SelectObject(memDC, font);
            for (int i = 0; i < texts.Count; i++)
            {
                var t = texts[i];
                if (t.fontSize != 12)
                {
                    SelectObject(memDC, oldFont);
                    DeleteObject(font);
                    font = CreateFontA(t.fontSize, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
                    oldFont = SelectObject(memDC, font);
                }
                // Draw text with alpha by writing pixels (GDI TextOut doesn't support alpha well)
                SetTextColor(memDC, t.color);
                if (t.text != null)
                    TextOutA(memDC, t.x, t.y, t.text, t.text.Length);
            }
            SelectObject(memDC, oldFont);
            DeleteObject(font);

            // Apply alpha to all non-zero pixels in the DIB
            ApplyAlpha(bits, _width, _height);

            // Update layered window
            var ptDst = new POINT { x = _posX, y = _posY };
            var sz = new SIZE { cx = _width, cy = _height };
            var ptSrc = new POINT { x = 0, y = 0 };
            var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            UpdateLayeredWindow(_hwnd, screenDC, ref ptDst, ref sz, memDC, ref ptSrc, 0, ref blend, ULW_ALPHA);

            // Cleanup
            SelectObject(memDC, oldBmp);
            DeleteObject(hBmp);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);
        }

        private static void DrawLine(IntPtr hdc, int x1, int y1, int x2, int y2, uint color, int thick)
        {
            IntPtr pen = CreatePen(0, thick, color);
            IntPtr old = SelectObject(hdc, pen);
            MoveToEx(hdc, x1, y1, IntPtr.Zero);
            LineTo(hdc, x2, y2);
            SelectObject(hdc, old);
            DeleteObject(pen);
        }

        private static void DrawRect(IntPtr hdc, int x, int y, int w, int h, uint color, int thick)
        {
            IntPtr pen = CreatePen(0, thick, color);
            IntPtr brush = CreateSolidBrush(0); // hollow
            IntPtr oldPen = SelectObject(hdc, pen);
            IntPtr oldBrush = SelectObject(hdc, brush);
            // Draw outline manually with lines for hollow rect
            MoveToEx(hdc, x, y, IntPtr.Zero);
            LineTo(hdc, x + w, y);
            LineTo(hdc, x + w, y + h);
            LineTo(hdc, x, y + h);
            LineTo(hdc, x, y);
            SelectObject(hdc, oldPen);
            SelectObject(hdc, oldBrush);
            DeleteObject(pen);
            DeleteObject(brush);
        }

        private static unsafe void DrawFilledRect(IntPtr hdc, IntPtr bits, int x, int y, int w, int h, uint color, byte alpha)
        {
            if (bits == IntPtr.Zero) return;
            byte r = (byte)(color & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)((color >> 16) & 0xFF);
            // Premultiply
            byte pr = (byte)((r * alpha) / 255);
            byte pg = (byte)((g * alpha) / 255);
            byte pb = (byte)((b * alpha) / 255);

            int stride = _width * 4;
            byte* p = (byte*)bits.ToPointer();
            int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
            int x1 = Math.Min(_width, x + w), y1 = Math.Min(_height, y + h);
            for (int row = y0; row < y1; row++)
            {
                for (int col = x0; col < x1; col++)
                {
                    int off = row * stride + col * 4;
                    p[off] = pb;     // B
                    p[off + 1] = pg; // G
                    p[off + 2] = pr; // R
                    p[off + 3] = alpha; // A
                }
            }
        }

        private static unsafe void ApplyAlpha(IntPtr bits, int w, int h)
        {
            if (bits == IntPtr.Zero) return;
            // For UpdateLayeredWindow with AC_SRC_ALPHA, pixels must be premultiplied.
            // GDI text/lines write RGB but leave A=0. Set A=255 for any non-zero pixel.
            int total = w * h;
            uint* px = (uint*)bits.ToPointer();
            for (int i = 0; i < total; i++)
            {
                uint v = px[i];
                if ((v & 0x00FFFFFF) != 0 && (v >> 24) == 0)
                {
                    // Non-zero color but zero alpha — set full alpha and premultiply
                    byte r = (byte)(v & 0xFF);
                    byte g = (byte)((v >> 8) & 0xFF);
                    byte b = (byte)((v >> 16) & 0xFF);
                    px[i] = (uint)(b | (g << 8) | (r << 16) | (255 << 24));
                }
            }
        }
    }
}
