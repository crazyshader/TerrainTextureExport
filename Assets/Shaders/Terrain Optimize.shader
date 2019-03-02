Shader "SP/Terrain Optimize" 
{
	Properties 
	{
		_Color("Tint Color", color) = (1, 1, 1, 1)
	    [NoScaleOffset]_Diffuse ("Diffuse Array", 2DArray) = "white" {}
		[NoScaleOffset]_Normal ("Normal Array", 2DArray) = "bump" {}
		[NoScaleOffset]_Index ("Index Map (RGBA)", 2D) = "white" {}
		[NoScaleOffset]_Blend ("Blend Map (RGBA)", 2D) = "white" {}
		_MaxSubTexCount("Max Sub Texture Count", Int) = 8
		_UVScale("Global UV Scales", Vector) = (45, 45, 0, 0)
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert fullforwardshadows
		#pragma exclude_renderers d3d9

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		fixed4 _Color;
		float _MaxSubTexCount;
		float4 _UVScale;
		UNITY_DECLARE_TEX2DARRAY(_Diffuse);
		UNITY_DECLARE_TEX2DARRAY(_Normal);
		sampler2D _Index;
		sampler2D _Blend;

		struct Input 
		{
			float2 uv_Diffuse;
			float2 uv_Index;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			_MaxSubTexCount = _MaxSubTexCount - 1;
			float4 indexTex = tex2D(_Index, IN.uv_Index);
			float indexLayer1 = round(indexTex.r * _MaxSubTexCount);
			float indexLayer2 = round(indexTex.g * _MaxSubTexCount);
			float indexLayer3 = round(indexTex.b * _MaxSubTexCount);
			float indexLayer4 = round(indexTex.a * _MaxSubTexCount);

			float4 mainTex = 0;
			float4 normalMap = 0;
			float4 blendTex = tex2D (_Blend, IN.uv_Index);
			float2 scaledUV = IN.uv_Diffuse * _UVScale.xy + _UVScale.zw;
			if (blendTex.r > 0)
			{
				mainTex += blendTex.r * UNITY_SAMPLE_TEX2DARRAY(_Diffuse, float3(scaledUV, indexLayer1));
				normalMap += blendTex.r * UNITY_SAMPLE_TEX2DARRAY(_Normal, float3(scaledUV, indexLayer1)); 
			}
			if (blendTex.g > 0)
			{
				mainTex += blendTex.g * UNITY_SAMPLE_TEX2DARRAY(_Diffuse, float3(scaledUV, indexLayer2));
				normalMap += blendTex.g * UNITY_SAMPLE_TEX2DARRAY(_Normal, float3(scaledUV, indexLayer2)); 
			}
			if (blendTex.b > 0)
			{
				mainTex += blendTex.b * UNITY_SAMPLE_TEX2DARRAY(_Diffuse, float3(scaledUV, indexLayer3));
				normalMap += blendTex.b * UNITY_SAMPLE_TEX2DARRAY(_Normal, float3(scaledUV, indexLayer3)); 
			}
			if (blendTex.a > 0)
			{
				mainTex += blendTex.a * UNITY_SAMPLE_TEX2DARRAY(_Diffuse, float3(scaledUV, indexLayer4));
				normalMap += blendTex.a * UNITY_SAMPLE_TEX2DARRAY(_Normal, float3(scaledUV, indexLayer4)); 
			}

			mainTex.rgb *= _Color.rgb;
			o.Albedo = mainTex.rgb;
			o.Alpha = 1.0;
			o.Normal = UnpackNormal(normalMap);
		}

		ENDCG
	}  

	FallBack "Legacy Shaders/Diffuse"
}
