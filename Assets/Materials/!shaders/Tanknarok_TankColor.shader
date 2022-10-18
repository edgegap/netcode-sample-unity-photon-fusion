Shader "Tanknarok/TankColor" {
	Properties {
		_Transition("Transition", Range(0, 1)) = 0  // The transition between 0 = normal and 1 = flashing (taking damage)
		_TransitionColor("Transition Color", Color) = (1,1,1,1)
		_MainTex("Palette", 2D) = "white" {}
		_RimColor("RimColor", Color) = (1,1,1,1)
		_SilhouetteColor("Silhouette Color", Color) = (1,1,1,1)
		
		
		_TextColor("Text Color", Color) = (1,1,1,1)
		[HDR] _EnergyColor("Energy Color", Color) = (1,1,1,1)
		[HideInInspector] _FlashEmissionMultiplier("EmissionMultiplier", Float) = 1.2
		[HideInInspector] _RimStrength("RimStrength", Float) = 3.0
	}

	CGINCLUDE

	float4 _SilhouetteColor;
	float _FlashEmissionMultiplier;
	float _Transition;
	float4 _TransitionColor;

	ENDCG

	SubShader {
		Tags { 
			"Queue" = "Geometry+1"
			"RenderType"="Opaque" 
		}
		LOD 200

		// outline pass
		Pass {
			Name "OUTLINE"
			Tags { 
				"LightMode" = "Always" 
			}

			Cull Off
			ZWrite Off
			ZTest Always
			ColorMask RGB

			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			half4 frag(v2f i) :COLOR {
				i.color = lerp(_SilhouetteColor, _TransitionColor, _Transition);
				return i.color;
			}

			ENDCG
		}

		// standard unity surface shader
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		
		sampler2D _MainTex;
		float4 _RimColor;
		float _RimStrength;

		struct Input {
			float2 uv_MainTex;
			float3 worldNormal;
			float3 viewDir;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Metallic = 0;
			o.Smoothness = 0;
			o.Alpha = 1;

			fixed4 damageColor = _TransitionColor * _FlashEmissionMultiplier;
			fixed4 c = lerp(tex2D(_MainTex, IN.uv_MainTex), damageColor, _Transition);

			//get the dot product between the normal and the view direction
			float fresnel = dot(IN.worldNormal, IN.viewDir);
			//invert the fresnel so the big values are on the outside
			fresnel = saturate(1 - fresnel);
			//raise the fresnel value to the exponents power to be able to adjust it
			fresnel = pow(fresnel, _RimStrength);

			// this is the old fresnel which produces visual glitches
			//fresnel = pow(1.0 - max(0, dot(normalize(IN.viewDir), o.Normal)), _RimStrength);
			float3 fresnelColor = fresnel * _RimColor.rgb;

			o.Albedo = c.rgb + fresnelColor;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
