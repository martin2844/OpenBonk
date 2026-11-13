// Object-space noise deformation from the shipped Simple shader vertex variant.
float _VertexAnimationIntensity;
float4 _VertexAnimationFrequency;
float4 MKToonNoiseAnimateVertex(float4 vertex, float3 normal)
{
#if defined(_MK_VERTEX_ANIMATION_NOISE)
    float seed = frac(sin(dot(vertex.xyz, float3(26246.8613, 88296.1172, 8913.5625))) * 987654.562);
    vertex.xyz += sin(seed * _Time.y * _VertexAnimationFrequency.xyz) * normal * _VertexAnimationIntensity;
#endif
    return vertex;
}
