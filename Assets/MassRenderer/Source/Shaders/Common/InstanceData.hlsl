#ifndef INSTANCE_DATA_INCLUDED
#define INSTANCE_DATA_INCLUDED

// Packed InstanceData structure matching C# InstanceData layout.
// PackedMeshSkin: lower 16 bits = meshIndex, upper 16 bits = textureSkinIndex
// PackedAnimData: lower 16 bits = animationIndex, upper 16 bits = animationSpeed (half-float)
struct InstanceData
{
    uint     packedMeshSkin;
    uint     packedAnimData;
    float4x4 objectToWorld;
};

int GetMeshIndex(InstanceData d)        { return (int)(d.packedMeshSkin & 0xFFFF); }
int GetTextureSkinIndex(InstanceData d)  { return (int)((d.packedMeshSkin >> 16) & 0xFFFF); }
int GetAnimationIndex(InstanceData d)    { return (int)(d.packedAnimData & 0xFFFF); }
float GetAnimationSpeed(InstanceData d)  { return f16tof32(d.packedAnimData >> 16); }

#endif
