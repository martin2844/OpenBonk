// Stuttered sine variant recovered from the shipped desktop shader. Positions
// and displacement are in object space; the frequency order is intentional.
float _VertexAnimationIntensity;
float4 _VertexAnimationFrequency;
float4 MKToonAnimateVertex(float4 vertex)
{
#if defined(_MK_VERTEX_ANIMATION_SINE) && defined(_MK_VERTEX_ANIMATION_STUTTER)
    float3 frequency = _VertexAnimationFrequency.zyx;
    float3 quantized = round(_Time.y * frequency) / frequency;
    vertex.xyz += sin((frac(quantized) + vertex.zxx) * frequency) * _VertexAnimationIntensity;
#endif
    return vertex;
}
