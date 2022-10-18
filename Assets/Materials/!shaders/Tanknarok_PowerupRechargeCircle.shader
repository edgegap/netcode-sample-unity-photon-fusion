Shader "Tanknarok/PowerupRechargeCircle" {
	Properties{
		_Color("Color", Color) = (0,0.710345,1,1)
		_Recharge("Recharge", Range(0, 1)) = 0
		_EmissionMultiplier("EmissionMultiplier", Float) = 1
		_Thickness_Outer("Thickness_Outer", Range(0, 1)) = 0
		_Thickness_Inner("Thickness_Inner", Range(0, 1)) = 0
		[HideInInspector]_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		_Stencil("Stencil ID", Float) = 0
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilComp("Stencil Comparison", Float) = 8
		_StencilOp("Stencil Operation", Float) = 0
		_StencilOpFail("Stencil Fail Operation", Float) = 0
		_StencilOpZFail("Stencil Z-Fail Operation", Float) = 0
	}

	SubShader{
		Tags {
			"Queue" = "AlphaTest"
			"RenderType" = "TransparentCutout"
			"CanUseSpriteAtlas" = "True"
			"PreviewType" = "Plane"
		}
		Pass {
			Name "FORWARD"
			Tags {
				"LightMode" = "ForwardBase"
			}
			Blend One OneMinusSrcAlpha
			Cull Off


			Stencil {
				Ref[_Stencil]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
				Comp[_StencilComp]
				Pass[_StencilOp]
				Fail[_StencilOpFail]
				ZFail[_StencilOpZFail]
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			#pragma multi_compile_fwdbase_fullshadows
//			#pragma only_renderers d3d9 d3d11 glcore gles 
			#pragma target 3.0
			uniform float4 _Color;
			uniform float _Recharge;
			uniform float _EmissionMultiplier;
			uniform float _Thickness_Outer;
			uniform float _Thickness_Inner;
			struct VertexInput {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
			};
			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				o.pos = UnityObjectToClipPos(v.vertex);
				#ifdef PIXELSNAP_ON
					o.pos = UnityPixelSnap(o.pos);
				#endif
				return o;
			}
			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float isFrontFace = (facing >= 0 ? 1 : 0);
				float faceSign = (facing >= 0 ? 1 : -1);
				float2 node_1792 = (i.uv0*2.0 + -1.0);
				float2 node_7025 = node_1792.rg;
				float node_7802 = length(node_1792);
				clip(((1.0 - ceil((((atan2(node_7025.g,node_7025.r) / 6.28318530718) + 0.5) - _Recharge)))*floor((_Thickness_Inner + node_7802))*(1.0 - floor((node_7802 + _Thickness_Outer)))) - 0.5);

				float3 emissive = (_EmissionMultiplier*_Color.rgb);
				float3 finalColor = emissive;
				return fixed4(finalColor,1);
			}
			ENDCG
		}
		Pass {
			Name "ShadowCaster"
			Tags {
				"LightMode" = "ShadowCaster"
			}
			Offset 1, 1
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define UNITY_PASS_SHADOWCASTER
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcaster
			//#pragma only_renderers d3d9 d3d11 glcore gles 
			#pragma target 3.0
			uniform float _Recharge;
			uniform float _Thickness_Outer;
			uniform float _Thickness_Inner;
			struct VertexInput {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};
			struct VertexOutput {
				V2F_SHADOW_CASTER;
				float2 uv0 : TEXCOORD1;
			};
			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				o.pos = UnityObjectToClipPos(v.vertex);
				#ifdef PIXELSNAP_ON
					o.pos = UnityPixelSnap(o.pos);
				#endif
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}
			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float isFrontFace = (facing >= 0 ? 1 : 0);
				float faceSign = (facing >= 0 ? 1 : -1);
				float2 node_1792 = (i.uv0*2.0 + -1.0);
				float2 node_7025 = node_1792.rg;
				float node_7802 = length(node_1792);
				clip(((1.0 - ceil((((atan2(node_7025.g,node_7025.r) / 6.28318530718) + 0.5) - _Recharge)))*floor((_Thickness_Inner + node_7802))*(1.0 - floor((node_7802 + _Thickness_Outer)))) - 0.5);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
