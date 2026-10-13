Shader "Custom/URP/LineRendererParticles"
{
    Properties
    {
        [Header(Particle Cluster Settings)]
        _ParticleColor ("Particle Color", Color) = (1, 0.9, 0.2, 1)
        _ParticleIntensity ("Particle Intensity", Range(0, 20)) = 5
        _ParticleSpeed ("Particle Speed", Range(0, 5)) = 1.5
        _ParticleSize ("Particle Size", Range(0.01, 0.3)) = 0.05
        _ParticlesPerCluster ("Particles Per Cluster", Range(3, 30)) = 10
        
        [Header(Cluster Settings)]
        _ClusterCount ("Cluster Count", Range(1, 10)) = 3
        _ClusterSpread ("Cluster Spread", Range(0, 0.5)) = 0.15
        _ClusterTightness ("Cluster Tightness", Range(0, 1)) = 0.7
        
        [Header(Animation)]
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 5
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.4
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        
        [Header(Glow Settings)]
        _GlowRadius ("Glow Radius", Range(0, 0.5)) = 0.15
        _GlowFalloff ("Glow Falloff", Range(1, 10)) = 3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ParticleColor;
                float _ParticleIntensity;
                float _ParticleSpeed;
                float _ParticleSize;
                float _ParticlesPerCluster;
                float _ClusterCount;
                float _ClusterSpread;
                float _ClusterTightness;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _PulseSpeed;
                float _PulseAmount;
                float _GlowRadius;
                float _GlowFalloff;
            CBUFFER_END
            
            // Hash function for pseudo-random numbers
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }
            
            float hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            // Noise function
            float noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), f);
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _ParticleSpeed;
                
                float3 finalColor = float3(0, 0, 0);
                float totalAlpha = 0.0;
                
                // Generate multiple clusters
                for(int c = 0; c < (int)_ClusterCount; c++)
                {
                    // Cluster center position along the line
                    float clusterOffset = hash(float(c) * 7.891);
                    float clusterPos = frac(time * 0.3 + clusterOffset);
                    
                    // Pulse effect for cluster
                    float pulse = sin(_Time.y * _PulseSpeed + clusterOffset * 6.28) * 0.5 + 0.5;
                    pulse = lerp(1.0, pulse, _PulseAmount);
                    
                    // Fade in at start and fade out at end for smooth looping
                    float fadeIn = smoothstep(0.0, 0.1, clusterPos);
                    float fadeOut = smoothstep(1.0, 0.9, clusterPos);
                    float fadeFactor = fadeIn * fadeOut;
                    
                    // Generate particles within cluster
                    for(int i = 0; i < (int)_ParticlesPerCluster; i++)
                    {
                        float particleId = float(c) * 100.0 + float(i);
                        float randomX = hash(particleId * 1.234) * 2.0 - 1.0;
                        float randomY = hash(particleId * 5.678) * 2.0 - 1.0;
                        
                        // Offset from cluster center (tighter clustering)
                        float offsetX = randomX * _ClusterSpread * (1.0 - _ClusterTightness);
                        float offsetY = randomY * _ClusterSpread * 0.5;
                        
                        // Particle position
                        float particleX = clusterPos + offsetX;
                        float particleY = 0.5 + offsetY;
                        
                        // Distance from current pixel to particle
                        float2 dist = float2(IN.uv.x - particleX, IN.uv.y - particleY);
                        float d = length(dist);
                        
                        // Core particle
                        float particle = 1.0 - smoothstep(0.0, _ParticleSize, d);
                        
                        // Glow around particle
                        float glow = exp(-d / _GlowRadius * _GlowFalloff);
                        
                        // Flicker per particle
                        float flicker = noise(_Time.y * _FlickerSpeed + particleId);
                        flicker = lerp(1.0, flicker, _FlickerAmount);
                        
                        // Combine particle and glow
                        float particleStrength = (particle + glow * 0.5) * flicker * pulse * fadeFactor;
                        
                        // Add variation in intensity per particle
                        float intensityVariation = hash(particleId * 9.876) * 0.3 + 0.7;
                        particleStrength *= intensityVariation;
                        
                        totalAlpha += particleStrength;
                        finalColor += _ParticleColor.rgb * particleStrength;
                    }
                }
                
                // Apply intensity and clamp
                finalColor *= _ParticleIntensity;
                totalAlpha = saturate(totalAlpha);
                
                // HDR glow effect
                finalColor = finalColor * (1.0 + totalAlpha * 0.5);
                
                return float4(finalColor, totalAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}