Shader "Tanknarok/Stripes" {
	Properties {
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		[PerRendererData] _Offset("Offset", Float) = 0
	}
	SubShader {
		Tags { 
			"RenderType"="Opaque"
		}
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			float2 uv_MainTex;
		};

		sampler2D _MainTex;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		    UNITY_DEFINE_INSTANCED_PROP(float, _Offset)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Metallic = 0;
			o.Smoothness = 0;

			float2 xy = (IN.uv_MainTex + (UNITY_ACCESS_INSTANCED_PROP(Props, _Offset) * float2(0, 1)));
            fixed3 col = tex2D(_MainTex, xy).rgb;

			o.Albedo = col;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
