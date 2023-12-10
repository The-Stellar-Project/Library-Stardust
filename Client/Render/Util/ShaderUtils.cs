using System.Runtime.InteropServices;

using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render.Util {
	public static unsafe class ShaderUtils {
		private static readonly Dictionary<string, WGPU.ShaderModule> _shaders = new();

		public static WGPU.ShaderModule LoadWgslModule(WGPU.Device device, string path) {
			if (_shaders.TryGetValue(key: path, value: out var module)) return module;

			string source = File.ReadAllText(path: path);

			WGPU.ShaderModuleWGSLDescriptor wgslModule;
			wgslModule.Chain.SType = WGPU.SType.ShaderModuleWGSLDescriptor;
			wgslModule.Code        = CString.AllocAnsi(s: source);

			WGPU.ShaderModuleDescriptor descriptor;
			descriptor.NextInChain = (WGPU.ChainedStruct*)&wgslModule;

			var createdModule = WGPU.DeviceCreateShaderModule(device, &descriptor);

			if (createdModule == WGPU.ShaderModule.Zero) throw new Exception(message: "Failed to create shader module");

			_shaders.Add(key: path, value: createdModule);

			Marshal.FreeHGlobal(hglobal: wgslModule.Code);

			return createdModule;
		}
	}
}