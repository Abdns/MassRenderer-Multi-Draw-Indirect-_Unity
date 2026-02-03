#ifndef MY_COMMON_UTILS_INCLUDED
#define MY_COMMON_UTILS_INCLUDED

struct VATClipAtlasInfo
{
    int   vertexCount;
    int   frameCount;
    float duration;
    float normalizedOffsetX;
    float normalizedOffsetY;
    float normalizedWidth;
    float normalizedLength;
};

float2 CalculateVATUV(VATClipAtlasInfo clip, uint vertexID, float animSpeed)
{
    float normalizedTime = fmod((_Time.y * animSpeed) / clip.duration, 1.0);
    
    float x = clip.normalizedOffsetX + (vertexID + 0.5) / clip.vertexCount * clip.normalizedWidth;
    float y = clip.normalizedOffsetY + normalizedTime * clip.normalizedLength;

    return float2(x, y);
}

float3 SampleAnimPosition(Texture2D<float4> vatAtlas, SamplerState atlasSampler, float2 uv)
{            
    return vatAtlas.SampleLevel(atlasSampler, uv, 0).xyz;
}

float3 SampleAnimNormal(Texture2D<float4> normAtlas, SamplerState atlasSampler, float2 uv)
{
    //RGBAFloat[-1...1] ---  RGBA32 [0..1] | n * 2.0 - 1.0
    return normAtlas.SampleLevel(atlasSampler, uv, 0).xyz;
}

#endif 