using System.Runtime.InteropServices;
using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render.Util;

public unsafe class PipelineBuilder {
    private readonly WGPU.Device _device;

    private WGPU.FragmentState* _fragmentState;
    private WGPU.RenderPipelineDescriptor _renderPipelineDescriptor;
    private WGPU.PipelineLayout _pipelineLayout;

    public PipelineBuilder(WGPU.Device device) {
        _device = device;
        
        _fragmentState = (WGPU.FragmentState*)Marshal.AllocHGlobal(sizeof(WGPU.FragmentState)).ToPointer();
        _renderPipelineDescriptor.Fragment = _fragmentState;
    }

    public PipelineBuilder Name(string name) {
        _renderPipelineDescriptor.Label = CString.AllocAnsi(name);

        return this;
    }
    
    public PipelineBuilder PipelineLayout(WGPU.PipelineLayout pipelineLayout) {
        _pipelineLayout = pipelineLayout;
        return this;
    }
    
    public PipelineBuilder VertexShader(string path, string entryPoint) {
        _renderPipelineDescriptor.Vertex.Module = ShaderUtils.LoadWgslModule(_device, path);
        _renderPipelineDescriptor.Vertex.EntryPoint = CString.AllocAnsi(entryPoint);

        return this;
    }

    public PipelineBuilder FragmentShader(string path, string entryPoint) {
        _fragmentState->Module = ShaderUtils.LoadWgslModule(_device, path);
        _fragmentState->EntryPoint = CString.AllocAnsi(entryPoint);

        return this;
    }

    public PipelineBuilder MultisampleState(uint count, uint mask) {
        _renderPipelineDescriptor.MultiSample.Count = count;
        _renderPipelineDescriptor.MultiSample.Mask = mask;

        return this;
    }

    public PipelineBuilder PrimitiveState(WGPU.PrimitiveTopology topology, WGPU.CullMode cullMode, WGPU.FrontFace frontFace, WGPU.IndexFormat format) {
        _renderPipelineDescriptor.Primitive.Topology = topology;
        _renderPipelineDescriptor.Primitive.CullMode = cullMode;
        _renderPipelineDescriptor.Primitive.FrontFace = frontFace;
        _renderPipelineDescriptor.Primitive.StripIndexFormat = format;
        
        return this;
    }

    public WGPU.RenderPipeline Build() {
        WGPU.RenderPipeline renderPipeline = WGPU.RenderPipeline.Zero;
        
        if (_renderPipelineDescriptor.Layout == default) {
            _renderPipelineDescriptor.Layout = PipelineLayoutUtils.CreateEmpty(_device);
        }
        
        fixed (WGPU.RenderPipelineDescriptor* descriptorPtr = &_renderPipelineDescriptor) {
            renderPipeline = WGPU.DeviceCreateRenderPipeline(_device, descriptorPtr);
        }

        if (_renderPipelineDescriptor.Label != default) {
            Marshal.FreeHGlobal(new IntPtr((void*)_renderPipelineDescriptor.Label));
        }
        
        Marshal.FreeHGlobal(new IntPtr((void*)_fragmentState->EntryPoint));
        Marshal.FreeHGlobal(new IntPtr((void*)_fragmentState));
        
        Marshal.FreeHGlobal(_renderPipelineDescriptor.Vertex.EntryPoint);

        if (renderPipeline == WGPU.RenderPipeline.Zero) {
            throw new Exception("Failed to create pipeline");
        }
        
        return renderPipeline;
    }
}