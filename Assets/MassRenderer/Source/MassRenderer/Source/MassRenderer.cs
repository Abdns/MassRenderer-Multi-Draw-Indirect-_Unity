using MassRendererSystem.Data;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VATBakerSystem;

namespace MassRendererSystem
{
    /// <summary>
    /// High-performance mass renderer using GPU instancing with indirect draw calls.
    /// Supports Vertex Animation Textures (VAT) for skeletal animation playback on GPU.
    /// Optionally supports GPU-based per-instance frustum culling via compute shader.
    /// </summary>
    public sealed class MassRenderer : IDisposable
    {
        private readonly RenderStaticData _mrData;
        private readonly MassRendererParams _mrParams;

        private Material _mrMaterial;
        private MaterialPropertyBlock _propertyBlock;

        private GraphicsBuffer _multiDrawCommandsBuffer;
        private RenderParams _rParams;

        private GraphicsBuffer _instancesIdOffset;
        private GraphicsBuffer _instanceDataBuffer;
        private GraphicsBuffer _vatClipsDataBuffer;

        private FrustumCuller _frustumCuller;
        private Camera _cullingCamera;
        private Matrix4x4 _globalTransform = Matrix4x4.identity;

        private GraphicsBuffer.IndirectDrawIndexedArgs[] _cachedDrawCommands;
        private int[] _cachedOffsets;

        private bool _hasMaterial;
        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Gets the graphics buffer containing per-instance data (transforms, animation state, etc.).
        /// </summary>
        public GraphicsBuffer InstancesDataBuffer
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
            _mrData = data ?? throw new ArgumentNullException(nameof(data));
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
            _mrData = data ?? throw new ArgumentNullException(nameof(data));
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

            if (_mrMaterial == null)
            {
                CreateMaterialFallback();
            }

            CreateBuffers();
            BuildRenderParams();

            _cullingCamera = _mrParams.CullingCamera;

            _isInitialized = true;
        }

        /// <summary>
        /// Renders all instances using GPU instancing with Multi-Draw Indirect.
        /// When frustum culling is enabled, first dispatches the culling compute shader,
        /// then renders only visible instances.
        /// </summary>
        public void Render()
        {
            if (!_isInitialized || _isDisposed)
            {
                return;
            }

            if (_frustumCuller != null)
            {
                Camera cam = _cullingCamera != null ? _cullingCamera : Camera.main;

                if (cam != null)
                {
                    _frustumCuller.Cull(cam, _multiDrawCommandsBuffer);
                }
                else
                {
                    UpdateInstancesBuffers();
                }
            }
            else
            {
                UpdateInstancesBuffers();
            }

            Graphics.RenderMeshIndirect(
                _rParams,
                _mrData.MergedPrototypeMeshes,
                _multiDrawCommandsBuffer,
                _mrData.PrototypeMeshes.Count);
        }

