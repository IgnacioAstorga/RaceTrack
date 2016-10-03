Shader "Unlit/Color NoCull" {

	Properties {
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
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};

			fixed4 _Color;
			fixed4 _BackfaceColor;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = i.normal.z >= 0 ? _Color : _BackfaceColor;
				return col;
			}
			ENDCG
		}
	}
}
