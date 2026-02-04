using MassRendererSystem.Data;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VATBakerSystem;
using static UnityEngine.GraphicsBuffer;

namespace MassRendererSystem
{
    /// <summary>
    /// High-performance GPU-based instanced renderer using Multi-Draw Indirect (MDI) rendering.
    /// Supports Vertex Animation Textures (VAT) for skeletal animation playback on GPU.
    /// </summary>
    public sealed class MassRenderer : IDisposable
    {
        private readonly RenderStaticData _mrData;
        private readonly MassRendererParams _mrParams;

        private Material _mrMaterial;
        private MaterialPropertyBlock _propertyBlock;

        private GraphicsBuffer _multiDrawCommandsBuffer;
        private RenderParams _rParams;

        private ComputeBuffer _instancesIdOffset;
        private ComputeBuffer _instanceDataBuffer;
        private ComputeBuffer _vatClipsDataBuffer;

        private bool _hasMaterial;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Gets the compute buffer containing per-instance data (transforms, animation state, etc.).
        /// </summary>
        public ComputeBuffer InstancesDataBuffer
        {
            get
            {
                if (_instanceDataBuffer == null)
                {
                    Debug.LogError($"[MassRenderer] InstanceDataBuffer not initialized! Call Initialize() first");
                }

                return _instanceDataBuffer;
            }
            private set
            {
                _instanceDataBuffer = value;
            }
        }

        /// <summary>
        /// Creates a new MassRenderer instance with auto-generated material based on shader type.
        /// </summary>
        /// <param name="data">Static render data containing meshes, textures, and VAT atlas.</param>
        /// <param name="mrParams">Renderer configuration parameters.</param>
        public MassRenderer(RenderStaticData data, MassRendererParams mrParams)
        {
            _mrData = data;
            _mrParams = mrParams;
        }

        /// <summary>
        /// Creates a new MassRenderer instance with a custom material.
        /// </summary>
        /// <param name="data">Static render data containing meshes, textures, and VAT atlas.</param>
        /// <param name="mrParams">Renderer configuration parameters.</param>
        /// <param name="mdiMaterial">Custom material for MDI rendering.</param>
        public MassRenderer(RenderStaticData data, MassRendererParams mrParams, Material mdiMaterial)
        {
            _mrData = data;
            _mrParams = mrParams;
            _mrMaterial = mdiMaterial;
        }

