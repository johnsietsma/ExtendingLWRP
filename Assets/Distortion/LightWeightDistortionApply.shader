Shader "NotHidden/DistortionApply"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "black" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
		LOD 300

		Pass
		{
			Name "DistortionApply"
			Tags{ "LightMode" = "LightweightForward" }

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma vertex VertexDistortionApply
			#pragma fragment FragmentDistortionApply

			#include "Distortion.hlsl"

			ENDHLSL
		}
	}
	Fallback "Hidden/InternalErrorShader"
}
