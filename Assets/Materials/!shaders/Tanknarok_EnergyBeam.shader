Shader "Tanknarok/EnergyBeam" {
	Properties{
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Particle Texture", 2D) = "white" {}
		_NoiseTex("Noise Texture", 2D) = "black" {}
		_InvFade("Soft Particles Factor", Range(0.01,10.0)) = 1.0
		_EmissionStrength("Emission Strength", Range(1.0, 30.0)) = 1.0
		_BeamFade("Beam fade", Range(0, 1)) = 0
	}

	Category{
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
		}
		Blend SrcAlpha One
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off

		SubShader {
			Pass {

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile_particles

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				sampler2D _NoiseTex;
				fixed4 _TintColor;
				
				

				struct appdata_t {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float2 uv : TEXCOORD0;

					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_OUTPUT_STEREO
				};

				float4 _MainTex_ST;

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos(o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.uv = TRANSFORM_TEX(v.uv,_MainTex);
					return o;
				}

				UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
				float _InvFade;
				float _EmissionStrength;
				float _BeamFade;

				fixed4 frag(v2f i) : SV_Target
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
					float partZ = i.projPos.z;
					float fade = saturate(_InvFade * (sceneZ - partZ));
					i.color.a *= fade;
					#endif

					// draw the beam
					fixed4 col = tex2D(_MainTex, i.uv) * i.color * _TintColor * _EmissionStrength;

					//fade the baem using _BeamFade
					//float slideFade = smoothstep(0, 0.01, saturate((1 - _BeamFade) * (i.uv.x - _BeamFade)));
					//float totalFade = 1 - smoothstep(0.01, 0.05, _BeamFade);
					//return col * slideFade * totalFade;

					float noise = tex2D(_NoiseTex, float2(i.uv.x, (i.uv.y / 10))).r;
					float noiseFade = smoothstep(0.5, .8, (1 - (_BeamFade * noise)));

					float beamFade = 1 - _BeamFade;
					return col * beamFade * noiseFade;
				}
				ENDCG
			}
		}
	}
}