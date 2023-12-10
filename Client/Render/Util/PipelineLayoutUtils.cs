using System.Runtime.InteropServices;

using Silk.NET.WebGPU;

namespace Stardust.Client.Render.Util {
	public static unsafe class PipelineLayoutUtils {
		public static PipelineLayout Create(
			Device            device,
			BindGroupLayout[] layouts,
			string?           name = null
		) {
			PipelineLayoutDescriptor descriptor;
			if (name != null) descriptor.Label = (byte*)Marshal.StringToHGlobalAnsi(s: name).ToPointer();

			descriptor.BindGroupLayoutCount = (uint)layouts.Length;
			fixed (BindGroupLayout* layoutPtr = layouts) {
				descriptor.BindGroupLayouts = &layoutPtr;
				var pipelineLayout =
					WebGPU.GetApi().DeviceCreatePipelineLayout(device: &device, descriptor: &descriptor);
				if (name != null) Marshal.FreeHGlobal(hglobal: *descriptor.Label);
				return *pipelineLayout;
			}
		}

		public static PipelineLayout* CreateEmpty(Device device) {
			PipelineLayoutDescriptor descriptor;
			return WebGPU.GetApi().DeviceCreatePipelineLayout(device: &device, descriptor: &descriptor);
		}
	}
}