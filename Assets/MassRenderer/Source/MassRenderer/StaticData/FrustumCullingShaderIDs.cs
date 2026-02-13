using UnityEngine;

namespace MassRendererSystem.Data
{
    /// <summary>
    /// Cached shader property IDs for the FrustumCulling compute shader.
    /// Used to set parameters and buffers without repeated string hashing.
    /// </summary>
    public static class FrustumCullingShaderIDs
    {
        public static readonly int FrustumPlanesID = Shader.PropertyToID("_FrustumPlanes");
        public static readonly int BoundingSphereRadiusID = Shader.PropertyToID("_BoundingSphereRadius");
        public static readonly int TotalInstanceCountID = Shader.PropertyToID("_TotalInstanceCount");
        public static readonly int PrototypeCountID = Shader.PropertyToID("_PrototypeCount");
        public static readonly int CommandCountID = Shader.PropertyToID("_CommandCount");
        public static readonly int CullGlobalTransformID = Shader.PropertyToID("_CullGlobalTransform");
        public static readonly int GlobalScaleID = Shader.PropertyToID("_GlobalScale");
        public static readonly int CameraPositionID = Shader.PropertyToID("_CameraPosition");
        public static readonly int MaxRenderDistanceSqID = Shader.PropertyToID("_MaxRenderDistanceSq");
        public static readonly int InputBufferID = Shader.PropertyToID("_InputBuffer");
        public static readonly int OutputBufferID = Shader.PropertyToID("_OutputBuffer");
        public static readonly int VisibleCountPerPrototypeID = Shader.PropertyToID("_VisibleCountPerPrototype");
        public static readonly int PrototypeOffsetsID = Shader.PropertyToID("_PrototypeOffsets");
        public static readonly int SegmentToPrototypeID = Shader.PropertyToID("_SegmentToPrototype");
        public static readonly int StagingDrawArgsID = Shader.PropertyToID("_StagingDrawArgs");
        public static readonly int OriginalDrawArgsID = Shader.PropertyToID("_OriginalDrawArgs");
    }
}
