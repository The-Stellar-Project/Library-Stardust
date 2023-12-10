using System.Runtime.CompilerServices;
using System.Text;

using Karma.Kommons.Utils;

using Silk.NET.SDL;

namespace Stardust.Client {
	public sealed unsafe class StardustWindow : IDisposable {
		private bool _isFullScreen;

		public Window _window;

		private SysWMInfo _wmInfo;

		public StardustWindow(string title, uint width, uint height, bool fullScreen, bool allowHighDpi) {
			this._isFullScreen = fullScreen;

			var flags = WindowFlags.Shown;

			/*
			 * Avoid weird fullscreen fuckery on *nix based OSs
			 * running X11 or Wayland, because it's completely
			 * broken within SDL2..
			 */
			if (fullScreen)
				flags |= SystemInfo.IsUnixoid ?
							 WindowFlags.FullscreenDesktop :
							 WindowFlags.Fullscreen;
			else flags |= WindowFlags.Resizable;

			if (allowHighDpi) flags       |= WindowFlags.AllowHighdpi;
			if (SystemInfo.IsMacOS) flags |= WindowFlags.Metal;

			fixed (byte* titlePtr = Encoding.UTF8.GetBytes(s: title))
				this._window = *Sdl.GetApi()
								   .CreateWindow(title: titlePtr,
												 x: Sdl.WindowposCentered,
												 y: Sdl.WindowposCentered,
												 w: (int)width,
												 h: (int)height,
												 flags: (uint)flags);

			// if (this._window == SDL.Window.Zero) Sdl.GetApi().ThrowError(); TODO: ...

			fixed (SysWMInfo* info = &this._wmInfo) Sdl.GetApi().GetVersion(ver: &info->Version);
			// SDL.Validate(result: SDL.GetWindowWmInfo(this._window, info)); TODO: ...
			// if (this.WmInfo.Type == SDL.SysWmType.Unknown) throw new Exception(message: "Unknown SysWm Type"); TODO: ...
		}

		public void Dispose() {
			fixed (Window* window = &this._window) Sdl.GetApi().DestroyWindow(window: window);
		}

		internal void Run(Action function) {
			Event e;
			bool  running = true;

			while (running) {
				while (Sdl.GetApi().PollEvent(@event: &e) != 0)
					switch (e.Type) {
						case (uint)EventType.Quit: {
							if (e.Quit.Type == (uint)EventType.Quit) running = false;
							break;
						}
					}

				function();
			}
		}

		#region Properties
		public bool IsFullScreen {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get => this._isFullScreen;
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				if (this._isFullScreen == value) return;
				fixed (Window* window = &this._window)
					Sdl.GetApi()
					   .SetWindowFullscreen(window: window,
											flags: value ?
													   (uint)(SystemInfo.IsUnixoid ?
																  WindowFlags.FullscreenDesktop :
																  WindowFlags.Fullscreen) :
													   0U);
				this._isFullScreen = value;
			}
		}

		public uint Width {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				fixed (Window* window = &this._window) Sdl.GetApi().GetWindowSize(window: window, w: &value, h: null);
				return (uint)value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentW;
				int currentH;
				fixed (Window* window = &this._window) {
					Sdl.GetApi()
					   .GetWindowSize(window: window, w: &currentW, h: &currentH);
					if (currentW != value) Sdl.GetApi().SetWindowSize(window: window, w: (int)value, h: currentH);
				}
			}
		}

		public uint Height {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				fixed (Window* window = &this._window) Sdl.GetApi().GetWindowSize(window: window, w: null, h: &value);

				return (uint)value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentW;
				int currentH;
				fixed (Window* window = &this._window) {
					Sdl.GetApi().GetWindowSize(window: window, w: &currentW, h: &currentH);
					if (currentH != value) Sdl.GetApi().SetWindowSize(window: window, w: currentW, h: (int)value);
				}
			}
		}

		public int X {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				fixed (Window* window =
						   &this._window) Sdl.GetApi().GetWindowPosition(window: window, x: &value, y: null);
				return value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentX;
				int currentY;
				fixed (Window* window = &this._window) {
					Sdl.GetApi().GetWindowPosition(window: window, x: &currentX, y: &currentY);
					if (currentX != value) Sdl.GetApi().SetWindowPosition(window: window, x: value, y: currentY);
				}
			}
		}

		public int Y {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				fixed (Window* window =
						   &this._window) Sdl.GetApi().GetWindowPosition(window: window, x: null, y: &value);
				return value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentX;
				int currentY;
				fixed (Window* window = &this._window) {
					Sdl.GetApi().GetWindowPosition(window: window, x: &currentX, y: &currentY);
					if (currentY != value) Sdl.GetApi().SetWindowPosition(window: window, x: currentX, y: value);
				}
			}
		}

		public SysWMInfo WmInfo => this._wmInfo;
		#endregion
	}
}