        /// <summary>
        /// Rebuilds the indirect draw commands buffer with new instance counts per prototype mesh.
        /// Call this when the distribution of instances across mesh types changes.
        /// Also initializes the frustum culler if culling is enabled.
        /// </summary>
        /// <param name="instanceCounts">Array of instance counts for each prototype mesh.</param>
        public void RebuildDrawCommands(int[] instanceCounts)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("MassRenderer not initialized. Call Initialize() first.");
            }

            _multiDrawCommandsBuffer?.Release();

            PrototypesMeshSegment[] segments = _mrData.PrototypesData.mergedMeshData;

            SetCommandsBuffer(_mrData.PrototypeMeshes.Count, instanceCounts, segments);

            BindMaterialData();

            if (_mrParams.IsFrustumCullingEnabled && _mrParams.FrustumCullingShader != null)
            {
                InitializeFrustumCuller(instanceCounts, segments);
            }
        }

        /// <summary>
        /// Sets a global transformation matrix applied to all instances.
        /// Useful for positioning the entire crowd/group in world space.
        /// Also updates the frustum culler's global transform for accurate culling.
        /// </summary>
        /// <param name="globalMatrix">The world transformation matrix to apply.</param>
        public void SetGlobalTransform(Matrix4x4 globalMatrix)
        {
            _globalTransform = globalMatrix;

            if (_mrMaterial != null)
            {
                _mrMaterial.SetMatrix(MDIShaderIDs.GlobalTransformID, globalMatrix);
            }

            _frustumCuller?.SetGlobalTransform(globalMatrix);
        }

        /// <summary>
        /// Sets the camera used for frustum culling.
        /// If not set, Camera.main is used as fallback.
        /// </summary>
        /// <param name="camera">Camera to extract frustum planes from.</param>
        public void SetCullingCamera(Camera camera)
        {
            _cullingCamera = camera;
        }

        /// <summary>
        /// Builds the RenderParams structure for use with RenderMeshIndirect.
        /// </summary>
        private void BuildRenderParams()
        {
            RenderParams rParams = new RenderParams(_mrMaterial)
            {
                worldBounds = _mrParams.RenderBounds,
                matProps = _propertyBlock,
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true
            };

            _rParams = rParams;
        }

        /// <summary>
        /// Updates material buffer bindings for instance data.
        /// Used when frustum culling is NOT active.
        /// </summary>
        private void UpdateInstancesBuffers()
        {
            _mrMaterial.SetBuffer(MDIShaderIDs.InstanceDataBufferID, _instanceDataBuffer);
        }

        /// <summary>
        /// Creates all required GPU compute buffers.
        /// </summary>
        private void CreateBuffers()
        {
            int prototypeCount = _mrData.PrototypeMeshes.Count;

            _instanceDataBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                _mrParams.InstanceCount,
                Marshal.SizeOf(typeof(InstanceData)));

            _instancesIdOffset = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                prototypeCount,
                Marshal.SizeOf(typeof(int)));

            _cachedDrawCommands = new GraphicsBuffer.IndirectDrawIndexedArgs[prototypeCount];
            _cachedOffsets = new int[prototypeCount];

            if (_mrParams.IsVATEnable)
            {
                _vatClipsDataBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured,
                    _mrData.AtlasData.allClips.Length,
                    Marshal.SizeOf(typeof(VATAtlasAnimationClip)));
            }
        }

        /// <summary>
        /// Creates a fallback material when none is provided in the constructor.
        /// </summary>
        private void CreateMaterialFallback()
        {
            Shader shader = MDIShaderIDs.GetShader(_mrParams.ShaderType);

            _mrMaterial = new Material(shader);
            _hasMaterial = true;
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Binds texture and buffer data to the material.
        /// </summary>
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

        /// <summary>
        /// Initializes the frustum culler with current instance data.
        /// </summary>
        private void InitializeFrustumCuller(int[] instanceCounts, PrototypesMeshSegment[] segments)
        {
            _frustumCuller?.Dispose();

            _frustumCuller = new FrustumCuller(
                _mrParams.FrustumCullingShader,
                _mrParams.InstanceCount,
                _mrData.PrototypeMeshes.Count,
                _mrParams.BoundingSphereRadius,
                _mrParams.MaxRenderDistance);

            _frustumCuller.Initialize(_instanceDataBuffer, instanceCounts, segments, _cachedDrawCommands);
            _frustumCuller.SetGlobalTransform(_globalTransform);

            _mrMaterial.SetBuffer(MDIShaderIDs.InstanceDataBufferID, _frustumCuller.VisibleOutputBuffer);
        }

        /// <summary>
        /// Creates and populates the indirect draw commands buffer.
        /// Uses cached arrays to avoid runtime allocations.
        /// When frustum culling is enabled, the buffer also has Structured target
        /// so that the compute shader can write directly into it (no staging/copy).
        /// </summary>
        /// <param name="uniqMeshesCount">Number of unique mesh prototypes.</param>
        /// <param name="meshesPerPrototype">Array of instance counts per prototype.</param>
        /// <param name="meshesData">Mesh segment data for merged mesh.</param>
        private void SetCommandsBuffer(int uniqMeshesCount, int[] meshesPerPrototype, PrototypesMeshSegment[] meshesData)
        {
            bool needsCulling = _mrParams.IsFrustumCullingEnabled && _mrParams.FrustumCullingShader != null;

            GraphicsBuffer.Target target = needsCulling
                ? GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured
                : GraphicsBuffer.Target.IndirectArguments;

            _multiDrawCommandsBuffer = new GraphicsBuffer(
                target,
                needsCulling ? uniqMeshesCount * (GraphicsBuffer.IndirectDrawIndexedArgs.size / sizeof(uint)) : uniqMeshesCount,
                needsCulling ? sizeof(uint) : GraphicsBuffer.IndirectDrawIndexedArgs.size);

            PopulateIndirectArgs(meshesData, meshesPerPrototype, _cachedDrawCommands);

            if (needsCulling)
            {
                uint[] flatArgs = new uint[uniqMeshesCount * 5];
                for (int i = 0; i < uniqMeshesCount; i++)
                {
                    int offset = i * 5;
                    flatArgs[offset + 0] = _cachedDrawCommands[i].indexCountPerInstance;
                    flatArgs[offset + 1] = _cachedDrawCommands[i].instanceCount;
                    flatArgs[offset + 2] = _cachedDrawCommands[i].startIndex;
                    flatArgs[offset + 3] = _cachedDrawCommands[i].baseVertexIndex;
                    flatArgs[offset + 4] = _cachedDrawCommands[i].startInstance;
                }
                _multiDrawCommandsBuffer.SetData(flatArgs);
            }
            else
            {
                _multiDrawCommandsBuffer.SetData(_cachedDrawCommands);
            }
        }

        /// <summary>
        /// Populates the indirect draw arguments for each mesh prototype.
        /// Uses cached offset array to avoid runtime allocations.
        /// </summary>
        /// <param name="segments">Mesh segment information.</param>
        /// <param name="meshesPerPrototype">Instance counts per prototype.</param>
        /// <param name="args">Output array of indirect draw arguments.</param>
        private void PopulateIndirectArgs(
            PrototypesMeshSegment[] segments,
            int[] meshesPerPrototype,
            GraphicsBuffer.IndirectDrawIndexedArgs[] args)
        {
            CalculateOffsets(meshesPerPrototype, _cachedOffsets);

            if (_instancesIdOffset != null)
            {
                _instancesIdOffset.SetData(_cachedOffsets);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int meshIdx = seg.MeshIndex;

                args[i].baseVertexIndex = (uint)seg.BaseVertex;
                args[i].indexCountPerInstance = (uint)seg.IndexCount;
                args[i].startIndex = (uint)seg.StartIndex;
                args[i].instanceCount = (uint)meshesPerPrototype[meshIdx];
                args[i].startInstance = (uint)_cachedOffsets[meshIdx];
            }
        }

        /// <summary>
        /// Calculates instance offsets.
        /// </summary>
        /// <param name="instanceCounts">Input array of instance counts.</param>
        /// <param name="offsets">Output array to fill with offsets.</param>
        private static void CalculateOffsets(int[] instanceCounts, int[] offsets)
        {
            int currentOffset = 0;
            for (int i = 0; i < instanceCounts.Length; i++)
            {
                offsets[i] = currentOffset;
                currentOffset += instanceCounts[i];
            }
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

            _frustumCuller?.Dispose();
            _frustumCuller = null;

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
