Shader "Custom/PortalShader" {

	Properties{
		_LeftEyeTexture("Left Eye Texture", 2D) = "white" {}
		_RightEyeTexture("Left Eye Texture", 2D) = "white" {}
		_AlphaTexture("Alpha Texture", 2D) = "white" {}
		_Alpha("AlphaValue", Float) = 1
		_ZTest("ZTest Enabled", Float) = 0
		_Mask("Mask enabled", Float) = 1
		//_Inverted("Inverted", Float) = 0
	}

		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent"}

		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest[_ZTest]
		Cull back
		ZWrite Off
		Pass{
		Name "MainShader"
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#pragma multi_compile __ STEREO_RENDER

		#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv:TEXCOORD0;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
	};

	sampler2D _LeftEyeTexture;
	sampler2D _RightEyeTexture;
	sampler2D _AlphaTexture;

	float _Alpha;
	float _Mask;
	//float _Inverted;

	v2f vert(appdata v, out float4 outpos : SV_POSITION)
	{
		v2f o;
		outpos = mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv = v.uv;
		return o;
	}

	fixed4 frag(v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
	{
		float2 sUV = screenPos.xy / _ScreenParams.xy;
		fixed4 alpha = tex2D(_AlphaTexture, i.uv);
		
		//Inversion for certain rendering engines
		//#if !UNITY_UV_STARTS_AT_TOP
		//if (_Inverted) {
		//	sUV.y = 1 - sUV.y;
		//}
		//#endif
		
		//clip(alpha.a);
		fixed4 col = fixed4(0.0, 0.0, 0.0, 0.0);
		if (unity_CameraProjection[0][2] < 0)
		{
			col = tex2D(_LeftEyeTexture, sUV);
			//col = tex2D(_LeftEyeTexture, i.uv);
		}
		else {
			col = tex2D(_RightEyeTexture, sUV);
			//col = tex2D(_RightEyeTexture, i.uv);
		}
		//Alpha from mask
		if(_Mask > 0)
			col.a = (1 - alpha.r) - (1 - _Alpha);
		//col = alpha;
		return col;
	}
		ENDCG
	}

		}

			Fallback "Diffuse"
}