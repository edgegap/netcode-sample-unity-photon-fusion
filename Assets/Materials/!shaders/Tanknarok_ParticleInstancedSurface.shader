Shader "Tanknarok/ParticleInstancedSurface" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        // And generate the shadow pass with instancing support
        //#pragma surface surf Standard nolightmap nometa noforwardadd keepalpha fullforwardshadows addshadow vertex:vert
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 4.5
        
        // Enable instancing for this shader
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:vertInstancingSetup
        #pragma exclude_renderers gles
        #include "UnityStandardParticleInstancing.cginc"
        
        sampler2D _MainTex;
        struct Input {
            float2 uv_MainTex;
            fixed4 vertexColor;
        };
        
        fixed4 _Color;
        
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            vertInstancingColor(o.vertexColor);
            vertInstancingUVs(v.texcoord, o.uv_MainTex);
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * IN.vertexColor * _Color;
            o.Albedo = c.rgb;

            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}