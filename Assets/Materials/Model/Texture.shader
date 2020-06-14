Shader "Custom/Texture"
{
	Properties {
		_MainTex  ("Texture", 2D) = "white" { }
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;

			struct vertInput {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct vertOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				o.uv = input.uv;
				return o;
			}

			fixed4 frag (vertOutput output) : SV_Target
			{
				float4 color = tex2D(_MainTex, output.uv);
				if ((color.a) == 0.0f) //transparent
				{
					discard;
				}
				return color;
			}
			ENDCG
		}
	}
}