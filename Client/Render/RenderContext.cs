using System.Runtime.InteropServices;

using Karma.Kommons.Utils;

using Silk.NET.SDL;
using Silk.NET.WebGPU;

using Surface = Silk.NET.WebGPU.Surface;

namespace Stardust.Client.Render {
	public unsafe class RenderContext : IDisposable {
		private readonly Instance          _instance;
		private          Adapter           _adapter;
		private          SupportedLimits   _adapterLimits;
		private          AdapterProperties _adapterProperties;
		public           Device            _device;
		private          Surface           _surface;

		public RenderContext(StardustWindow stardustWindow) {
			Console.WriteLine(value: "Creating render context...");

			this._instance = *WebGPU.GetApi().CreateInstance(descriptor: null);

			this.CreateSurface(stardustWindow: stardustWindow);
			this.CreateAdapter();
			this.CreateDevice();

			fixed (Device* device = &this._device)
				WebGPU.GetApi()
					  .DeviceSetUncapturedErrorCallback(device: device,
														callback: PfnErrorCallback.From(proc: UncapturedErrorCallback),
														userdata: null);
		}

		public Device Device      => this._device;
		public Queue  DeviceQueue { get; private set; }

		public Surface Surface {
			get => this._surface;
			private set => this._surface = value;
		}

		public void Dispose() {
			/* TODO: ... | check if ported behaviour is accurate
			   WGPU.DevicePoll(this._device, true);
			   WGPU.DeviceDrop(this._device);
			 */
			fixed (Device* device = &this._device) {
				WebGPU.GetApi().DeviceRelease(device: device);
				WebGPU.GetApi().DeviceDestroy(device: device);
			}
		}

		private static void UncapturedErrorCallback(ErrorType errorType, byte* error, void* p1) =>
			Console.WriteLine(value: Marshal.PtrToStringAnsi(ptr: new nint(value: error)));

		private void CreateSurface(StardustWindow stardustWindow) {
			SurfaceDescriptor surfaceDescriptor;

			if (SystemInfo.IsWindows) {
				var data = stardustWindow.WmInfo.Info.Win;

				SurfaceDescriptorFromWindowsHWND windowsSurfaceDescriptor;
				windowsSurfaceDescriptor.Chain.SType = SType.SurfaceDescriptorFromWindowsHwnd;
				windowsSurfaceDescriptor.Hinstance   = &data.HInstance;
				windowsSurfaceDescriptor.Hwnd        = &data.Hwnd;
				surfaceDescriptor.NextInChain        = (ChainedStruct*)&windowsSurfaceDescriptor;

				fixed (Instance* instance = &this._instance)
					this.Surface = *WebGPU.GetApi()
										  .InstanceCreateSurface(instance: instance, descriptor: &surfaceDescriptor);
			} else if (SystemInfo.IsMacOS) {
				fixed (Window* window = &stardustWindow._window) {
					var view = Sdl.GetApi().MetalCreateView(window: window);

					// if (view == SDL.MetalView.Zero) throw new Exception(message: "Could not create metal layer"); TODO: ...

					var layer = Sdl.GetApi().MetalGetLayer(view: view);

					SurfaceDescriptorFromMetalLayer metalSurfaceDescriptor;
					metalSurfaceDescriptor.Chain.SType = SType.SurfaceDescriptorFromMetalLayer;
					metalSurfaceDescriptor.Layer       = layer;
					surfaceDescriptor.NextInChain      = (ChainedStruct*)&metalSurfaceDescriptor;
				}

				fixed (Instance* instance = &this._instance)
					this.Surface = *WebGPU.GetApi()
										  .InstanceCreateSurface(instance: instance, descriptor: &surfaceDescriptor);
			} else {
				if (stardustWindow.WmInfo.Subsystem == SysWMType.Wayland) {
					var data = stardustWindow.WmInfo.Info.Wayland;

					SurfaceDescriptorFromWaylandSurface waylandSurfaceDescriptor;
					waylandSurfaceDescriptor.Chain.SType = SType.SurfaceDescriptorFromWaylandSurface;
					waylandSurfaceDescriptor.Display     = data.Display;
					waylandSurfaceDescriptor.Surface     = data.Surface;
					surfaceDescriptor.NextInChain        = (ChainedStruct*)&waylandSurfaceDescriptor;

					fixed (Instance* instance = &this._instance)
						this.Surface = *WebGPU.GetApi()
											  .InstanceCreateSurface(instance: instance,
																	 descriptor: &surfaceDescriptor);
				} else {
					var data = stardustWindow.WmInfo.Info.X11;

					SurfaceDescriptorFromXlibWindow xSurfaceDescriptor;
					xSurfaceDescriptor.Chain.SType = SType.SurfaceDescriptorFromXlibWindow;
					xSurfaceDescriptor.Display     = data.Display;
					// xSurfaceDescriptor.Window      = data.Window; TODO: ...
					surfaceDescriptor.NextInChain = (ChainedStruct*)&xSurfaceDescriptor;

					fixed (Instance* instance = &this._instance)
						this.Surface = *WebGPU.GetApi()
											  .InstanceCreateSurface(instance: instance,
																	 descriptor: &surfaceDescriptor);
				}
			}

			// if (this.Surface == Surface.Zero) throw new Exception(message: "InstanceCreateSurface failed"); TODO: ...

			SurfaceConfiguration surfaceConfiguration;
			surfaceConfiguration.Usage       = TextureUsage.RenderAttachment;
			surfaceConfiguration.Format      = TextureFormat.Bgra8Unorm;
			surfaceConfiguration.Width       = stardustWindow.Width;
			surfaceConfiguration.Height      = stardustWindow.Height;
			surfaceConfiguration.PresentMode = PresentMode.Immediate;

			fixed (Surface* surface = &this._surface)
				WebGPU.GetApi().SurfaceConfigure(surface: surface, config: &surfaceConfiguration);
		}

