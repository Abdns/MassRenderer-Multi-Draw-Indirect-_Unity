#ifndef MATRIX_UTILS_INCLUDED
#define MATRIX_UTILS_INCLUDED

float4x4 BuildTRS(float3 position, float3 direction, float3 scale)
{
    float3 forward = normalize(direction);
    
    float3 tmpUp = abs(forward.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);    
    float3 right = normalize(cross(tmpUp, forward)); 
    float3 up    = cross(forward, right);          
    
    return float4x4(
        right.x * scale.x,   up.x * scale.y,   forward.x * scale.z, position.x,
        right.y * scale.x,   up.y * scale.y,   forward.y * scale.z, position.y,
        right.z * scale.x,   up.z * scale.y,   forward.z * scale.z, position.z,
        0,                   0,                0,                   1
    );
}

#endif