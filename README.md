# MassRenderer

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

High-performance GPU-based rendering system for Unity using **Multi-Draw Indirect (MDI)** with per-mesh instancing and **Vertex Animation Textures (VAT)** for skeletal animation.

https://github.com/user-attachments/assets/a5d207fd-cff6-44aa-bd7d-f8ee6f8e35b1

## Features

- **Multi-Draw Indirect Rendering** — Render thousands of meshes with a single draw call
- **GPU Instancing** — Efficient per-instance data handling via compute buffers
- **VAT Animation System** — Bake skeletal animations to textures for GPU playback
- **Multiple Mesh Support** — Render different mesh types in one batch
- **Texture Skins** — Support for multiple texture variations per prototype
- **URP Compatible** — Works with Unity's Universal Render Pipeline

## Requirements

- Unity 2022.3 or higher
- Universal Render Pipeline (URP)
- Graphics API with Multi-Draw Indirect support (DX11+, Vulkan, Metal)

## Quick Start

### Step 1: Bake Render Data

1. Add the `RenderDataCreator` component to an empty GameObject
2. Configure prototype meshes and textures
3. *(Optional)* Add an Animator with animation clips for VAT baking
4. Click the **"Bake"** button to generate render data

<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/45e069ab-1322-4a66-a009-46a5fe97f6df" width="400" alt="RenderDataCreator Setup"></td>
    <td><img src="https://github.com/user-attachments/assets/eb62592c-d988-427a-acf8-4fb19164d4fb" width="400" alt="Bake Result"></td>
  </tr>
</table>

### Step 2: Initialize MassRenderer

```csharp
// Configure parameters
MassRendererParams msParams = new MassRendererParams
{
    IsVATEnable = true,
    InstanceCount = 1000,
    RenderBounds = new Bounds(Vector3.zero, Vector3.one * 1000f),
    ShaderType = MassRenderShaderType.Lit
};

// Create and initialize renderer
var renderer = new MassRenderer(renderStaticData, msParams, material);
renderer.Initialize();
```

### Step 3: Fill Instance Data

When MassRenderer is initialized, it creates an `InstancesDataBuffer`.

**For static objects** — Fill the buffer once after initialization.

**For dynamic objects** — Fill the buffer after initialization and update it before calling the `Render()` method.

RenderData provides helper methods to get index ranges for setting the required animation clip or skin:

```csharp
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
```

### Step 4: Render

```csharp
void Update()
{
    // _simulationSystem.Simulate(); — for dynamic objects
    renderer.Render();
}

void OnDestroy()
{
    renderer.Dispose();
}
```

For more details, see `CrowdSimulation` and `GrassSimulation` examples in the Samples folder.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
