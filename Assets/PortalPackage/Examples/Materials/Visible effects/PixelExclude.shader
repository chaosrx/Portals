Shader "Custom/PixelExclude" {
	Properties{
		_MainTex("Color (RGB)", 2D) = "white" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}
		_ClipVector("Clip Vector", Vector) = (0, 0, 0)
		_ClipPosition("Clip Position", Vector) = (0, 0, 0)
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		Cull Off
		CGPROGRAM
#pragma surface surf Lambert 
		struct Input {
		float2 uv_MainTex;
		float2 uv_BumpMap;
		float3 worldPos;};
	sampler2D _MainTex;
	sampler2D _BumpMap;
	float3 _ClipPosition;
	float3 _ClipVector;
	void surf(Input IN, inout SurfaceOutput o) {
		clip(dot (_ClipVector, IN.worldPos - _ClipPosition));
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		o.Alpha = 1;
	}
	ENDCG
	}
		Fallback "Diffuse"
}