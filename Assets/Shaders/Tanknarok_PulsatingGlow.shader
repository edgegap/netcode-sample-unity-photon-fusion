Shader "Tanknarok/PulseGlow"
{
	Properties{
		_Color1("Color1", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (0,0,0,1)
		_PulseSpeed("Pulse Speed", Float) = 1
		_GlowStrength("Glow Strength", Float) = 1
		_Delay("Pulse Delay", Range(0, 1)) = 0
	}
	SubShader{
		Tags {
			"RenderType" = "Opaque"
		}
		LOD 200
		Pass {
			Name "FORWARD"
			Tags {
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#pragma multi_compile_fwdbase_fullshadows
			#pragma only_renderers d3d9 d3d11 glcore gles xboxone
			#pragma target 3.0

			float4 _Color1;
			float4 _Color2;
			float _GlowStrength;
			float _PulseSpeed;
			float _Delay;

			struct VertexInput {
				float4 vertex : POSITION;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.pos = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			float4 frag(VertexOutput i) : COLOR {
				float s = sin((_PulseSpeed * _Time.y) + (_Delay * 3.14)) * 0.5 + 0.5;
				float3 emissive = (lerp(_Color1.rgb, _Color2.rgb, s) * _GlowStrength);

				fixed4 col = fixed4(emissive, 1);
				return col;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}