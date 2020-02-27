Shader "Custom/AlwaysOnTop"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue"="Overlay" "RenderType"="Overlay"  }
		ZTest Always

		CGPROGRAM
		#pragma surface surf Lambert alpha

		struct Input
		{
			float4 color : COLOR;
		};

		fixed4 _Color;
		void surf (Input IN, inout SurfaceOutput o)
		{
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}