        /// <summary>
        /// Initializes the renderer by creating GPU buffers and setting up render parameters.
        /// Must be called before Render() or RebuildDrawCommands().
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the renderer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MassRenderer));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException("MassRenderer already initialized.");
            }

            if(_mrMaterial == null)
            {
                CreateMaterialFallback();
            }

            CreateBuffers();
            BuildRenderParams();
            UpdateInstancesBuffers();

            _isInitialized = true;
        }

        /// <summary>
        /// Renders all instances using GPU instancing with Multi-Draw Indirect.
        /// Should be called every frame.
        /// </summary>
        public void Render()
        {
            UpdateInstancesBuffers();

            Graphics.RenderMeshIndirect(_rParams, _mrData.MergedPrototypeMeshes, _multiDrawCommandsBuffer, _mrData.PrototypeMeshes.Count);
        }

        /// <summary>
        /// Rebuilds the indirect draw commands buffer with new instance counts per prototype mesh.
        /// Call this when the distribution of instances across mesh types changes.
        /// </summary>
        /// <param name="instanceCounts">Array of instance counts for each prototype mesh.</param>
        /// <exception cref="InvalidOperationException">Thrown if renderer is not initialized.</exception>
        public void RebuildDrawCommands(int[] instanceCounts)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("MassRenderer not initializes. Call Initialize() first.");
            }

            _multiDrawCommandsBuffer?.Release();

            SetCommandsBuffer(_mrData.PrototypeMeshes.Count, instanceCounts, _mrData.PrototypesData.mergedMeshData);

            BindMaterialData();
        }

        /// <summary>
        /// Sets a global transformation matrix applied to all instances.
        /// Useful for positioning the entire crowd/group in world space.
        /// </summary>
        /// <param name="globalMatrix">The world transformation matrix to apply.</param>
        public void SetGlobalTransform(Matrix4x4 globalMatrix)
        {
            if (_mrMaterial != null)
            {
                _mrMaterial.SetMatrix(MDIShaderIDs.GlobalTransformID, globalMatrix);
            }
        }

        private void BuildRenderParams()
        {
            RenderParams rParamas = new RenderParams(_mrMaterial);
            rParamas.worldBounds = _mrParams.RenderBounds;
            rParamas.matProps = _propertyBlock;
            rParamas.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rParamas.receiveShadows = true;

            _rParams = rParamas;
        }

        private void UpdateInstancesBuffers()
        {
            _mrMaterial.SetBuffer(MDIShaderIDs.InstanceDataBufferID, _instanceDataBuffer);
        }

        private void CreateBuffers()
        {
            _instanceDataBuffer = new ComputeBuffer(_mrParams.InstanceCount, Marshal.SizeOf(typeof(InstanceData)));
            _instancesIdOffset = new ComputeBuffer(_mrData.PrototypeMeshes.Count, Marshal.SizeOf(typeof(int)));

            if (_mrParams.IsVATEnable)
            {
                _vatClipsDataBuffer = new ComputeBuffer(_mrData.AtlasData.allClips.Length, Marshal.SizeOf(typeof(VATAtlasAnimationClip)));
            }
        }

        private void CreateMaterialFallback()
        {
            Shader shader = MDIShaderIDs.GetShader(_mrParams.ShaderType);

            _mrMaterial = new Material(shader);
            _hasMaterial = true;
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void BindMaterialData()
        {
            _mrMaterial.SetTexture(MDIShaderIDs.TextureSkinsID, _mrData.TextureSkins);

            _mrMaterial.SetBuffer(MDIShaderIDs.InstanceIdOffsetID, _instancesIdOffset);

            if (_mrParams.IsVATEnable)
            {
                _mrMaterial.EnableKeyword(MDIShaderIDs.KEYWORD_VAT_ON);

                _mrMaterial.SetTexture(MDIShaderIDs.PositionVATAtlasID, _mrData.AtlasData.PositionAtlas);
                _mrMaterial.SetTexture(MDIShaderIDs.NormalVATAtlasID, _mrData.AtlasData.NormalAtlas);

                _vatClipsDataBuffer.SetData(_mrData.AtlasData.allClips);
                _mrMaterial.SetBuffer(MDIShaderIDs.VATClipsBufferID, _vatClipsDataBuffer);
            }
            else
            {
                _mrMaterial.DisableKeyword(MDIShaderIDs.KEYWORD_VAT_ON);
            }
        }

        private void SetCommandsBuffer(int uniqMeshesCount, int[] meshesPerPrototype, PrototypesMeshSegment[] meshesData)
        {
            _multiDrawCommandsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, uniqMeshesCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            IndirectDrawIndexedArgs[] multiDrawCommands = new IndirectDrawIndexedArgs[uniqMeshesCount];

            PopulateIndirectArgs(meshesData, meshesPerPrototype, multiDrawCommands);

            _multiDrawCommandsBuffer.SetData(multiDrawCommands);
        }

        private void PopulateIndirectArgs(PrototypesMeshSegment[] segments, int[] meshesPerPrototype, GraphicsBuffer.IndirectDrawIndexedArgs[] args)
        {
            int[] indecesOffsets = CalculateInstanceOffsets(meshesPerPrototype);

            if (_instancesIdOffset != null)
            {
                _instancesIdOffset.SetData(indecesOffsets);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int meshIdx = seg.MeshIndex;

                args[i].baseVertexIndex = (uint)seg.BaseVertex;
                args[i].indexCountPerInstance = (uint)seg.IndexCount;
                args[i].startIndex = (uint)seg.StartIndex;
                args[i].instanceCount = (uint)meshesPerPrototype[meshIdx];
                args[i].startInstance = (uint)indecesOffsets[meshIdx];
            }
        }

        private int[] CalculateInstanceOffsets(int[] instanceCounts)
        {
            int count = instanceCounts.Length;
            int[] offsets = new int[count];

            int currentOffset = 0;

            for (int i = 0; i < count; i++)
            {
                offsets[i] = currentOffset;
                currentOffset += instanceCounts[i];
            }

            return offsets;
        }

        /// <summary>
        /// Releases all GPU resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _instanceDataBuffer?.Release();
            _instanceDataBuffer = null;

            _instancesIdOffset?.Release();
            _instancesIdOffset = null;

            _vatClipsDataBuffer?.Release();
            _vatClipsDataBuffer = null;

            _multiDrawCommandsBuffer?.Release();
            _multiDrawCommandsBuffer = null;


            if (_mrMaterial != null && _hasMaterial)
            {
                UnityEngine.Object.Destroy(_mrMaterial);
                _mrMaterial = null;
            }
        }
    }
}
