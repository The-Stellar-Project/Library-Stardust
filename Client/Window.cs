using System.Runtime.CompilerServices;
using System.Text;
using Karma.CoreInvoke;
using Karma.Kommons.Utils;
using Karma.MediaCore;

namespace Stardust.Client; 

public sealed unsafe class Window : IDisposable {
    private readonly SDL.Window _handle;
    private bool _isFullScreen;

    private SDL.WmInfo _wmInfo;

    public Window(string title, uint width, uint height, bool fullScreen, bool allowHighDpi) {
        _isFullScreen = fullScreen;

        var flags = SDL.WindowFlag.Shown;

        // Avoid weird fullscreen fuckery on *nix based OSs
        // running X11 or Wayland, because it's completely
        // broken within SDL2..
        if (fullScreen) {
            flags |= SystemInfo.IsUnixoid 
                ? SDL.WindowFlag.FullscreenDesktop
                : SDL.WindowFlag.Fullscreen;
        }
        else {
            flags |= SDL.WindowFlag.Resizable;
        }

        if (allowHighDpi) flags |= SDL.WindowFlag.AllowHighDpi;
        if (SystemInfo.IsMacOS) flags |= SDL.WindowFlag.Metal;

        fixed (byte* titlePtr = Encoding.UTF8.GetBytes(title)) {
            _handle = SDL.CreateWindow((CString) titlePtr, (int) SDL.WindowPosCenter, (int) SDL.WindowPosCenter, (int) width, (int) height, flags);
        }

        if (_handle == SDL.Window.Zero) SDL.ThrowCurrentError();

        fixed (SDL.WmInfo* info = &_wmInfo) {
            SDL.GetVersion(&info->Version);
            SDL.Validate(SDL.GetWindowWmInfo(_handle, info));
        }

        if (WmInfo.Type == SDL.SysWmType.Unknown) {
            throw new Exception("Unknown SysWm Type");
        }
    }

    public void Dispose() {
        SDL.DestroyWindow(_handle);
    }

    internal void Run() {
        SDL.Event e;
        var running = true;
        
        while (running) {
            while (SDL.PollEvent(&e) != 0) {
                switch (e.Type) {
                    case SDL.EventType.Quit: {
                        if (e.Quit.Type == SDL.EventType.Quit) {
                            running = false;
                        }
                        break;
                    }
                }
            }
        }
    }

    #region Properties

    public bool IsFullScreen {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isFullScreen;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if (_isFullScreen == value) return;
            SDL.SetWindowFullscreen(_handle, value ? (uint) (SystemInfo.IsUnixoid ? SDL.WindowFlag.FullscreenDesktop : SDL.WindowFlag.Fullscreen) : 0U);
            _isFullScreen = value;
        }
    }

    public uint Width {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int value;
            SDL.GetWindowSize(_handle, &value, null);
            return (uint) value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            int currentW;
            int currentH;
            SDL.GetWindowSize(_handle, &currentW, &currentH);

            if (currentW != value) SDL.SetWindowSize(_handle, (int) value, currentH);
        }
    }

    public uint Height {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int value;
            SDL.GetWindowSize(_handle, null, &value);
            return (uint) value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            int currentW;
            int currentH;
            SDL.GetWindowSize(_handle, &currentW, &currentH);

            if (currentH != value) SDL.SetWindowSize(_handle, currentW, (int) value);
        }
    }

    public int X {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int value;
            SDL.GetWindowPosition(_handle, &value, null);
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            int currentX;
            int currentY;
            SDL.GetWindowPosition(_handle, &currentX, &currentY);

            if (currentX != value) SDL.SetWindowPosition(_handle, value, currentY);
        }
    }

    public int Y {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int value;
            SDL.GetWindowPosition(_handle, null, &value);
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            int currentX;
            int currentY;
            SDL.GetWindowPosition(_handle, &currentX, &currentY);

            if (currentY != value) SDL.SetWindowPosition(_handle, currentX, value);
        }
    }

    public SDL.Window Handle => _handle;
    public SDL.WmInfo WmInfo => _wmInfo;

    #endregion
}
