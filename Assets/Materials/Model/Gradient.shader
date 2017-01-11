Shader "Custom/Gradient"
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

			sampler2D _Palette;

			struct vertInput {
				float4 pos : POSITION;
				float4 color : COLOR0;
			}; 

			struct vertOutput {
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				fixed4 color : COLOR0;
			};

			vertOutput vert (vertInput input)
			{
				vertOutput o;
				o.pos = mul(UNITY_MATRIX_MVP, input.pos);
				o.uv = mul(UNITY_MATRIX_MV, input.pos);
				o.color = input.color;
				return o;
			}

			fixed4 frag (vertOutput output) : SV_Target
			{
				float palette = output.color.b + 1.0/32.0;
				float2 screen = output.uv.xy / output.uv.z + float2(1.0, 1.0);
				//vertical or horizontal gradient
				float gradient = dot(screen, output.color.rg); 
				gradient = abs(((gradient*3.0)%2.0) - 1.0);
				return tex2D (_Palette, float2(gradient, palette));
			}
			ENDCG
		}
	}
}