using System.Runtime.InteropServices;

using Silk.NET.WebGPU;

namespace Stardust.Client.Render.Util {
	public static unsafe class ShaderUtils {
		private static readonly Dictionary<string, ShaderModule> _shaders = new();

		public static ShaderModule* LoadWgslModule(Device device, string path) {
			if (_shaders.TryGetValue(key: path, value: out var module)) return &module;

			string source = File.ReadAllText(path: path);

			ShaderModuleWGSLDescriptor wgslModule;
			wgslModule.Chain.SType = SType.ShaderModuleWgslDescriptor;
			wgslModule.Code        = (byte*)Marshal.StringToHGlobalAnsi(s: source).ToPointer();

			ShaderModuleDescriptor descriptor;
			descriptor.NextInChain = (ChainedStruct*)&wgslModule;

			var createdModule = WebGPU.GetApi().DeviceCreateShaderModule(device: &device, descriptor: &descriptor);

			// if (createdModule == ShaderModule.Zero) throw new Exception(message: "Failed to create shader module"); TODO: ...

			_shaders.Add(key: path, value: *createdModule);
			Marshal.FreeHGlobal(hglobal: *wgslModule.Code);
			return createdModule;
		}
	}
}