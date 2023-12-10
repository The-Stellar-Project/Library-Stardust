using System.Runtime.InteropServices;

using Silk.NET.WebGPU;

namespace Stardust.Client.Render.Util {
	public unsafe class PipelineBuilder {
		private readonly Device _device;

		private readonly FragmentState*           _fragmentState;
		private          PipelineLayout           _pipelineLayout;
		private          RenderPipelineDescriptor _renderPipelineDescriptor;

		public PipelineBuilder(Device device) {
			this._device = device;

			this._fragmentState = (FragmentState*)Marshal.AllocHGlobal(cb: sizeof(FragmentState)).ToPointer();
			this._renderPipelineDescriptor.Fragment = this._fragmentState;
		}

		public PipelineBuilder Name(string name) {
			this._renderPipelineDescriptor.Label = (byte*)Marshal.StringToHGlobalAnsi(s: name).ToPointer();
			return this;
		}

		public PipelineBuilder PipelineLayout(PipelineLayout pipelineLayout) {
			this._pipelineLayout = pipelineLayout;
			return this;
		}

		public PipelineBuilder VertexShader(string path, string entryPoint) {
			this._renderPipelineDescriptor.Vertex.Module = ShaderUtils.LoadWgslModule(device: this._device, path: path);
			this._renderPipelineDescriptor.Vertex.EntryPoint =
				(byte*)Marshal.StringToHGlobalAnsi(s: entryPoint).ToPointer();
			return this;
		}

		public PipelineBuilder FragmentShader(string path, string entryPoint) {
			this._fragmentState->Module     = ShaderUtils.LoadWgslModule(device: this._device, path: path);
			this._fragmentState->EntryPoint = (byte*)Marshal.StringToHGlobalAnsi(s: entryPoint).ToPointer();

			return this;
		}

		public PipelineBuilder MultisampleState(uint count, uint mask) {
			this._renderPipelineDescriptor.Multisample.Count = count;
			this._renderPipelineDescriptor.Multisample.Mask  = mask;
			return this;
		}

		public PipelineBuilder PrimitiveState(
			PrimitiveTopology topology,
			CullMode          cullMode,
			FrontFace         frontFace,
			IndexFormat       format
		) {
			this._renderPipelineDescriptor.Primitive.Topology         = topology;
			this._renderPipelineDescriptor.Primitive.CullMode         = cullMode;
			this._renderPipelineDescriptor.Primitive.FrontFace        = frontFace;
			this._renderPipelineDescriptor.Primitive.StripIndexFormat = format;
			return this;
		}

		public RenderPipeline Build() {
			RenderPipeline* renderPipeline = null;

			if (this._renderPipelineDescriptor.Layout == default)
				fixed (Device* device = &this._device)
					this._renderPipelineDescriptor.Layout = PipelineLayoutUtils.CreateEmpty(device: *device);

			fixed (RenderPipelineDescriptor* descriptorPtr =
					   &this._renderPipelineDescriptor)
			fixed (Device* device = &this._device)
				renderPipeline = WebGPU.GetApi().DeviceCreateRenderPipeline(device: device, descriptor: descriptorPtr);

			if (this._renderPipelineDescriptor.Label != default)
				Marshal.FreeHGlobal(hglobal: new nint(value: this._renderPipelineDescriptor.Label));

			Marshal.FreeHGlobal(hglobal: new nint(value: this._fragmentState->EntryPoint));
			Marshal.FreeHGlobal(hglobal: new nint(value: this._fragmentState));

			Marshal.FreeHGlobal(hglobal: *this._renderPipelineDescriptor.Vertex.EntryPoint);

			if (renderPipeline == null) throw new Exception(message: "Failed to create pipeline");

			return *renderPipeline;
		}
	}
}