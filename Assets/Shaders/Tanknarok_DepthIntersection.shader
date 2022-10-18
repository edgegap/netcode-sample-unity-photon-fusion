Shader "Tanknarok/DepthIntersection"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_DepthColor("DepthColor", Color) = (0.5,0.5,0.5,1)
		_Depth("Depth", Float) = 2.5
		_EmissionStrength("EmissionStrength", Float) = 1
	}
	SubShader{
		Tags {
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		Pass {
			Name "FORWARD"

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define UNITY_PASS_FORWARDBASE
			#include "UnityCG.cginc"
			#pragma multi_compile_fwdbase
			#pragma only_renderers d3d9 d3d11 glcore gles xboxone
			#pragma target 3.0

			uniform sampler2D _CameraDepthTexture;
			uniform float4 _Color;
			uniform float4 _DepthColor;
			uniform float _Depth;
			uniform float _EmissionStrength;

			struct VertexInput {
				float4 vertex : POSITION;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float4 posWorld : TEXCOORD0;
				float4 projPos : TEXCOORD2;
			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);
				return o;
			}
			float4 frag(VertexOutput i) : COLOR {
				float sceneZ = max(0, LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
				float partZ = max(0,i.projPos.z - _ProjectionParams.g);


				float normalizedDepth = (1.0 - saturate((sceneZ - partZ) / _Depth));
				float3 lavaColor = _Color.rgb * _EmissionStrength;
				float3 depthColor = _DepthColor * normalizedDepth;
				float3 emissive = lavaColor + depthColor;

				return fixed4(emissive, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}

