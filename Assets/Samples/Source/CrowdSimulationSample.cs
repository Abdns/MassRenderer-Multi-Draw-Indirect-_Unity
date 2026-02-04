using MassRendererSystem;
using MassRendererSystem.Data;
using UnityEngine;

/// <summary>
/// Sample MonoBehaviour demonstrating animated crowd rendering with MassRenderer.
/// Uses compute shader for GPU-based crowd simulation with VAT animations.
/// </summary>
public class CrowdSimulationSample : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RenderStaticData _mrData;
    [SerializeField] private Material _mrMaterial;
    [SerializeField] private ComputeShader _cs;
    [SerializeField] private int _instanceCount = 512;
    [SerializeField] private bool _hasVAT = true;

    [Header("Transform")]
    [SerializeField] private Vector3 _position = Vector3.zero;
    [SerializeField] private Vector3 _angle = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 _scale = new Vector3(1f, 1f, 1f);

    private MassRenderer _renderer;
    private CrowdSimulation _simulationSystem;
    private Bounds _renderBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

    private void Start()
    {
        MassRendererParams msParams = new MassRendererParams
        {
            IsVATEnable = _hasVAT,
            InstanceCount = _instanceCount,
            RenderBounds = _renderBounds,
            ShaderType = MassRenderShaderType.Lit
        };

        _renderer = new MassRenderer(_mrData, msParams, _mrMaterial);

        _renderer.Initialize();
        UpdateTransform();

        _simulationSystem = new CrowdSimulation(_instanceCount, _renderer.InstancesDataBuffer, _mrData, _cs);
        _simulationSystem.Initialize();

        _renderer.RebuildDrawCommands(_simulationSystem.InstanceCounts);
    }

    private void Update()
    {
        _simulationSystem.Simulate();
        _renderer.Render();
    }

    private void OnDestroy()
    {
        _renderer?.Dispose();
        _simulationSystem?.ReleaseBuffers();
    }

    private void OnValidate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (_renderer != null)
        {
            Matrix4x4 globalMatrix = Matrix4x4.TRS(_position, Quaternion.Euler(_angle), _scale);
            _renderer.SetGlobalTransform(globalMatrix);
        }
    }
}