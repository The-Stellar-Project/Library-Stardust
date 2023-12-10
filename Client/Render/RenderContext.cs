using System.Runtime.InteropServices;

using Karma.CoreGPU;
using Karma.CoreInvoke;
using Karma.Kommons.Utils;
using Karma.MediaCore;

namespace Stardust.Client.Render {
	public unsafe class RenderContext : IDisposable {
		private readonly WGPU.Instance          _instance;
		private          WGPU.Adapter           _adapter;
		private          WGPU.SupportedLimits   _adapterLimits;
		private          WGPU.AdapterProperties _adapterProperties;
		public           WGPU.Device            _device;

		public RenderContext(Window window) {
			Console.WriteLine(value: "Creating render context...");

			this._instance = WGPU.CreateInstance(null);

			this.CreateSurface(window: window);
			this.CreateAdapter();
			this.CreateDevice();
			this.CreateSwapChain(window: window);

			WGPU.DeviceSetUncapturedErrorCallback(this._device, &UncapturedErrorCallback, null);
		}

		public WGPU.Device    Device      => this._device;
		public WGPU.Queue     DeviceQueue { get; private set; }
		public WGPU.Surface   Surface     { get; private set; }
		public WGPU.SwapChain SwapChain   { get; private set; }

		public void Dispose() {
			WGPU.DevicePoll(this._device, true);

			WGPU.DeviceDrop(this._device);
		}

		[UnmanagedCallersOnly]
		private static void UncapturedErrorCallback(WGPU.ErrorType errorType, CString error, void* p1) =>
			Console.WriteLine(value: Marshal.PtrToStringAnsi(ptr: error));

		private void CreateSurface(Window window) {
			WGPU.SurfaceDescriptor surfaceDescriptor;
			if (SystemInfo.IsWindows) {
				var data = window.WmInfo.Data.Windows;

				WGPU.SurfaceDescriptorFromWindowsHWND windowsSurfaceDescriptor;
				windowsSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromWindowsHWND;
				windowsSurfaceDescriptor.Hinstance   = data.Instance;
				windowsSurfaceDescriptor.Hwnd        = data.Window;
				surfaceDescriptor.NextInChain        = (WGPU.ChainedStruct*)&windowsSurfaceDescriptor;

				this.Surface = WGPU.InstanceCreateSurface(this._instance, &surfaceDescriptor);
			} else if (SystemInfo.IsMacOS) {
				var view = SDL.CreateMetalView(window.Handle);

				if (view == SDL.MetalView.Zero) throw new Exception(message: "Could not create metal layer");

				var layer = SDL.GetMetalLayer(view);

				WGPU.SurfaceDescriptorFromMetalLayer metalSurfaceDescriptor;
				metalSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromMetalLayer;
				metalSurfaceDescriptor.Layer       = layer;
				surfaceDescriptor.NextInChain      = (WGPU.ChainedStruct*)&metalSurfaceDescriptor;

				this.Surface = WGPU.InstanceCreateSurface(this._instance, &surfaceDescriptor);
			} else {
				if (window.WmInfo.Type == SDL.SysWmType.Wayland) {
					var data = window.WmInfo.Data.Wayland;

					WGPU.SurfaceDescriptorFromWaylandSurface waylandSurfaceDescriptor;
					waylandSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromWaylandSurface;
					waylandSurfaceDescriptor.Display     = data.Display;
					waylandSurfaceDescriptor.Surface     = data.Surface;
					surfaceDescriptor.NextInChain        = (WGPU.ChainedStruct*)&waylandSurfaceDescriptor;

					this.Surface = WGPU.InstanceCreateSurface(this._instance, &surfaceDescriptor);
				} else {
					var data = window.WmInfo.Data.X11;

					WGPU.SurfaceDescriptorFromXlib xSurfaceDescriptor;
					xSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromXlib;
					xSurfaceDescriptor.Display     = data.Display;
					xSurfaceDescriptor.Window      = data.Window;
					surfaceDescriptor.NextInChain  = (WGPU.ChainedStruct*)&xSurfaceDescriptor;

					this.Surface = WGPU.InstanceCreateSurface(this._instance, &surfaceDescriptor);
				}
			}

			if (this.Surface == WGPU.Surface.Zero) throw new Exception(message: "InstanceCreateSurface failed");
		}

