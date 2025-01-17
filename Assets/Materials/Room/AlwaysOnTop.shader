Shader "Custom/AlwaysOnTop"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_OffsetUnits ("Depth Bias", Float) = 0
	}
	SubShader
	{
		Offset 0, [_OffsetUnits]

		CGPROGRAM
		#pragma surface surf Lambert

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