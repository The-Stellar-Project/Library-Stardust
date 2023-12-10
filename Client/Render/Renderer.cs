using Karma.CoreGPU;
using Karma.CoreInvoke;

using Stardust.Client.Render.Util;

namespace Stardust.Client.Render {
	public unsafe class Renderer {
		private readonly RenderContext       _context;
		private readonly WGPU.RenderPipeline _renderPipeline;

		public Renderer(RenderContext context) {
			this._context = context;

			this._renderPipeline = new PipelineBuilder(device: this._context.Device)
								   .VertexShader(path: "triangle.wgsl", entryPoint: "vs_main")
								   .FragmentShader(path: "triangle.wgsl", entryPoint: "fs_main")
								   .MultisampleState(count: 1, mask: 0xffffffff)
								   .Build();
		}

		public void Render() {
			var view = WGPU.SwapChainGetCurrentTextureView(this._context.SwapChain);

			WGPU.CommandEncoderDescriptor commandEncoderDescriptor;
			commandEncoderDescriptor.Label = CString.Zero;
			var commandEncoder = WGPU.DeviceCreateCommandEncoder(this._context.Device, &commandEncoderDescriptor);
			if (commandEncoder == WGPU.CommandEncoder.Zero)
				throw new Exception(message: "DeviceCreateCommandEncoder failed");

			WGPU.Color clearColor;
			clearColor.R = 1.0;
			clearColor.G = 0.0;
			clearColor.B = 0.0;
			clearColor.A = 1.0;

			WGPU.RenderPassColorAttachment attachment;
			attachment.View       = view;
			attachment.LoadOp     = WGPU.LoadOp.Clear;
			attachment.StoreOp    = WGPU.StoreOp.Store;
			attachment.ClearColor = clearColor;

			WGPU.RenderPassDescriptor renderPassDescriptor;
			renderPassDescriptor.ColorAttachmentCount = 1;
			renderPassDescriptor.ColorAttachments     = &attachment;

			var renderEncoder = WGPU.CommandEncoderBeginRenderPass(commandEncoder, &renderPassDescriptor);
			WGPU.RenderPassEncoderSetPipeline(renderEncoder, this._renderPipeline);
			WGPU.RenderPassEncoderEndPass(renderEncoder);

			WGPU.CommandBufferDescriptor commandBufferDescriptor;
			commandBufferDescriptor.Label = CString.Zero;
			var commandBuffer = WGPU.CommandEncoderFinish(commandEncoder, &commandBufferDescriptor);
			if (commandBuffer == WGPU.CommandBuffer.Zero) throw new Exception(message: "CommandEncoderFinish failed");

			WGPU.QueueSubmit(this._context.DeviceQueue, 1, &commandBuffer);

			WGPU.SwapChainPresent(this._context.SwapChain);
		}
	}
}