		private void CreateDevice() {
			fixed (Device* device = &this._device) {
				RequiredLimits requiredLimits;
				requiredLimits.Limits.MaxBindGroups = 1;

				DeviceDescriptor deviceDescriptor;
				deviceDescriptor.RequiredLimits = &requiredLimits;

				fixed (Adapter* adapter = &this._adapter)
					WebGPU.GetApi()
						  .AdapterRequestDevice(adapter: adapter,
												descriptor: &deviceDescriptor,
												callback: PfnRequestDeviceCallback.From(proc: RequestDeviceCallback),
												userdata: device);
			}

			// if (this._device == WGPU.Device.Zero) throw new Exception(message: "AdapterRequestDevice failed"); TODO: ...

			fixed (Device* device = &this._device) this.DeviceQueue = *WebGPU.GetApi().DeviceGetQueue(device: device);
		}

		private static void RequestAdapterCallback(
			RequestAdapterStatus status,
			Adapter*             adapter,
			byte*                message,
			void*                userdata
		) {
			Console.WriteLine(value: Marshal.PtrToStringAnsi(ptr: new nint(value: message)));
			*(Adapter*)userdata = *adapter;
		}

		private static void RequestDeviceCallback(
			RequestDeviceStatus status,
			Device*             device,
			byte*               message,
			void*               userdata
		) => *(Device*)userdata = *device;

		private void CreateAdapter() {
			fixed (Adapter* adapter = &this._adapter) {
				RequestAdapterOptions requestAdapterOptions;
				// requestAdapterOptions.CompatibleSurface    = this.Surface; TODO: ...
				requestAdapterOptions.PowerPreference      = PowerPreference.Undefined;
				requestAdapterOptions.ForceFallbackAdapter = false;

				fixed (Instance* instance = &this._instance)
					WebGPU.GetApi()
						  .InstanceRequestAdapter(instance: instance,
												  options: &requestAdapterOptions,
												  callback: PfnRequestAdapterCallback
													  .From(proc: RequestAdapterCallback),
												  userdata: adapter);
			}

			// if (this._adapter == Adapter.Zero) throw new Exception(message: "Failed to request adapter"); TODO: ...

			fixed (Adapter* adapter = &this._adapter) {
				fixed (SupportedLimits* limits = &this._adapterLimits)
					WebGPU.GetApi().AdapterGetLimits(adapter: adapter, limits: limits);

				var supportedLimits = this._adapterLimits.Limits;
				if (supportedLimits.MaxBindGroups < 1) throw new Exception(message: "Missing adapter capabilities");

				fixed (AdapterProperties* props =
						   &this._adapterProperties)
					WebGPU.GetApi().AdapterGetProperties(adapter: adapter, properties: props);

				this.PrintAdapterInfo();
			}
		}

		private void PrintAdapterInfo() {
			var limits = this._adapterLimits.Limits;
			Console.WriteLine(value: $"Device Name: {
				Marshal.PtrToStringAnsi(ptr: new nint(value: this._adapterProperties.Name))}, Backend: {
					this._adapterProperties.BackendType}");
		}
	}
}