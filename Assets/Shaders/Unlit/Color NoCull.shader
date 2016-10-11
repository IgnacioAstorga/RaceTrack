Shader "Unlit/Color NoCull" {

	Properties {
		_MainTex("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
		_BackfaceColor("Backface Color", Color) = (0.5, 0.5, 0.5, 1)
	}

	SubShader {

		Cull Off

		Tags { 
			"RenderType" = "Opaque"
		}

		Pass {
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoords : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float2 texcoords : TEXCOORD0;
			};

			fixed4 _Color;
			sampler2D _MainTex;
			fixed4 _BackfaceColor;

			float4 _MainTex_ST;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
				o.texcoords = TRANSFORM_TEX(v.texcoords, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.texcoords);
				col *= i.normal.z >= 0 ? _Color : _BackfaceColor;
				return col;
			}
			ENDCG
		}
	}
}
