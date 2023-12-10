using Silk.NET.WebGPU;

using Stardust.Client.Render.Util;

namespace Stardust.Client.Render {
	public unsafe class Renderer {
		private readonly RenderContext  _context;
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
			var surfaceTexture = new SurfaceTexture();
			fixed (Surface* surface = &this._context.Surface)
				WebGPU.GetApi().SurfaceGetCurrentTexture(surface: surface, surfaceTexture: &surfaceTexture);

			TextureViewDescriptor textureViewDescriptor;
			// textureViewDescriptor.Label = CString.Zero; TODO: ...
			var textureView = WebGPU.GetApi()
									.TextureCreateView(texture: surfaceTexture.Texture,
													   descriptor: &textureViewDescriptor);

			CommandEncoderDescriptor commandEncoderDescriptor;
			// commandEncoderDescriptor.Label = CString.Zero; TODO: ...
			CommandEncoder* commandEncoder = null;
			fixed (Device* device = &this._context.Device)
				commandEncoder =
					WebGPU.GetApi().DeviceCreateCommandEncoder(device: device, descriptor: &commandEncoderDescriptor);
			if (commandEncoder == null) throw new Exception(message: "DeviceCreateCommandEncoder failed");

			Color clearColor;
			clearColor.R = 1.0;
			clearColor.G = 0.0;
			clearColor.B = 0.0;
			clearColor.A = 1.0;

			RenderPassColorAttachment attachment;
			attachment.View       = textureView;
			attachment.LoadOp     = LoadOp.Clear;
			attachment.StoreOp    = StoreOp.Store;
			attachment.ClearValue = clearColor;

			RenderPassDescriptor renderPassDescriptor;
			renderPassDescriptor.ColorAttachmentCount = 1;
			renderPassDescriptor.ColorAttachments     = &attachment;

			var renderEncoder = WebGPU.GetApi()
									  .CommandEncoderBeginRenderPass(commandEncoder: commandEncoder,
																	 descriptor: &renderPassDescriptor);
			fixed (RenderPipeline* renderPipeline = &this._renderPipeline)
				WebGPU.GetApi()
					  .RenderPassEncoderSetPipeline(renderPassEncoder: renderEncoder, pipeline: renderPipeline);

			WebGPU.GetApi().RenderPassEncoderEnd(renderPassEncoder: renderEncoder);

			CommandBufferDescriptor commandBufferDescriptor;
			// commandBufferDescriptor.Label = CString.Zero; TODO: ...
			CommandBuffer* commandBuffer = null;
			commandBuffer = WebGPU.GetApi()
								  .CommandEncoderFinish(commandEncoder: commandEncoder,
														descriptor: &commandBufferDescriptor);
			if (commandBuffer == null) throw new Exception(message: "CommandEncoderFinish failed");

			fixed (Queue* deviceQueue = &this._context.DeviceQueue)
				WebGPU.GetApi().QueueSubmit(queue: deviceQueue, commandCount: 1, commands: &commandBuffer);
			fixed (Surface* surface = &this._context.Surface) WebGPU.GetApi().SurfacePresent(surface: surface);
		}
	}
}