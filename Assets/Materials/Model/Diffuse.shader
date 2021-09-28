Shader "Custom/Diffuse"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType" = "Transparent" }

		Pass
		{
			Zwrite off
			Blend SrcAlpha OneMinusSrcAlpha

			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityLightingCommon.cginc"

			float4 _Color;

			struct vertInput {
				float4 pos : POSITION;
				float3 normal : NORMAL;
			};

			struct vertOutput {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR0;
			};
 
			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				float3 light = normalize(_WorldSpaceLightPos0.xyz);
				float3 color = _Color.rgb * abs(dot(input.normal, light)) * _LightColor0.rgb;
				o.color = float4(color.x, color.y, color.z, _Color.a);
				return o;
			}
  
			fixed4 frag (vertOutput output) : SV_Target
			{
				return output.color;
			}
			ENDCG
		}
	} 
}