Shader "Tanknarok/ForcefieldHologram" {
	Properties{
		_MainTex("MainTex", 2D) = "white" {}
		_TintColor("Color", Color) = (0.5,0.5,0.5,1)

		_MaxDistance("MaxDistance", Float) = 4
		_EmissionIntensity("Player Proximity Intensity", Float) = 1.2
		
		_MinStrength("MinStrength", Range(0, 1)) = 0	
		_FallOffStrength("FallOffStrength", Range(0, 4)) = 1.1
		[HideInInspector]_FallOffYOffset("FallOffYOffset", Range(0, 1)) = 0
		_IntersectBoost("Intersection Boost", Range(0, 1)) = 1
		

		_PositionPLAYER1("PositionPLAYER1", Vector) = (0,0,0,0)
		_PositionPLAYER2("PositionPLAYER2", Vector) = (0,0,0,0)
		_PositionPLAYER3("PositionPLAYER3", Vector) = (0,0,0,0)
		_PositionPLAYER4("PositionPLAYER4", Vector) = (0,0,0,0)

		[MaterialToggle] _PLAYER2Toggle("PLAYER2Toggle", Float) = 0
		[MaterialToggle] _PLAYER1Toggle("PLAYER1Toggle", Float) = 0
		[MaterialToggle] _PLAYER3Toggle("PLAYER3Toggle", Float) = 0
		[MaterialToggle] _PLAYER4Toggle("PLAYER4Toggle", Float) = 0

		[HideInInspector]_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader{
		Tags {
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		Pass {
			Name "FORWARD"
			Tags {
				"LightMode" = "ForwardBase"
			}

			//https://docs.unity3d.com/Manual/SL-Blend.html
			Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
			//Blend One OneMinusSrcAlpha // Premultiplied transparency
			//Blend One One // Additive
			//Blend OneMinusDstColor One // Soft Additive
			//Blend DstColor Zero // Multiplicative
			//Blend DstColor SrcColor // 2x Multiplicative

			//https://docs.unity3d.com/Manual/SL-Pass.html
			Cull Off // render the backside
			//Lighting Off
			ZWrite Off // Prevent forcefield from hiding other transparent things

			//https://docs.unity3d.com/Manual/SL-ShaderPrograms.html
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			uniform sampler2D _CameraDepthTexture;
			uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
			uniform float4 _TintColor;
			uniform float _MaxDistance;
			uniform float _IntersectBoost;
			uniform float _MinStrength;
			uniform float _FallOffStrength;
			uniform float _FallOffYOffset;
			uniform float _EmissionIntensity;

			uniform float4 _PositionPLAYER1;
			uniform float4 _PositionPLAYER2;
			uniform float4 _PositionPLAYER3;
			uniform float4 _PositionPLAYER4;
			uniform fixed _PLAYER2Toggle;
			uniform fixed _PLAYER1Toggle;
			uniform fixed _PLAYER3Toggle;
			uniform fixed _PLAYER4Toggle;

			struct VertexInput {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
				//float4 vertexColor : COLOR;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				//float4 vertexColor : COLOR;
				float4 projPos : TEXCOORD2;

			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				//o.vertexColor = v.vertexColor;
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);
				return o;
			}
			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				//float isFrontFace = (facing >= 0 ? 1 : 0);
				//float faceSign = (facing >= 0 ? 1 : -1);
				float sceneZ = max(0,LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.y);
				float partZ = max(0, i.projPos.z - _ProjectionParams.y);
				float4 tex = tex2D(_MainTex, TRANSFORM_TEX(i.uv0, _MainTex));

				float3 center = float3(0, 0, 0);
				float p1Distance = distance(lerp(center, _PositionPLAYER1.xyz, _PLAYER1Toggle), i.posWorld.xyz) / _MaxDistance;  // The distance between player1 and the current pixel in world space.
				float p2Distance = distance(lerp(center, _PositionPLAYER2.xyz, _PLAYER2Toggle), i.posWorld.xyz) / _MaxDistance;  // If the player isn't present (playertoggle) then use the world center instead.
				float p3Distance = distance(lerp(center, _PositionPLAYER3.xyz, _PLAYER3Toggle), i.posWorld.xyz) / _MaxDistance;  // Oh, and the player toggles and positions are updated via a script.
				float p4Distance = distance(lerp(center, _PositionPLAYER4.xyz, _PLAYER4Toggle), i.posWorld.xyz) / _MaxDistance;
				float shortestDistance = saturate(1.0 - min(min(min(p1Distance, p2Distance), p3Distance), p4Distance));  // get the distance from the closest player
				float intensity = max(_MinStrength, shortestDistance * _EmissionIntensity);  // calculate the brightness/intensity based on the distance. Closer -> more intense.
				

				float3 base = tex.rgb * _TintColor.rgb * intensity * 2.0;	// the base hologram
				float3 intersect = (1.0 - saturate((sceneZ - partZ) / _IntersectBoost)) * intensity * _TintColor.rgb;  // added boost where the hologram intersects anything
				float falloff = 1.0 - saturate((i.uv0.g.r - _FallOffYOffset) * _FallOffStrength);  // fades the hologram at the top

				// put it all together
				float3 emissive = (base + intersect) * falloff * fmod(i.pos.y, 2);

				return fixed4(emissive, length(emissive));
			}
			ENDCG
		}
	}
}
