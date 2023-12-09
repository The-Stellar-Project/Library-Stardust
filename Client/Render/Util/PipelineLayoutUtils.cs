using System.Runtime.InteropServices;
using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render.Util;

public static unsafe class PipelineLayoutUtils {
    public static WGPU.PipelineLayout Create(WGPU.Device device, WGPU.BindGroupLayout[] layouts, string? name = null) {
        WGPU.PipelineLayoutDescriptor descriptor;
        if (name != null) {
            descriptor.Label = CString.AllocAnsi(name);
        }

        descriptor.BindGroupLayoutCount = (uint) layouts.Length;
        fixed (WGPU.BindGroupLayout* layoutPtr = layouts) {
            descriptor.BindGroupLayouts = layoutPtr;

            var pipelineLayout = WGPU.DeviceCreatePipelineLayout(device, &descriptor);

            if (name != null) {
                Marshal.FreeHGlobal(descriptor.Label);
            }

            return pipelineLayout;
        }
    }

    public static WGPU.PipelineLayout CreateEmpty(WGPU.Device device) {
        WGPU.PipelineLayoutDescriptor descriptor;
        return WGPU.DeviceCreatePipelineLayout(device, &descriptor);
    }
}