using MassRendererSystem.Data;
using MassRendererSystem.Utils;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Per-agent simulation data for crowd movement.
/// </summary>
public struct AgentData
{
    /// <summary>
    /// World position of the agent.
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// Movement direction (normalized).
    /// </summary>
    public Vector3 direction;

    /// <summary>
    /// Scale of the agent.
    /// </summary>
    public Vector3 scale;

    /// <summary>
    /// Movement speed in units per second.
    /// </summary>
    public float speed;
}

/// <summary>
/// GPU-based crowd simulation using compute shaders.
/// Updates agent positions and transforms on the GPU each frame.
/// </summary>
public class CrowdSimulation
{
    private const string KERNEL_NAME = "CSMain";
    private const int SIMULATION_BLOCK_SIZE = 128;

    private int _kernelIndex;
    private int _threadGroupSize;
    private readonly int _unitsCount;

    private Vector3 _boundSize = new Vector3(100, 10, 100);
    private Vector3 _boundPosition = Vector3.zero;

    private GraphicsBuffer _agentBuffer;
    private readonly GraphicsBuffer _instanceBuffer;
    private readonly RenderStaticData _data;
    private readonly ComputeShader _computeCS;
    public int[] InstanceCounts { get; private set; }

    public CrowdSimulation(int unitsCount, GraphicsBuffer instanceBuffer, RenderStaticData data, ComputeShader computeCS)
    {
        _unitsCount = unitsCount;
        _instanceBuffer = instanceBuffer;
        _computeCS = computeCS;
        _data = data;
    }

    public void Initialize()
    {
        _kernelIndex = _computeCS.FindKernel(KERNEL_NAME);

        _threadGroupSize = Mathf.CeilToInt((float)_unitsCount / SIMULATION_BLOCK_SIZE);

        _agentBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            _unitsCount,
            Marshal.SizeOf(typeof(AgentData)));

        FillBuffers();

        _computeCS.SetInt("_InstanceCount", _unitsCount);
        _computeCS.SetVector("_BoundCenter", _boundPosition);
        _computeCS.SetVector("_BoundSize", _boundSize);

        _computeCS.SetBuffer(_kernelIndex, "_AgentDataBufferWrite", _agentBuffer);
        _computeCS.SetBuffer(_kernelIndex, "_InstanceDataBufferWrite", _instanceBuffer);
    }

    public void Simulate()
    {
        _computeCS.SetFloat("_DeltaTime", Time.deltaTime);
        _computeCS.Dispatch(_kernelIndex, _threadGroupSize, 1, 1);
    }

    private void FillBuffers()
    {
        AgentData[] unitData = new AgentData[_unitsCount];
        InstanceData[] instanceData = new InstanceData[_unitsCount];

        int prototypesCount = _data.PrototypeMeshes.Count;
        int[] typeCounts = new int[prototypesCount];
        int remaining = _unitsCount;

        for (int i = 0; i < typeCounts.Length; i++)
        {
            typeCounts[i] = _unitsCount / typeCounts.Length;
            remaining -= typeCounts[i];
        }

        if (typeCounts.Length > 0)
        {
            typeCounts[0] += remaining;
        }

        InstanceCounts = typeCounts;

        int currentInstanceIndex = 0;

        for (int meshId = 0; meshId < typeCounts.Length; meshId++)
        {
            int countForThisMesh = typeCounts[meshId];

            for (int k = 0; k < countForThisMesh; k++)
            {

                unitData[currentInstanceIndex] = new AgentData
                {
                    position = MathUtils.GetSpreadPosition(_boundPosition, _boundSize),
                    direction = new Vector3(1, 0, 0),
                    scale = Vector3.one,
                    speed = 5
                };

                FillBufferStaticData(instanceData, currentInstanceIndex, meshId);

                currentInstanceIndex++;
            }
        }

        _agentBuffer.SetData(unitData);
        _instanceBuffer.SetData(instanceData);
    }

    private void FillBufferStaticData(InstanceData[] instancesData, int instanceIndex, int meshIndex)
    {
        var skinRange = _data.GetSkinTextureIndexRange(meshIndex);
        int skinIndex = Random.Range(skinRange.start, skinRange.end);

        var animRange = _data.GetAnimationIndexRange(meshIndex);
        int walkAnimIndex = Random.Range(animRange.start + 1, animRange.start + 1);

        instancesData[instanceIndex] = new InstanceData
        {
            MeshIndex = meshIndex,
            TextureSkinIndex = skinIndex,
            AnimationIndex = walkAnimIndex,
            AnimationSpeed = Random.Range(0.8f, 1.2f)
        };
    }

    public void ReleaseBuffers()
    {
        _agentBuffer?.Release();
        _agentBuffer = null;
    }
}
