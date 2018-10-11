Shader "ExtendingLWRP/Stained BumpDistort LWRP (no grab)"
{
	Properties
	{
		_MainTex("Tint Color (RGB)", 2D) = "white" {}
		_TintAmt("Tint Amount", Range(0,1)) = 0.1
		_BumpAmt("Distortion", range(0,64)) = 10
		_BumpMap("Normalmap", 2D) = "bump" {}
	}

	SubShader
	{
		// We must be transparent, so other objects are drawn before this one.
		Tags { "Queue" = "Transparent" "RenderPipeline" = "LightweightPipeline" "RenderType" = "Opaque" }

		Pass
		{
			Name "Simple"
			Tags { "LightMode" = "LightweightForward" }

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 texcoord   : TEXCOORD0;
			};

			struct Varyings
			{
				float4 vertex					: SV_POSITION;
				float4 uvGrab		            : TEXCOORD0; 
				float2 uvBump					: TEXCOORD1;
				float3 uvMain					: TEXCOORD2; // xy: uv0, z: fogCoord

			};

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

			sampler2D _GrabBlurTexture;
			float4 _GrabBlurTexture_TexelSize;

			float _BumpAmt;
			half _TintAmt;
			float4 _BumpMap_ST;
			float4 _MainTex_ST;


			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.vertex = vertexInput.positionCS;

#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
#else
				float scale = 1.0;
#endif
				output.uvGrab.xy = (float2(output.vertex.x, output.vertex.y*scale) + output.vertex.w) * 0.5;
				output.uvGrab.zw = output.vertex.zw;
				output.uvBump = TRANSFORM_TEX(input.texcoord, _BumpMap);
				output.uvMain.xy = TRANSFORM_TEX(input.texcoord, _MainTex);
				output.uvMain.z = ComputeFogFactor(vertexInput.positionCS.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// calculate perturbed coordinates
				// we could optimize this by just reading the x & y without reconstructing the Z
				half2 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uvBump)).rg;
				float2 offset = bump * _BumpAmt * _GrabBlurTexture_TexelSize.xy;
				input.uvGrab.xy = offset * input.uvGrab.z + input.uvGrab.xy;

				half4 col = tex2Dproj(_GrabBlurTexture, input.uvGrab);
				half4 tint = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMain.xy);
				col = lerp(col, tint, _TintAmt);

				col.xyz = MixFog(col.xyz, input.uvMain.z);
				return col;
			}
			ENDHLSL
		}
	}

	FallBack "Hidden/InternalErrorShader"
}
