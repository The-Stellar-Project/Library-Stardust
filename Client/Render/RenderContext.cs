using System.Runtime.InteropServices;
using Karma.CoreGPU;
using Karma.CoreInvoke;
using Karma.Kommons.Utils;
using Karma.MediaCore;

namespace Stardust.Client.Render;

public unsafe class RenderContext : IDisposable {
    private WGPU.Adapter _adapter;
    public WGPU.Device _device;
    private WGPU.SupportedLimits _adapterLimits;
    private WGPU.AdapterProperties _adapterProperties;
    public WGPU.Device Device => _device;
    public WGPU.Queue DeviceQueue { get; private set; }
    public WGPU.Surface Surface { get; private set; }
    public WGPU.SwapChain SwapChain { get; private set; }

    public RenderContext(Window window) {
        CreateSurface(window);
        CreateAdapter();
        CreateDevice();
        CreateSwapChain(window);
    }

    private void CreateSurface(Window window) {
        WGPU.SurfaceDescriptor surfaceDescriptor;
        if (SystemInfo.IsWindows) {
            var data = window.WmInfo.Data.Windows;

            WGPU.SurfaceDescriptorFromWindowsHWND windowsSurfaceDescriptor;
            windowsSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromWindowsHWND;
            windowsSurfaceDescriptor.Hinstance = data.Instance;
            windowsSurfaceDescriptor.Hwnd = data.Window;
            surfaceDescriptor.NextInChain = (WGPU.ChainedStruct*) &windowsSurfaceDescriptor;

            Surface = WGPU.InstanceCreateSurface(WGPU.Instance.Zero, &surfaceDescriptor);
        }
        else if (SystemInfo.IsMacOS) {
            var view = SDL.CreateMetalView(window.Handle);

            if (view == SDL.MetalView.Zero) {
                throw new Exception("Could not create metal layer");
            }

            var layer = SDL.GetMetalLayer(view);

            WGPU.SurfaceDescriptorFromMetalLayer metalSurfaceDescriptor;
            metalSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromMetalLayer;
            metalSurfaceDescriptor.Layer = layer;
            surfaceDescriptor.NextInChain = (WGPU.ChainedStruct*) &metalSurfaceDescriptor;

            Surface = WGPU.InstanceCreateSurface(WGPU.Instance.Zero, &surfaceDescriptor);
        }
        else {
            if (window.WmInfo.Type == SDL.SysWmType.Wayland) {
                var data = window.WmInfo.Data.Wayland;

                WGPU.SurfaceDescriptorFromWaylandSurface waylandSurfaceDescriptor;
                waylandSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromWaylandSurface;
                waylandSurfaceDescriptor.Display = data.Display;
                waylandSurfaceDescriptor.Surface = data.Surface;
                surfaceDescriptor.NextInChain = (WGPU.ChainedStruct*) &waylandSurfaceDescriptor;

                Surface = WGPU.InstanceCreateSurface(WGPU.Instance.Zero, &surfaceDescriptor);
            }
            else {
                var data = window.WmInfo.Data.X11;

                WGPU.SurfaceDescriptorFromXlib xSurfaceDescriptor;
                xSurfaceDescriptor.Chain.SType = WGPU.SType.SurfaceDescriptorFromXlib;
                xSurfaceDescriptor.Display = data.Display;
                xSurfaceDescriptor.Window = data.Window;
                surfaceDescriptor.NextInChain = (WGPU.ChainedStruct*) &xSurfaceDescriptor;

                Surface = WGPU.InstanceCreateSurface(WGPU.Instance.Zero, &surfaceDescriptor);
            }
        }

        if (Surface == WGPU.Surface.Zero) {
            throw new Exception("InstanceCreateSurface failed");
        }
    }
    
    private void CreateDevice() {
        fixed (WGPU.Device* device = &_device) {
            WGPU.RequiredLimits requiredLimits;
            requiredLimits.Limits.MaxBindGroups = 1;

            WGPU.DeviceDescriptor deviceDescriptor;
            deviceDescriptor.RequiredLimits = &requiredLimits;

            WGPU.AdapterRequestDevice(_adapter, &deviceDescriptor, &RequestDeviceCallback, device);
        }

        if (_device == WGPU.Device.Zero) {
            throw new Exception("AdapterRequestDevice failed");
        }

        DeviceQueue = WGPU.DeviceGetQueue(_device);
    }
    
    [UnmanagedCallersOnly]
    private static void RequestAdapterCallback(WGPU.RequestAdapterStatus status, WGPU.Adapter adapter, CString message, void* userdata) {
        *(WGPU.Adapter*) userdata = adapter;
    }

    [UnmanagedCallersOnly]
    private static void RequestDeviceCallback(WGPU.RequestDeviceStatus status, WGPU.Device device, CString message, void* userdata) {
        *(WGPU.Device*) userdata = device;
    }
    
    private void CreateAdapter() {
        fixed (WGPU.Adapter* adapter = &_adapter) {
            WGPU.RequestAdapterOptions requestAdapterOptions;
            requestAdapterOptions.CompatibleSurface = Surface;
            requestAdapterOptions.PowerPreference = WGPU.PowerPreference.HighPerformance;
            requestAdapterOptions.ForceFallbackAdapter = false;

            WGPU.InstanceRequestAdapter(WGPU.Instance.Zero, &requestAdapterOptions, &RequestAdapterCallback, adapter);
        }

        if (_adapter == WGPU.Adapter.Zero) {
            throw new Exception("Failed to request adapter");
        }

        fixed (WGPU.SupportedLimits* limits = &_adapterLimits) {
            WGPU.AdapterGetLimits(_adapter, limits);
        }

        var supportedLimits = _adapterLimits.Limits;

        if (supportedLimits.MaxBindGroups < 1) {
            throw new Exception("Missing adapter capabilities");
        }

        fixed (WGPU.AdapterProperties* props = &_adapterProperties) {
            WGPU.AdapterGetProperties(_adapter, props);
        }

        PrintAdapterInfo();
    }
    
    private void CreateSwapChain(Window window) {
        WGPU.SwapChainDescriptor swapChainDescriptor;
        swapChainDescriptor.Usage = WGPU.TextureUsage.RenderAttachment;
        swapChainDescriptor.Format = WGPU.TextureFormat.BGRA8Unorm;
        swapChainDescriptor.Width = window.Width;
        swapChainDescriptor.Height = window.Height;
        swapChainDescriptor.PresentMode = WGPU.PresentMode.Immediate;

        SwapChain = WGPU.DeviceCreateSwapChain(_device, Surface, &swapChainDescriptor);

        if (SwapChain == WGPU.SwapChain.Zero) {
            throw new Exception("DeviceCreateSwapChain failed");
        }
    }


    private void PrintAdapterInfo() {
        var limits = _adapterLimits.Limits;
        Console.WriteLine($"Device Name: {_adapterProperties.Name.ToString()}, Backend: {_adapterProperties.BackendType}");
    }

    public void Dispose() {
        WGPU.DevicePoll(_device, true);

        WGPU.DeviceDrop(_device);
    }
}