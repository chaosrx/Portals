Shader "Custom/PortalPlaceholder" {
	Properties {
		_Color("Main Color", Color) = (1,1,1,1)
		_ColorF("Final Color", Color) = (1,1,1,1)
		_AlphaTexture("Alpha Texture", 2D) = "white" {}
		_Alpha("AlphaValue", Range(0, 1)) = 1
	}
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {

			CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"

		float random(in float2 _st) {
			return frac(sin(dot(_st.xy, float2(12.9898,78.233))) * 43758.54531237);
		}

		// Based on Morgan McGuire @morgan3d
		// https://www.shadertoy.com/view/4dS3Wd
		float noise(in float2 _st) {
			float2 i = floor(_st);
			float2 f = frac(_st);

			// Four corners in 2D of a tile
			float a = random(i);
			float b = random(i + float2(1.0, 0.0));
			float c = random(i + float2(0.0, 1.0));
			float d = random(i + float2(1.0, 1.0));

			float2 u = f * f * (3. - 2.0 * f);

			return lerp(a, b, u.x) +
				(c - a)* u.y * (1. - u.x) +
				(d - b) * u.x * u.y;
		}

#define NUM_OCTAVES 5

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			UNITY_TRANSFER_FOG(o, o.vertex);
			return o;
		}

		float fbm(in float2 _st) {
			float v = 0.0;
			float a = 0.5;
			float2 shift = float2(20.0, 20.0);
			// Rotate to reduce axial bias
			float2x2 rot = float2x2 (cos(0.5), sin(0.5),
				-sin(0.5), cos(0.50));
			for (int i = 0; i < NUM_OCTAVES; ++i) {
				v += a * noise(_st);
				_st = float2(rot[0][0], rot[0][1]) * _st * 2.2 + shift;
				a *= 0.5;
			}
			return v;
		}

		float4 _Color;
		float4 _ColorF;
		float _Alpha;
		sampler2D _AlphaTexture;

		fixed4 frag(v2f i) : SV_Target {
			float2 uv2 = i.uv * 1000;
			float2 st = (uv2 - 0.5* _ScreenParams.xy) / min(_ScreenParams.x,  _ScreenParams.y);
			st *= 3.5;

			float3 color = float3(0, 0, 0);
			float2 a = float2(0, 0);
			float2 b = float2(0, 0);
			float2 c = float2(60.,800.);

			a.x = fbm(st);
			a.y = fbm(st + float2(1.0, 1.0));

			b.x = fbm(st + 4.*a);
			b.y = fbm(st);

			c.x = fbm(st + 7.0*b + float2(10.7, 0.2) + 0.215*_Time.y * 10);
			c.y = fbm(st + 3.944*b + float2(.3,12.8) + 0.16*_Time.y * 10);

			float f = fbm(st + b + c);

			//color = lerp(float3(0.445,0.002,0.419), float3(1.000,0.467,0.174), clamp((f*f),0.2, 1.0));
			//color = lerp(color, float3(0.413,0.524,0.880), clamp(length(c.x),0.480, 0.92));

			color = lerp(_Color, _ColorF, clamp((f*f), 0.2, 1.0));
			//color = lerp(color, _ColorF, clamp(length(c.x), 0.480, 0.92));

			st = st / 3.5;
			float3 finalColor = float3(f*1.9*color);
			float3 bgColor = float3(0.950, 0.951, 0.90);

			fixed4 alpha = tex2D(_AlphaTexture, i.uv);
			return float4(finalColor.x, finalColor.y, finalColor.z, 1 - alpha.r);
		}
		ENDCG
		}	
	}
	FallBack "Diffuse"
}
