using MassRendererSystem;
using MassRendererSystem.Data;
using UnityEngine;

/// <summary>
/// Sample MonoBehaviour demonstrating static foliage/grass rendering with MassRenderer.
/// Places instances in a grid pattern without animation support.
/// </summary>
public class GrassSimulationSample : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RenderStaticData _mrData;
    [SerializeField] private int _instanceCount = 1000;
    [SerializeField] private Bounds _renderBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

    [Header("Grid Settings")]
    [SerializeField] private Vector2 _areaSize = new Vector2(50, 50);
    [SerializeField] private float _positionJitter = 0.5f;

    [Header("Transform")]
    [SerializeField] private Vector3 _position = Vector3.zero;
    [SerializeField] private Vector3 _angel = Vector3.zero;
    [SerializeField] private Vector3 _scale = Vector3.one;

    private MassRenderer _renderer;
    private GrassSimulation _simulationSystem;

    private void Start()
    {
        if (_mrData == null) return;

        MassRendererParams msParams = new MassRendererParams
        {
            IsVATEnable = false,
            InstanceCount = _instanceCount,
            RenderBounds = _renderBounds,
            ShaderType = MassRenderShaderType.SimpleLit
        };

        _renderer = new MassRenderer(_mrData, msParams);
        _renderer.Initialize();
        UpdateTransform();

        _simulationSystem = new GrassSimulation(
            _mrData,
            _renderer.InstancesDataBuffer,
            _instanceCount,
            _areaSize,
            _positionJitter
        );

        _simulationSystem.InitBuffers();

        _renderer.RebuildDrawCommands(_simulationSystem.InstanceCounts);
    }

    private void Update()
    {
        _renderer.Render();
    }

    private void OnDestroy()
    {
        _renderer?.Dispose();
    }

    private void OnValidate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (_renderer != null)
        {
            Matrix4x4 globalMatrix = Matrix4x4.TRS(_position, Quaternion.Euler(_angel), _scale);
            _renderer.SetGlobalTransform(globalMatrix);
        }
    }
}