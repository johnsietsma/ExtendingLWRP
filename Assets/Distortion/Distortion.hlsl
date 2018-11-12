#ifndef DISTORTION_INCLUDED
#define DISTORTION_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
	TEXTURE2D(_MainTex);
	SAMPLER(sampler_MainTex);
	float4 _MainTex_ST;
	TEXTURE2D(_DistortionTex);
	SAMPLER(sampler_DistortionTex);
CBUFFER_END

CBUFFER_START(UnityPerMaterial)
	TEXTURE2D(_DistortionVectorMap);
	SAMPLER(sampler_DistortionVectorMap);
	float4 _DistortionVectorMap_ST;
	float _DistortionAmount;
CBUFFER_END



struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings VertexDistortion(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _DistortionVectorMap);
    output.positionCS = TransformObjectToHClip(input.position.xyz);
    return output;
}

half4 FragmentDistortion(Varyings input) : SV_TARGET
{
	float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.uv).rgb;
	distortion *= _DistortionAmount;
	//TODO
	// distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
	// builtinData.distortion = distortion.rg * _DistortionScale;
	// builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;

    return half4(distortion.rgb,1);
}

Varyings VertexDistortionApply(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
	output.positionCS = TransformObjectToHClip(input.position.xyz);
	return output;
}

half4 FragmentDistortionApply(Varyings input) : SV_TARGET
{
	float2 d = SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, input.uv).rg;
	float2 uv = input.uv;
	uv += d;
	half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
	return c;
}

#endif // DISTORTION_INCLUDED
