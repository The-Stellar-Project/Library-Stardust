using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render; 

public unsafe class Renderer {
    private readonly RenderContext _context;
    
    public Renderer(RenderContext context) {
        _context = context;
    }

    public void Render() {
        var view = WGPU.SwapChainGetCurrentTextureView(_context.SwapChain);
        
        WGPU.CommandEncoderDescriptor commandEncoderDescriptor;
        commandEncoderDescriptor.Label = CString.Zero;
        var commandEncoder = WGPU.DeviceCreateCommandEncoder(_context.Device, &commandEncoderDescriptor);
        if (commandEncoder == WGPU.CommandEncoder.Zero) {
            throw new Exception("DeviceCreateCommandEncoder failed");
        }

        WGPU.Color clearColor;
        clearColor.R = 1.0;
        clearColor.G = 0.0;
        clearColor.B = 0.0;
        clearColor.A = 1.0;
        
        WGPU.RenderPassColorAttachment attachment;
        attachment.View = view;
        attachment.LoadOp = WGPU.LoadOp.Clear;
        attachment.StoreOp = WGPU.StoreOp.Store;
        attachment.ClearColor = clearColor;
        
        WGPU.RenderPassDescriptor renderPassDescriptor;
        renderPassDescriptor.ColorAttachmentCount = 1;
        renderPassDescriptor.ColorAttachments = &attachment;

        var renderEncoder = WGPU.CommandEncoderBeginRenderPass(commandEncoder, &renderPassDescriptor);
        WGPU.RenderPassEncoderEndPass(renderEncoder);

        WGPU.CommandBufferDescriptor commandBufferDescriptor;
        commandBufferDescriptor.Label = CString.Zero;
        var commandBuffer = WGPU.CommandEncoderFinish(commandEncoder, &commandBufferDescriptor);
        if (commandBuffer == WGPU.CommandBuffer.Zero) {
            throw new Exception("CommandEncoderFinish failed");
        }

        WGPU.QueueSubmit(_context.DeviceQueue, 1, &commandBuffer);
        
        WGPU.SwapChainPresent(_context.SwapChain);
    }
}