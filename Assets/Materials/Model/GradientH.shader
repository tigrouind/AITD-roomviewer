﻿Shader "Custom/GradientH"
{
	Properties {
		_Palette ("Texture", 2D) = "white" { }
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Palette;

			struct vertInput {
				float4 pos : POSITION;
				float4 color : COLOR0;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
			};

			struct vertOutput {
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				fixed4 color : COLOR0;
			};

			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				o.pos.z = o.pos.z - input.uv1.x * 0.00001; //fix z-fighting
				o.screenPos = ComputeScreenPos(o.pos);
				o.uv = input.uv0;
				o.color = input.color;
				return o;
			}

			fixed4 frag (vertOutput output) : SV_Target
			{
				float palette = output.color.b + 1.0/32.0;
				float2 screen = output.screenPos.xy / output.screenPos.w;

				float gradient = (screen.x - output.uv.x) / (output.uv.y - output.uv.x);
				return tex2D (_Palette, float2(gradient, palette));
			}
			ENDCG
		}
	}
}