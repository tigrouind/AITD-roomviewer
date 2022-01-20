Shader "Custom/ArrowOnTop"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Overlay" "RenderType"="Overlay"  }
		ZTest Always

		CGPROGRAM
		#pragma surface surf Lambert alpha

		struct Input
		{
			float2 uv_MainTex;
		};

		sampler2D _MainTex;
		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 tex = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb;
			o.Alpha = tex.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}