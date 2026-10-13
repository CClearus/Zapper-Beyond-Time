Shader "Custom/URP_WireframeOutline"
{
    Properties
    {
        _WireColor ("Wire Color", Color) = (1,1,1,1)
        _WireThickness ("Wire Thickness", Range(0, 10)) = 1
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
        
        Pass
        {
            Name "WireframePass"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct v2g
            {
                float4 positionCS : SV_POSITION;
            };
            
            struct g2f
            {
                float4 positionCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _WireColor;
                float _WireThickness;
            CBUFFER_END
            
            v2g vert(Attributes input)
            {
                v2g output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }
            
            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f output;
                
                // Assign barycentric coordinates to each vertex
                output.positionCS = input[0].positionCS;
                output.barycentric = float3(1, 0, 0);
                triStream.Append(output);
                
                output.positionCS = input[1].positionCS;
                output.barycentric = float3(0, 1, 0);
                triStream.Append(output);
                
                output.positionCS = input[2].positionCS;
                output.barycentric = float3(0, 0, 1);
                triStream.Append(output);
            }
            
            half4 frag(g2f input) : SV_Target
            {
                // Calculate distance to nearest edge using barycentric coordinates
                float3 barys = input.barycentric;
                float minDist = min(barys.x, min(barys.y, barys.z));
                
                // Calculate screen-space derivative for consistent thickness
                float3 deltas = fwidth(barys);
                float minDelta = min(deltas.x, min(deltas.y, deltas.z));
                
                // Create the wireframe effect
                float thickness = minDelta * _WireThickness;
                float edge = step(minDist, thickness);
                
                // Return wire color at edges, transparent elsewhere
                half4 color = _WireColor;
                color.a *= edge;
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}