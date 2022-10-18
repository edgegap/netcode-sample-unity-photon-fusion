Shader "Tanknarok/Standard" {
	Properties{
        [Header(Main texture)]
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "black" {}
		_MainColor("Main Color (if no texture)", Color) = (0,0,0,0)
	    _Smoothness("Smoothness", Range(0,1)) = 0
		
		[Space]
		[Header(Variant offset)]
		[IntRange] _NumVariants("NumVariants", Range(1,16)) = 1
		_Variant("Variant", float) = 0
        
        [Space]
        [Header(Worldspace Gradient)]
		_WSOuterColor("Outer Color   (Transparent disables)", Color) = (0,0,0,0)
		_WSVerticalColor("Vertical Color   (Transparent disables)", Color) = (0,0,0,0)
		_WSVerticalThreshold("Vertical Threshold", Range(0,10)) = 10
		[HideInInspector] _WSMapWidth("MapWidth", Float) = 26.35	// defult tanknarok mapwidth
		[HideInInspector] _WSMapHeight("MapHeight", Float) = 17.7	// defult tanknarok mapheight
		_WSInnerFudge("Inner Fudge", Range(0, 2)) = 0.8
		_WSOuterFudge("Outer Fudge", Range(0, 2)) = 1.2
		
	//	[Space]
	//	[Header(Fresnel)]  // fresnel was used by ice crystals at one point, but was later replaced with just smoothness
	//	_RimColor("RimColor", Color) = (0,0,0,0)
	//	_RimAngle("RimAngle", Range(0.0, 10.0)) = 3.0
		
	//	[Space]
	//	[Header(Sparkles)]
	//	[NoScaleOffset] _SparkleTex("Sparkle Texture", 2D) = "black" {}
	//	[HideInInspector] _SparkleScale("Sparkle Scale", float) = 0.5
	//	_SparkleStrength("Sparkle Strength", Range(0, 20)) = 1
		
		[Space]
		[Header(Splatmap)]
        //_SplatOpacity("Splatmap Opacity", Range(0, 1)) = 1  // I get an error if this comes after the splat texture??? 
        [NoScaleOffset] _SplatTexture ("Splatmap Texture   (None disables)", 2D) = "black" {}
        [HideInInspector] _SplatTextureScale ("Splatmap Scale", Float) = 60  // needs to match the size of the plane that catches splat hit raycasts
	}

	SubShader{
		Tags {
			"RenderType" = "Opaque"
		}
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		float4 _MainColor;
		float _Smoothness;

		struct Input {
			float2 uv_MainTex;  // altering this will ruin the offset/tiling on the image; you cant have variables start with uv_, you big dumb sack of literal garbage
			float3 worldPos;
			float3 worldNormal;
			float3 viewDir;
		};

		float _NumVariants;

		float4 _WSOuterColor;
		float4 _WSVerticalColor;
		float _WSVerticalThreshold;
		float _WSMapWidth;
		float _WSMapHeight;
		float _WSInnerFudge;
		float _WSOuterFudge;
		
	//	float4 _RimColor;
	//	float _RimAngle;
		
	//	sampler2D _SparkleTex;
	//	float _SparkleScale;
	//	float _SparkleStrength;
	//	
        sampler2D _SplatTexture;
        //float _SplatOpacity;
        float _SplatTextureScale;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float, _Variant)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {
			o.Metallic = 0;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
            _WSVerticalThreshold = max(_WSVerticalThreshold, 0);

            // variant offset
			int nv = floor(_NumVariants);
			int v = floor(abs(UNITY_ACCESS_INSTANCED_PROP(Props, _Variant)));

			float2 xy = IN.uv_MainTex.xy;
			xy.x += fmod(v, nv) * (1.0 / nv);

			fixed3 col = tex2D(_MainTex, xy).rgb + _MainColor.rgb;

			// worldspacegradient
            float distance = abs((IN.worldPos.x*IN.worldPos.x) / (_WSMapWidth*_WSMapWidth)) + abs((IN.worldPos.z*IN.worldPos.z) / (_WSMapHeight*_WSMapHeight));
            col = lerp(col, _WSVerticalColor.rgb, smoothstep(0, _WSVerticalThreshold, (IN.worldPos.y * _WSVerticalColor.a)));  // vertical color  (transparent disables)
            col = lerp(col, _WSOuterColor.rgb, smoothstep(_WSInnerFudge, _WSOuterFudge, (distance * _WSOuterColor.a)));     // inside or outside color  (transparent disables)

    //      // fresnel
	//		float fresnel = dot(IN.worldNormal, IN.viewDir); //get the dot product between the normal and the view direction
	//		fresnel = saturate(1 - fresnel); //invert the fresnel so the big values are on the outside
	//		fresnel = pow(fresnel, _RimAngle); //raise the fresnel value to the exponents power to be able to adjust it
	//		float3 fresnelColor = fresnel * _RimColor.rgb;
	//		col = col + fresnelColor;
			
	//		// sparkles
	//		float3 viewDir = normalize(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz);
	//		float sparkle = tex2D(_SparkleTex, (IN.worldPos.xz * _SparkleScale) + viewDir.xz).r * tex2D(_SparkleTex, (IN.worldPos.xz * _SparkleScale) - viewDir.xz).g;
	//		col = col +  (sparkle * _SparkleStrength);
			
            // splatmap
            float2 splatPosition = (IN.worldPos.xz * -1) + (_SplatTextureScale * 0.5);
            float4 splatColor = tex2D(_SplatTexture, (splatPosition / _SplatTextureScale));
            splatColor = lerp(splatColor, float4(0,0,0,0), smoothstep(0, 1, IN.worldPos.y - 3));  // quickfix: fade out the splat colors above a certain y-height. Quick and dirty; might want to parameterize those magic numbers there.
            col = lerp(col, splatColor.rgb, splatColor.a);
			
            o.Albedo = col;

		}
		ENDCG
	}
	FallBack "Diffuse"
}
