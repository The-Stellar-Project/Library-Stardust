using System.Runtime.CompilerServices;
using System.Text;

using Karma.CoreInvoke;
using Karma.Kommons.Utils;
using Karma.MediaCore;

namespace Stardust.Client {
	public sealed unsafe class Window : IDisposable {
		private bool _isFullScreen;

		private SDL.WmInfo _wmInfo;

		public Window(string title, uint width, uint height, bool fullScreen, bool allowHighDpi) {
			this._isFullScreen = fullScreen;

			var flags = SDL.WindowFlag.Shown;

			/*
			 * Avoid weird fullscreen fuckery on *nix based OSs
			 * running X11 or Wayland, because it's completely
			 * broken within SDL2..
			 */
			if (fullScreen)
				flags |= SystemInfo.IsUnixoid ?
							 SDL.WindowFlag.FullscreenDesktop :
							 SDL.WindowFlag.Fullscreen;
			else flags |= SDL.WindowFlag.Resizable;

			if (allowHighDpi) flags       |= SDL.WindowFlag.AllowHighDpi;
			if (SystemInfo.IsMacOS) flags |= SDL.WindowFlag.Metal;

			fixed (byte* titlePtr = Encoding.UTF8.GetBytes(s: title))
				this.Handle = SDL.CreateWindow((CString)titlePtr,
											   (int)SDL.WindowPosCenter,
											   (int)SDL.WindowPosCenter,
											   (int)width,
											   (int)height,
											   flags);

			if (this.Handle == SDL.Window.Zero) SDL.ThrowCurrentError();

			fixed (SDL.WmInfo* info = &this._wmInfo) {
				SDL.GetVersion(&info->Version);
				SDL.Validate(result: SDL.GetWindowWmInfo(this.Handle, info));
			}

			if (this.WmInfo.Type == SDL.SysWmType.Unknown) throw new Exception(message: "Unknown SysWm Type");
		}

		public void Dispose() => SDL.DestroyWindow(this.Handle);

		internal void Run(Action function) {
			SDL.Event e;
			bool      running = true;

			while (running) {
				while (SDL.PollEvent(&e) != 0)
					switch (e.Type) {
						case SDL.EventType.Quit: {
							if (e.Quit.Type == SDL.EventType.Quit) running = false;

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
				SDL.SetWindowFullscreen(this.Handle,
										value ?
											(uint)(SystemInfo.IsUnixoid ?
													   SDL.WindowFlag.FullscreenDesktop :
													   SDL.WindowFlag.Fullscreen) :
											0U);
				this._isFullScreen = value;
			}
		}

		public uint Width {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				SDL.GetWindowSize(this.Handle, &value, null);
				return (uint)value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentW;
				int currentH;
				SDL.GetWindowSize(this.Handle, &currentW, &currentH);

				if (currentW != value) SDL.SetWindowSize(this.Handle, (int)value, currentH);
			}
		}

		public uint Height {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				SDL.GetWindowSize(this.Handle, null, &value);
				return (uint)value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentW;
				int currentH;
				SDL.GetWindowSize(this.Handle, &currentW, &currentH);

				if (currentH != value) SDL.SetWindowSize(this.Handle, currentW, (int)value);
			}
		}

		public int X {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				SDL.GetWindowPosition(this.Handle, &value, null);
				return value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentX;
				int currentY;
				SDL.GetWindowPosition(this.Handle, &currentX, &currentY);

				if (currentX != value) SDL.SetWindowPosition(this.Handle, value, currentY);
			}
		}

		public int Y {
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			get {
				int value;
				SDL.GetWindowPosition(this.Handle, null, &value);
				return value;
			}
			[MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
			set {
				int currentX;
				int currentY;
				SDL.GetWindowPosition(this.Handle, &currentX, &currentY);

				if (currentY != value) SDL.SetWindowPosition(this.Handle, currentX, value);
			}
		}

		public SDL.Window Handle { get; }

		public SDL.WmInfo WmInfo => this._wmInfo;
		#endregion
	}
}