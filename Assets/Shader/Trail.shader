Shader "Custom/FrostpunkTrailEffect"
{
    Properties
    {
        _MainTex ("Trail Texture", 2D) = "white" {}
        _TrailColor ("Trail Color", Color) = (1, 0.8, 0.3, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        _FadeSpeed ("Fade Speed", Range(0, 2)) = 0.5
        _TrailWidth ("Trail Width", Range(0.01, 0.5)) = 0.1
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3
        _NoiseScale ("Noise Scale", Range(0, 10)) = 2
        _FlickerSpeed ("Flicker Speed", Range(0, 5)) = 1
        _BlendMode ("Blend Mode", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        Blend SrcAlpha One // Additive blending for glow
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TrailColor;
            float _GlowIntensity;
            float _FadeSpeed;
            float _TrailWidth;
            float _EmissionStrength;
            float _NoiseScale;
            float _FlickerSpeed;
            float _BlendMode;
            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Smooth noise
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the trail texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Time-based animation
                float time = _Time.y * _FlickerSpeed;
                
                // Create animated noise for organic flicker
                float2 noiseUV = i.uv * _NoiseScale + float2(time * 0.1, time * 0.05);
                float noiseVal = smoothNoise(noiseUV);
                float flicker = 0.8 + noiseVal * 0.4;
                
                // Distance fade from center (creates the trail tapering effect)
                float centerDist = abs(i.uv.y - 0.5) * 2.0;
                float trailMask = 1.0 - smoothstep(_TrailWidth * 0.5, _TrailWidth * 2.0, centerDist);
                
                // Horizontal fade (trails fade as they extend)
                float horizontalFade = 1.0 - pow(i.uv.x, _FadeSpeed + 1.0);
                
                // Combine masks
                float finalMask = col.a * trailMask * horizontalFade * flicker;
                
                // Apply trail color with glow
                float3 glowColor = _TrailColor.rgb * _GlowIntensity * _EmissionStrength;
                
                // Create inner bright core
                float coreMask = 1.0 - smoothstep(0.0, _TrailWidth * 0.3, centerDist);
                float3 coreGlow = glowColor * 2.0 * coreMask;
                
                // Final color composition
                float3 finalColor = glowColor + coreGlow;
                finalColor *= i.color.rgb; // Vertex color support
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return fixed4(finalColor, finalMask * _TrailColor.a);
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/VertexLit"
}