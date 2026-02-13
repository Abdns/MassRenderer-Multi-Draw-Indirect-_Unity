using System;
using System.Runtime.InteropServices;
using MassRendererSystem.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace MassRendererSystem
{
    /// <summary>
    /// Performs GPU-based frustum culling of instances using a compute shader.
    /// Discards invisible instances and produces updated IndirectDraw arguments
    /// so that only visible objects are rendered.
    /// </summary>
    public sealed class FrustumCuller : IDisposable
    {
        private const int CULL_BLOCK_SIZE = 128;
        private const int RESET_BLOCK_SIZE = 64;
        private const int ARGS_BLOCK_SIZE = 64;
        private const int ARGS_PER_COMMAND = 5;

        private const string KERNEL_RESET = "ResetCounters";
        private const string KERNEL_CULL = "FrustumCull";
        private const string KERNEL_UPDATE_ARGS = "UpdateDrawArgs";

        private readonly ComputeShader _cullingCS;
        private readonly int _totalInstanceCount;
        private readonly int _prototypeCount;
        private readonly float _boundingSphereRadius;
        private readonly float _maxRenderDistance;

        private int _kernelReset;
        private int _kernelCull;
        private int _kernelUpdateArgs;

        private int _resetThreadGroups;
        private int _cullThreadGroups;
        private int _argsThreadGroups;

        private GraphicsBuffer _visibleOutputBuffer;
        private GraphicsBuffer _visibleCountBuffer;
        private GraphicsBuffer _prototypeOffsetsBuffer;
        private GraphicsBuffer _segmentToPrototypeBuffer;

        private GraphicsBuffer _originalDrawArgsBuffer;

        private readonly Plane[] _frustumPlanesCachedRaw = new Plane[6];
        private readonly Vector4[] _frustumPlanesCached = new Vector4[6];
        private Matrix4x4 _globalTransform = Matrix4x4.identity;
        private float _globalScale = 1f;

        private int _commandCount;

        private bool _initialized;
        private bool _disposed;

        /// <summary>Buffer containing visible instance data after culling.</summary>
        public GraphicsBuffer VisibleOutputBuffer => _visibleOutputBuffer;

        /// <summary>
        /// Creates a new <see cref="FrustumCuller"/> instance.
        /// </summary>
        /// <param name="cullingShader">Compute shader that performs frustum culling.</param>
        /// <param name="totalInstanceCount">Total number of instances.</param>
        /// <param name="prototypeCount">Number of unique mesh prototypes.</param>
        /// <param name="boundingSphereRadius">Bounding sphere radius of the prototype mesh.</param>
        /// <param name="cullingShader">Compute shader that performs frustum culling.</param>
        /// <param name="totalInstanceCount">Total number of instances.</param>
        /// <param name="prototypeCount">Number of unique mesh prototypes.</param>
        /// <param name="boundingSphereRadius">Bounding sphere radius of the prototype mesh.</param>
        /// <param name="maxRenderDistance">Maximum render distance. 0 or negative = no distance culling.</param>
        public FrustumCuller(
            ComputeShader cullingShader,
            int totalInstanceCount,
            int prototypeCount,
            float boundingSphereRadius,
            float maxRenderDistance = 0f)
        {
            _cullingCS = cullingShader ?? throw new ArgumentNullException(nameof(cullingShader));
            _totalInstanceCount = totalInstanceCount;
            _prototypeCount = prototypeCount;
            _boundingSphereRadius = boundingSphereRadius;
            _maxRenderDistance = maxRenderDistance;
        }

        /// <summary>
        /// Initializes internal buffers and binds data to the compute shader.
        /// Must be called once before using <see cref="Cull"/>.
        /// </summary>
        /// <param name="sourceBuffer">Source buffer containing all instance data.</param>
        /// <param name="instanceCounts">Instance count for each prototype.</param>
        /// <param name="segments">Mesh segments describing draw commands.</param>
        /// <param name="cachedDrawCommands">Original IndirectDraw arguments.</param>
        public void Initialize(
            GraphicsBuffer sourceBuffer,
            int[] instanceCounts,
            PrototypesMeshSegment[] segments,
            GraphicsBuffer.IndirectDrawIndexedArgs[] cachedDrawCommands)
        {
            if (_initialized)
                throw new InvalidOperationException("FrustumCuller already initialized.");

            _kernelReset = _cullingCS.FindKernel(KERNEL_RESET);
            _kernelCull = _cullingCS.FindKernel(KERNEL_CULL);
            _kernelUpdateArgs = _cullingCS.FindKernel(KERNEL_UPDATE_ARGS);

            _resetThreadGroups = Mathf.CeilToInt((float)_prototypeCount / RESET_BLOCK_SIZE);
            _cullThreadGroups = Mathf.CeilToInt((float)_totalInstanceCount / CULL_BLOCK_SIZE);

            _commandCount = segments.Length;
            _argsThreadGroups = Mathf.CeilToInt((float)_commandCount / ARGS_BLOCK_SIZE);

            _visibleOutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,_totalInstanceCount,Marshal.SizeOf(typeof(InstanceData)));
            _visibleCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,_prototypeCount,sizeof(uint));
            _prototypeOffsetsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,_prototypeCount,sizeof(uint));

            uint[] offsets = CalculateOffsets(instanceCounts);
            _prototypeOffsetsBuffer.SetData(offsets);

            _segmentToPrototypeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,_commandCount,sizeof(uint));
            uint[] segToProto = new uint[_commandCount];
            for (int i = 0; i < _commandCount; i++)
            {
                segToProto[i] = (uint)segments[i].MeshIndex;
            }
            _segmentToPrototypeBuffer.SetData(segToProto);

            int totalUints = _commandCount * ARGS_PER_COMMAND;
            _originalDrawArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,totalUints,sizeof(uint));

            uint[] flatArgs = FlattenDrawArgs(cachedDrawCommands, _commandCount);
            _originalDrawArgsBuffer.SetData(flatArgs);

            _cullingCS.SetInt(FrustumCullingShaderIDs.TotalInstanceCountID, _totalInstanceCount);
            _cullingCS.SetInt(FrustumCullingShaderIDs.PrototypeCountID, _prototypeCount);
            _cullingCS.SetInt(FrustumCullingShaderIDs.CommandCountID, _commandCount);
            _cullingCS.SetFloat(FrustumCullingShaderIDs.BoundingSphereRadiusID, _boundingSphereRadius);

            BindBuffers(sourceBuffer);

            _initialized = true;
        }

        /// <summary>
        /// Sets the global transformation matrix used when computing instance world positions.
        /// Pre-computes the max scale factor on CPU to avoid per-instance computation on GPU.
        /// </summary>
        /// <param name="globalMatrix">Global transformation matrix.</param>
        public void SetGlobalTransform(Matrix4x4 globalMatrix)
        {
            _globalTransform = globalMatrix;
            _globalScale = ComputeMaxScale(globalMatrix);
        }

        /// Performs frustum culling for the given camera and writes updated draw arguments
        /// directly into the IndirectArgs buffer (no staging/copy step).
        /// </summary>
        /// <param name="camera">Camera whose frustum planes are used for culling.</param>
        /// <param name="drawArgsBuffer">Target IndirectDraw arguments buffer. Must have Structured | IndirectArguments targets.</param>
        public void Cull(Camera camera, GraphicsBuffer drawArgsBuffer)
        {
            if (!_initialized || _disposed) return;
            if (camera == null || drawArgsBuffer == null) return;

            ExtractFrustumPlanes(camera);

            _cullingCS.SetVectorArray(FrustumCullingShaderIDs.FrustumPlanesID, _frustumPlanesCached);
            _cullingCS.SetMatrix(FrustumCullingShaderIDs.CullGlobalTransformID, _globalTransform);
            _cullingCS.SetFloat(FrustumCullingShaderIDs.GlobalScaleID, _globalScale);

            Vector3 camPos = camera.transform.position;
            _cullingCS.SetVector(FrustumCullingShaderIDs.CameraPositionID, new Vector4(camPos.x, camPos.y, camPos.z, 0f));

            float maxDistSq = _maxRenderDistance > 0f ? _maxRenderDistance * _maxRenderDistance : 0f;
            _cullingCS.SetFloat(FrustumCullingShaderIDs.MaxRenderDistanceSqID, maxDistSq);

            _cullingCS.SetBuffer(_kernelUpdateArgs, FrustumCullingShaderIDs.StagingDrawArgsID, drawArgsBuffer);

            _cullingCS.Dispatch(_kernelReset, _resetThreadGroups, 1, 1);
            _cullingCS.Dispatch(_kernelCull, _cullThreadGroups, 1, 1);
            _cullingCS.Dispatch(_kernelUpdateArgs, _argsThreadGroups, 1, 1);
        }

        /// <summary>
        /// Records frustum culling dispatches into a CommandBuffer instead of
        /// executing them immediately. This avoids CPU-GPU sync points and allows
        /// the GPU to batch all compute work together.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record into.</param>
        /// <param name="camera">Camera whose frustum planes are used for culling.</param>
        /// <param name="drawArgsBuffer">Target IndirectDraw arguments buffer. Must have Structured | IndirectArguments targets.</param>
        public void DispatchCullingCmd(CommandBuffer cmd, Camera camera, GraphicsBuffer drawArgsBuffer)
        {
            if (!_initialized || _disposed) return;
            if (camera == null || drawArgsBuffer == null || cmd == null) return;

            ExtractFrustumPlanes(camera);

            cmd.SetComputeVectorArrayParam(_cullingCS, FrustumCullingShaderIDs.FrustumPlanesID, _frustumPlanesCached);
            cmd.SetComputeMatrixParam(_cullingCS, FrustumCullingShaderIDs.CullGlobalTransformID, _globalTransform);
            cmd.SetComputeFloatParam(_cullingCS, FrustumCullingShaderIDs.GlobalScaleID, _globalScale);

            Vector3 camPos = camera.transform.position;
            cmd.SetComputeVectorParam(_cullingCS, FrustumCullingShaderIDs.CameraPositionID, new Vector4(camPos.x, camPos.y, camPos.z, 0f));

            float maxDistSq = _maxRenderDistance > 0f ? _maxRenderDistance * _maxRenderDistance : 0f;
            cmd.SetComputeFloatParam(_cullingCS, FrustumCullingShaderIDs.MaxRenderDistanceSqID, maxDistSq);

            cmd.SetComputeBufferParam(_cullingCS, _kernelUpdateArgs, FrustumCullingShaderIDs.StagingDrawArgsID, drawArgsBuffer);

            cmd.DispatchCompute(_cullingCS, _kernelReset, _resetThreadGroups, 1, 1);
            cmd.DispatchCompute(_cullingCS, _kernelCull, _cullThreadGroups, 1, 1);
            cmd.DispatchCompute(_cullingCS, _kernelUpdateArgs, _argsThreadGroups, 1, 1);
        }

        /// <summary>
        /// Releases all GPU buffers created by this instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _visibleOutputBuffer?.Release();
            _visibleCountBuffer?.Release();
            _prototypeOffsetsBuffer?.Release();
            _segmentToPrototypeBuffer?.Release();
            _originalDrawArgsBuffer?.Release();

            _visibleOutputBuffer = null;
            _visibleCountBuffer = null;
            _prototypeOffsetsBuffer = null;
            _segmentToPrototypeBuffer = null;
            _originalDrawArgsBuffer = null;
        }

        /// <summary>
        /// Binds all buffers to the corresponding compute shader kernels.
        /// </summary>
        /// <summary>
        /// Binds all buffers to the corresponding compute shader kernels.
        /// Note: _StagingDrawArgs is bound per-frame in Cull() to the actual draw args buffer.
        /// </summary>
        private void BindBuffers(GraphicsBuffer sourceBuffer)
        {
            _cullingCS.SetBuffer(_kernelReset, FrustumCullingShaderIDs.VisibleCountPerPrototypeID, _visibleCountBuffer);

            _cullingCS.SetBuffer(_kernelCull, FrustumCullingShaderIDs.InputBufferID, sourceBuffer);
            _cullingCS.SetBuffer(_kernelCull, FrustumCullingShaderIDs.OutputBufferID, _visibleOutputBuffer);
            _cullingCS.SetBuffer(_kernelCull, FrustumCullingShaderIDs.VisibleCountPerPrototypeID, _visibleCountBuffer);
            _cullingCS.SetBuffer(_kernelCull, FrustumCullingShaderIDs.PrototypeOffsetsID, _prototypeOffsetsBuffer);

            _cullingCS.SetBuffer(_kernelUpdateArgs, FrustumCullingShaderIDs.VisibleCountPerPrototypeID, _visibleCountBuffer);
            _cullingCS.SetBuffer(_kernelUpdateArgs, FrustumCullingShaderIDs.SegmentToPrototypeID, _segmentToPrototypeBuffer);
            _cullingCS.SetBuffer(_kernelUpdateArgs, FrustumCullingShaderIDs.OriginalDrawArgsID, _originalDrawArgsBuffer);
        }

        /// <summary>
        /// Extracts six frustum planes from the camera and stores them in a cached array.
        /// Uses pre-allocated Plane[] to avoid GC allocations every frame.
        /// </summary>
        private void ExtractFrustumPlanes(Camera camera)
        {
            Matrix4x4 vpMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(vpMatrix, _frustumPlanesCachedRaw);

            for (int i = 0; i < 6; i++)
            {
                _frustumPlanesCached[i] = new Vector4(
                    _frustumPlanesCachedRaw[i].normal.x,
                    _frustumPlanesCachedRaw[i].normal.y,
                    _frustumPlanesCachedRaw[i].normal.z,
                    _frustumPlanesCachedRaw[i].distance);
            }
        }

        /// <summary>
        /// Converts an array of <see cref="GraphicsBuffer.IndirectDrawIndexedArgs"/> into a flat uint array
        /// for uploading to a GPU buffer.
        /// </summary>
        private static uint[] FlattenDrawArgs(GraphicsBuffer.IndirectDrawIndexedArgs[] args, int count)
        {
            uint[] flat = new uint[count * ARGS_PER_COMMAND];
            for (int i = 0; i < count; i++)
            {
                int offset = i * ARGS_PER_COMMAND;
                flat[offset + 0] = args[i].indexCountPerInstance;
                flat[offset + 1] = args[i].instanceCount;
                flat[offset + 2] = args[i].startIndex;
                flat[offset + 3] = args[i].baseVertexIndex;
                flat[offset + 4] = args[i].startInstance;
            }
            return flat;
        }

        /// <summary>
        /// Calculates output buffer offsets for each prototype based on instance counts.
        /// </summary>
        private static uint[] CalculateOffsets(int[] instanceCounts)
        {
            uint[] offsets = new uint[instanceCounts.Length];
            uint current = 0;
            for (int i = 0; i < instanceCounts.Length; i++)
            {
                offsets[i] = current;
                current += (uint)instanceCounts[i];
            }
            return offsets;
        }

        /// <summary>
        /// Computes the maximum axis scale from a transformation matrix on CPU.
        /// This avoids redundant per-instance computation in the compute shader.
        /// </summary>
        private static float ComputeMaxScale(Matrix4x4 m)
        {
            float sx = new Vector3(m.m00, m.m10, m.m20).magnitude;
            float sy = new Vector3(m.m01, m.m11, m.m21).magnitude;
            float sz = new Vector3(m.m02, m.m12, m.m22).magnitude;
            return Mathf.Max(sx, Mathf.Max(sy, sz));
        }
    }
}
