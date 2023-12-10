using Silk.NET.WebGPU;

using Stardust.Client.Render.Util;

namespace Stardust.Client.Render {
	public unsafe class Renderer {
		private readonly RenderContext       _context;
		private readonly RenderPipeline _renderPipeline;

		public Renderer(RenderContext context) {
			this._context = context;

			this._renderPipeline = new PipelineBuilder(device: this._context.Device)
								   .VertexShader(path: "triangle.wgsl", entryPoint: "vs_main")
								   .FragmentShader(path: "triangle.wgsl", entryPoint: "fs_main")
								   .MultisampleState(count: 1, mask: 0xffffffff)
								   .Build();
		}

		public void Render() {
			SurfaceTexture* surfaceTexture;
			var view = WebGPU.GetApi().SurfaceGetCurrentTexture(this._context.Surface, surfaceTexture);

			CommandEncoderDescriptor commandEncoderDescriptor;
			commandEncoderDescriptor.Label = CString.Zero;
			CommandEncoder* commandEncoder = null;
			commandEncoder = WebGPU.GetApi().DeviceCreateCommandEncoder(this._context.Device, &commandEncoderDescriptor);
			if (commandEncoder == null)
				throw new Exception(message: "DeviceCreateCommandEncoder failed");

			Color clearColor;
			clearColor.R = 1.0;
			clearColor.G = 0.0;
			clearColor.B = 0.0;
			clearColor.A = 1.0;

			RenderPassColorAttachment attachment;
			attachment.View       = view;
			attachment.LoadOp     = LoadOp.Clear;
			attachment.StoreOp    = StoreOp.Store;
			attachment.ClearValue = clearColor;

			RenderPassDescriptor renderPassDescriptor;
			renderPassDescriptor.ColorAttachmentCount = 1;
			renderPassDescriptor.ColorAttachments     = &attachment;

			var renderEncoder = WebGPU.GetApi().CommandEncoderBeginRenderPass(commandEncoder, &renderPassDescriptor);
			fixed (RenderPipeline* renderPipeline = &this._renderPipeline) {
				WebGPU.GetApi().RenderPassEncoderSetPipeline(renderEncoder, renderPipeline);
			}
			WebGPU.GetApi().RenderPassEncoderEnd(renderEncoder);

			CommandBufferDescriptor commandBufferDescriptor;
			commandBufferDescriptor.Label = CString.Zero;
			CommandBuffer* commandBuffer = null;
			commandBuffer = WebGPU.GetApi().CommandEncoderFinish(commandEncoder, &commandBufferDescriptor);
			if (commandBuffer == null) throw new Exception(message: "CommandEncoderFinish failed");
			
			WebGPU.GetApi().QueueSubmit(this._context.DeviceQueue, 1, &commandBuffer);
			WebGPU.GetApi().SurfacePresent(this._context.Surface);
		}
	}
}