		private void CreateDevice() {
			fixed (WGPU.Device* device = &this._device) {
				WGPU.RequiredLimits requiredLimits;
				requiredLimits.Limits.MaxBindGroups = 1;

				WGPU.DeviceDescriptor deviceDescriptor;
				deviceDescriptor.RequiredLimits = &requiredLimits;

				WGPU.AdapterRequestDevice(this._adapter, &deviceDescriptor, &RequestDeviceCallback, device);
			}

			if (this._device == WGPU.Device.Zero) throw new Exception(message: "AdapterRequestDevice failed");

			this.DeviceQueue = WGPU.DeviceGetQueue(this._device);
		}

		[UnmanagedCallersOnly]
		private static void RequestAdapterCallback(
			WGPU.RequestAdapterStatus status,
			WGPU.Adapter              adapter,
			CString                   message,
			void*                     userdata
		) {
			Console.WriteLine(value: Marshal.PtrToStringAnsi(ptr: message));
			*(WGPU.Adapter*)userdata = adapter;
		}

		[UnmanagedCallersOnly]
		private static void RequestDeviceCallback(
			WGPU.RequestDeviceStatus status,
			WGPU.Device              device,
			CString                  message,
			void*                    userdata
		) => *(WGPU.Device*)userdata = device;

		private void CreateAdapter() {
			fixed (WGPU.Adapter* adapter = &this._adapter) {
				WGPU.RequestAdapterOptions requestAdapterOptions;
				requestAdapterOptions.CompatibleSurface    = this.Surface;
				requestAdapterOptions.PowerPreference      = WGPU.PowerPreference.Undefined;
				requestAdapterOptions.ForceFallbackAdapter = false;

				WGPU.InstanceRequestAdapter(this._instance, &requestAdapterOptions, &RequestAdapterCallback, adapter);
			}

			if (this._adapter == WGPU.Adapter.Zero) throw new Exception(message: "Failed to request adapter");

			fixed (WGPU.SupportedLimits* limits = &this._adapterLimits) WGPU.AdapterGetLimits(this._adapter, limits);

			var supportedLimits = this._adapterLimits.Limits;

			if (supportedLimits.MaxBindGroups < 1) throw new Exception(message: "Missing adapter capabilities");

			fixed (WGPU.AdapterProperties* props =
					   &this._adapterProperties) WGPU.AdapterGetProperties(this._adapter, props);

			this.PrintAdapterInfo();
		}

		private void CreateSwapChain(Window window) {
			WGPU.SwapChainDescriptor swapChainDescriptor;
			swapChainDescriptor.Usage       = WGPU.TextureUsage.RenderAttachment;
			swapChainDescriptor.Format      = WGPU.TextureFormat.BGRA8Unorm;
			swapChainDescriptor.Width       = window.Width;
			swapChainDescriptor.Height      = window.Height;
			swapChainDescriptor.PresentMode = WGPU.PresentMode.Immediate;

			this.SwapChain = WGPU.DeviceCreateSwapChain(this._device, this.Surface, &swapChainDescriptor);

			if (this.SwapChain == WGPU.SwapChain.Zero) throw new Exception(message: "DeviceCreateSwapChain failed");
		}


		private void PrintAdapterInfo() {
			var limits = this._adapterLimits.Limits;
			Console.WriteLine(value: $"Device Name: {this._adapterProperties.Name.ToString()}, Backend: {
				this._adapterProperties.BackendType}");
		}
	}
}