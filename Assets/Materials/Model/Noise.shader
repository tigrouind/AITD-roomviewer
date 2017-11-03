Shader "Custom/Noise"
{
	Properties {
		_Noise ("Texture", 2D) = "white" { }
		_Palette ("Texture", 2D) = "white" { }
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _Noise;
			sampler2D _Palette;

			struct vertInput {
				float4 pos : POSITION;
				float4 color : COLOR0;
				float2 uv : TEXCOORD0;
			}; 

			struct vertOutput {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR0;
				float2 uv : TEXCOORD0;
			};

			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				o.color = input.color;
				o.uv = input.uv;
				return o;
			}

			fixed4 frag (vertOutput output) : SV_Target
			{	
				float2 noiseuv = output.uv;
				float shift = tex2D (_Noise, noiseuv).a;

				float2 paletteuv = output.color.rg + 1.0/32.0 + float2(shift * 5.0/16.0, 0.0);
				return tex2D (_Palette, paletteuv);
			}
			ENDCG
		}
	}
}