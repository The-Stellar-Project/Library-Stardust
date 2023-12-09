using System.Runtime.InteropServices;
using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render.Util; 

public static unsafe class ShaderUtils {
    private static Dictionary<string, WGPU.ShaderModule> _shaders = new();

    public static WGPU.ShaderModule LoadWgslModule(WGPU.Device device, string path) {
        if (_shaders.TryGetValue(path, out var module)) {
            return module;
        }

        var source = File.ReadAllText(path);

        WGPU.ShaderModuleWGSLDescriptor wgslModule;
        wgslModule.Chain.SType = WGPU.SType.ShaderModuleWGSLDescriptor;
        wgslModule.Code = CString.AllocAnsi(source);

        WGPU.ShaderModuleDescriptor descriptor;
        descriptor.NextInChain = (WGPU.ChainedStruct*) &wgslModule;

        var createdModule = WGPU.DeviceCreateShaderModule(device, &descriptor);

        if (createdModule == WGPU.ShaderModule.Zero) {
            throw new Exception("Failed to create shader module");
        }

        _shaders.Add(path, createdModule);
        
        Marshal.FreeHGlobal(wgslModule.Code);
        
        return createdModule;
    }
}