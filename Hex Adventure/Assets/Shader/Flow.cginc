#if !defined(FLOW_INCLUDED)
#define FLOW_INCLUDED

float3 FlowUVW (float2 uv, float2 flowVector, float2 jump, float flowOffset, float tiling, float time, bool isFlow)
{
    float phaseOffset = isFlow ? 0.5 : 0;
    float progress = frac(time + phaseOffset);
    
    float3 uvw;

    // Offset
    uvw.xy = uv - flowVector * (progress + flowOffset);

    // Tiling
    uvw.xy *= tiling;
    uvw.xy += phaseOffset;

    // Jump
    uvw.xy += (time - progress) * jump;
    uvw.z = 1 - abs(1 - 2 * progress);
    return uvw;
}


float2 DirectionalFlowUV(float2 uv, float3 flowVectorAndSpeed, float tiling, float time, out float2x2 rotation)
{
    float2 dir = normalize(flowVectorAndSpeed.xy); // Not a unit vector, normalize first 
    rotation = float2x2(dir.y, dir.x, -dir.x, dir.y);
    uv = mul(float2x2(dir.y, -dir.x, dir.x, dir.y), uv);
    uv.y -= time * flowVectorAndSpeed.z;
    return uv * tiling;
}

#endif