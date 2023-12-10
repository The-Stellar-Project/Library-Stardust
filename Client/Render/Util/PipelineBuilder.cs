using System.Runtime.InteropServices;

using Karma.CoreGPU;
using Karma.CoreInvoke;

namespace Stardust.Client.Render.Util {
	public unsafe class PipelineBuilder {
		private readonly WGPU.Device _device;

		private readonly WGPU.FragmentState*           _fragmentState;
		private          WGPU.PipelineLayout           _pipelineLayout;
		private          WGPU.RenderPipelineDescriptor _renderPipelineDescriptor;

		public PipelineBuilder(WGPU.Device device) {
			this._device = device;

			this._fragmentState = (WGPU.FragmentState*)Marshal.AllocHGlobal(cb: sizeof(WGPU.FragmentState)).ToPointer();
			this._renderPipelineDescriptor.Fragment = this._fragmentState;
		}

		public PipelineBuilder Name(string name) {
			this._renderPipelineDescriptor.Label = CString.AllocAnsi(s: name);

			return this;
		}

		public PipelineBuilder PipelineLayout(WGPU.PipelineLayout pipelineLayout) {
			this._pipelineLayout = pipelineLayout;
			return this;
		}

		public PipelineBuilder VertexShader(string path, string entryPoint) {
			this._renderPipelineDescriptor.Vertex.Module = ShaderUtils.LoadWgslModule(device: this._device, path: path);
			this._renderPipelineDescriptor.Vertex.EntryPoint = CString.AllocAnsi(s: entryPoint);

			return this;
		}

		public PipelineBuilder FragmentShader(string path, string entryPoint) {
			this._fragmentState->Module     = ShaderUtils.LoadWgslModule(device: this._device, path: path);
			this._fragmentState->EntryPoint = CString.AllocAnsi(s: entryPoint);

			return this;
		}

		public PipelineBuilder MultisampleState(uint count, uint mask) {
			this._renderPipelineDescriptor.MultiSample.Count = count;
			this._renderPipelineDescriptor.MultiSample.Mask  = mask;

			return this;
		}

		public PipelineBuilder PrimitiveState(
			WGPU.PrimitiveTopology topology,
			WGPU.CullMode          cullMode,
			WGPU.FrontFace         frontFace,
			WGPU.IndexFormat       format
		) {
			this._renderPipelineDescriptor.Primitive.Topology         = topology;
			this._renderPipelineDescriptor.Primitive.CullMode         = cullMode;
			this._renderPipelineDescriptor.Primitive.FrontFace        = frontFace;
			this._renderPipelineDescriptor.Primitive.StripIndexFormat = format;

			return this;
		}

		public WGPU.RenderPipeline Build() {
			var renderPipeline = WGPU.RenderPipeline.Zero;

			if (this._renderPipelineDescriptor.Layout == default)
				this._renderPipelineDescriptor.Layout = PipelineLayoutUtils.CreateEmpty(device: this._device);

			fixed (WGPU.RenderPipelineDescriptor* descriptorPtr =
					   &this._renderPipelineDescriptor)
				renderPipeline = WGPU.DeviceCreateRenderPipeline(this._device, descriptorPtr);

			if (this._renderPipelineDescriptor.Label != default)
				Marshal.FreeHGlobal(hglobal: new nint(value: (void*)this._renderPipelineDescriptor.Label));

			Marshal.FreeHGlobal(hglobal: new nint(value: (void*)this._fragmentState->EntryPoint));
			Marshal.FreeHGlobal(hglobal: new nint(value: this._fragmentState));

			Marshal.FreeHGlobal(hglobal: this._renderPipelineDescriptor.Vertex.EntryPoint);

			if (renderPipeline == WGPU.RenderPipeline.Zero) throw new Exception(message: "Failed to create pipeline");

			return renderPipeline;
		}
	}
}