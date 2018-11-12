Shader "Lightweight Render Pipeline/Distortion"
{
	Properties
	{
		[NoScaleOffset] _DistortionVectorMap("Distortion Vector Map", 2D) = "black" {}
		_DistortionAmount("Distortion Amount", Float) = 0.5
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
		LOD 300

		Pass
		{
			Name "DistortionVectors"
			Tags{ "LightMode" = "DistortionVectors" }

			//Blend One One, One One
			//BlendOp Add, Add
			//ZTest LEqual 
			//ZWrite Off

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma vertex VertexDistortion
			#pragma fragment FragmentDistortion

			#include "Distortion.hlsl"

			ENDHLSL
		}
	}
	Fallback "Hidden/InternalErrorShader"
}
