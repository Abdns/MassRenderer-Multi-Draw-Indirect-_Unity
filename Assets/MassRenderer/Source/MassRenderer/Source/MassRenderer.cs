using MassRendererSystem.Data;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VATBakerSystem;
using static UnityEngine.GraphicsBuffer;

namespace MassRendererSystem
{
    public sealed class MassRenderer : IDisposable
    {
        private readonly RenderStaticData _data;
        private readonly MassRendererParams _params;

        private Material _mdiMaterial;
        private MaterialPropertyBlock _propertyBlock;

        private GraphicsBuffer _multiDrawCommandsBuffer;
        private RenderParams _rParamas;

        private ComputeBuffer _instancesIdOffset;
        private ComputeBuffer _instanceDataBuffer;
        private ComputeBuffer _vatClipsDataBuffer;

        private bool _initialized;
        private bool _disposed;

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


        public MassRenderer(RenderStaticData data, MassRendererParams mrParams)
        {
            _data = data;
            _params = mrParams;
        }

        public MassRenderer(RenderStaticData data, MassRendererParams mrParams, Material mdiMaterial)
        {
            _data = data;
            _params = mrParams;
            _mdiMaterial = mdiMaterial;
        }


        public void Initialize()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MassRenderer));
            }

            if (_initialized)
            {
                throw new InvalidOperationException("MassRenderer already initialized.");
            }

            if(_mdiMaterial == null)
            {
                CreateMaterialFallback();
            }

            CreateBuffers();
            BuildRenderParams();

            _initialized = true;
        }

        public void Render()
        {
            UpdateInstancesBuffers();

            Graphics.RenderMeshIndirect(_rParamas, _data.MergedPrototypeMeshes, _multiDrawCommandsBuffer, _data.PrototypeMeshes.Count);
        }


        public void RebuildDrawCommands(int[] instanceCounts)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("MassRenderer not initializes. Call Initialize() first.");
            }

            _multiDrawCommandsBuffer?.Release();

            SetCommandsBuffer(_data.PrototypeMeshes.Count, instanceCounts, _data.PrototypesData.mergedMeshData);

            BindMaterialData();
        }

        public void SetGlobalTransform(Matrix4x4 globalMatrix)
        {
            if (_mdiMaterial != null)
            {
                _mdiMaterial.SetMatrix(MDIShaderIDs.GlobalTransformID, globalMatrix);
            }
        }

        private void BuildRenderParams()
        {
            RenderParams rParamas = new RenderParams(_mdiMaterial);
            rParamas.worldBounds = _params.RenderBounds;
            rParamas.matProps = _propertyBlock;
            rParamas.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rParamas.receiveShadows = true;

            _rParamas = rParamas;
        }

        private void UpdateInstancesBuffers()
        {
            _mdiMaterial.SetBuffer(MDIShaderIDs.InstanceDataBufferID, _instanceDataBuffer);
        }

        private void CreateBuffers()
        {
            _instanceDataBuffer = new ComputeBuffer(_params.InstanceCount, Marshal.SizeOf(typeof(InstanceData)));
            _instancesIdOffset = new ComputeBuffer(_data.PrototypeMeshes.Count, Marshal.SizeOf(typeof(int)));

            if (_params.IsVATEnable)
            {
                _vatClipsDataBuffer = new ComputeBuffer(_data.AtlasData.allClips.Length, Marshal.SizeOf(typeof(VATAtlasAnimationClip)));
            }
        }

        private void CreateMaterialFallback()
        {
            Shader shader = MDIShaderIDs.GetShader(_params.ShaderType);

            _mdiMaterial = new Material(shader);
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void BindMaterialData()
        {
            _mdiMaterial.SetTexture(MDIShaderIDs.TextureSkinsID, _data.TextureSkins);

            _mdiMaterial.SetBuffer(MDIShaderIDs.InstanceIdOffsetID, _instancesIdOffset);

            if (_params.IsVATEnable)
            {
                _mdiMaterial.EnableKeyword(MDIShaderIDs.KEYWORD_VAT_ON);

                _mdiMaterial.SetTexture(MDIShaderIDs.PositionVATAtlasID, _data.AtlasData.PositionAtlas);
                _mdiMaterial.SetTexture(MDIShaderIDs.NormalVATAtlasID, _data.AtlasData.NormalAtlas);

                _vatClipsDataBuffer.SetData(_data.AtlasData.allClips);
                _mdiMaterial.SetBuffer(MDIShaderIDs.VATClipsBufferID, _vatClipsDataBuffer);
            }
            else
            {
                _mdiMaterial.DisableKeyword(MDIShaderIDs.KEYWORD_VAT_ON);
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _instanceDataBuffer?.Release();
            _instancesIdOffset?.Release();
            _vatClipsDataBuffer?.Release();
            _multiDrawCommandsBuffer?.Release(); 


            if (_mdiMaterial != null)
            {
                UnityEngine.Object.Destroy(_mdiMaterial);
                _mdiMaterial = null;
            }
        }
    }
}
