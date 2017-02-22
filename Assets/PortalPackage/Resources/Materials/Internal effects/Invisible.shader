Shader "Custom/Invisible" {
	Properties
	{
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Cull Off
		ZWrite Off
		ZTest Always
		ColorMask 0

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		struct v2f
	{
		float2 uv : TEXCOORD0;
	};

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};


		v2f vert(appdata v, out float4 outpos : SV_POSITION)
	{
		v2f o;
		outpos = mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv = v.uv;
		return o;
	}
	fixed4 frag(v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
	{
		return 0;
	}
		ENDCG
	}
	}
}
