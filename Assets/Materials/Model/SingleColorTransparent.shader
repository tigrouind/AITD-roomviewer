Shader "Custom/SingleColorTransparent"
{
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Pass
		{			 
			Zwrite off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct vertInput {
				float4 pos : POSITION;
				float4 color : COLOR0;
			}; 

			struct vertOutput {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR0;
			};

			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				o.color = input.color;
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