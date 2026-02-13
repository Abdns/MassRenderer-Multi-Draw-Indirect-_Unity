using MassRendererSystem.Data;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Static grass/foliage placement simulation.
/// Distributes instances in a grid pattern with random jitter, rotation, and scale.
/// </summary>
public class GrassSimulation
{
    private readonly RenderStaticData _data;
    private readonly GraphicsBuffer _unitBuffer;
    private readonly int _totalUnits;

    private readonly Vector2 _areaSize;
    private readonly float _jitter;

    public int[] InstanceCounts { get; private set; }

    public GrassSimulation(RenderStaticData data, GraphicsBuffer unitBuffer, int unitCount, Vector2 areaSize, float jitter)
    {
        _data = data;
        _unitBuffer = unitBuffer;
        _totalUnits = unitCount;
        _areaSize = areaSize;
        _jitter = jitter;
    }

    public void InitBuffers()
    {
        InstanceData[] instancesData = new InstanceData[_totalUnits];

        int prototypesCount = _data.PrototypeMeshes.Count;
        int[] typeCounts = new int[prototypesCount];

        if (prototypesCount > 0)
        {
            int baseCount = _totalUnits / prototypesCount;
            int remaining = _totalUnits;
            for (int i = 0; i < prototypesCount; i++)
            {
                typeCounts[i] = baseCount;
                remaining -= baseCount;
            }
            typeCounts[0] += remaining;
        }
        InstanceCounts = typeCounts;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(_totalUnits));
        int rows = Mathf.CeilToInt((float)_totalUnits / columns);

        float cellWidth = _areaSize.x / columns;
        float cellDepth = _areaSize.y / rows;

        float startX = -_areaSize.x * 0.5f + cellWidth * 0.5f;
        float startZ = -_areaSize.y * 0.5f + cellDepth * 0.5f;

        int globalIndex = 0;

        for (int meshId = 0; meshId < prototypesCount; meshId++)
        {
            int count = typeCounts[meshId];

            for (int k = 0; k < count; k++)
            {
                int currentRow = globalIndex / columns;
                int currentCol = globalIndex % columns;

                float rndX = Random.Range(-_jitter, _jitter);
                float rndZ = Random.Range(-_jitter, _jitter);

                float xPos = startX + (currentCol * cellWidth) + rndX;
                float zPos = startZ + (currentRow * cellDepth) + rndZ;

                Vector3 position = new Vector3(xPos, 0, zPos);

                float rndRot = Random.Range(0f, 360f);
                Quaternion rotation = Quaternion.Euler(0, rndRot, 0);

                float rndScale = Random.Range(0.8f, 1.2f);
                Vector3 scale = new Vector3(1, rndScale, 1);

                Matrix4x4 globalMatrix = Matrix4x4.TRS(position, rotation, scale);

                FillBufferStaticData(instancesData, globalIndex, meshId);
                instancesData[globalIndex].ObjectToWorld = globalMatrix;

                globalIndex++;
            }
        }

        _unitBuffer.SetData(instancesData);
    }

    private void FillBufferStaticData(InstanceData[] instancesData, int instanceIndex, int meshIndex)
    {
        var skinRange = _data.GetSkinTextureIndexRange(meshIndex);
        int globalSkin = (skinRange.end > skinRange.start) ? Random.Range(skinRange.start, skinRange.end) : 0;

        var animRange = _data.GetAnimationIndexRange(meshIndex);
        int animCount = animRange.end - animRange.start;

        int globalAnimIndex = (animCount > 0) ? Random.Range(animRange.start, animRange.end) : 0;

        instancesData[instanceIndex].MeshIndex = meshIndex;
        instancesData[instanceIndex].TextureSkinIndex = globalSkin;
        instancesData[instanceIndex].AnimationIndex = globalAnimIndex;
        instancesData[instanceIndex].AnimationSpeed = Random.Range(0.8f, 1.2f);
    }
}
