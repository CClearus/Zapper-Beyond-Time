Shader "Custom/DetailedGrid"
{
    Properties
    {
        [Header(Grid Settings)]
        _GridWidth ("Grid Width", Float) = 10
        _GridHeight ("Grid Height", Float) = 10
        _CellSize ("Cell Size", Float) = 1
        
        [Header(Grid Visual)]
        _MainGridColor ("Main Grid Color", Color) = (1,1,1,1)
        _SubGridColor ("Sub Grid Color", Color) = (0.7,0.7,0.7,1)
        _BackgroundColor ("Background Color", Color) = (0.1,0.1,0.1,1)
        _MainGridThickness ("Main Grid Thickness", Range(0.001, 0.1)) = 0.02
        _SubGridThickness ("Sub Grid Thickness", Range(0.001, 0.05)) = 0.01
        _SubGridDivisions ("Sub Grid Divisions", Int) = 4
        
        [Header(Transparency)]
        _Alpha ("Overall Alpha", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            float _GridWidth;
            float _GridHeight;
            float _CellSize;
            fixed4 _MainGridColor;
            fixed4 _SubGridColor;
            fixed4 _BackgroundColor;
            float _MainGridThickness;
            float _SubGridThickness;
            int _SubGridDivisions;
            float _Alpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Scale UV based on grid dimensions
                o.uv = v.uv * float2(_GridWidth, _GridHeight);
                
                return o;
            }
            
            float GetGrid(float2 uv, float lineWidth)
            {
                // Create grid lines that are perfectly centered
                float2 grid = abs(frac(uv + 0.5) - 0.5);
                float gridLine = min(grid.x, grid.y);
                return smoothstep(lineWidth, 0.0, gridLine);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Main grid lines - one line per cell
                float mainGrid = GetGrid(uv, _MainGridThickness);
                
                // Sub grid lines - perfectly aligned within each main cell
                float2 cellUV = frac(uv + 0.5);  // Get UV within current cell (0-1 range)
                float2 subUV = cellUV * _SubGridDivisions;
                float subGrid = GetGrid(subUV, _SubGridThickness);
                
                // Start with background color
                fixed3 finalColor = _BackgroundColor.rgb;
                
                // Blend sub grid
                finalColor = lerp(finalColor, _SubGridColor.rgb, subGrid * _SubGridColor.a);
                
                // Blend main grid (on top)
                finalColor = lerp(finalColor, _MainGridColor.rgb, mainGrid * _MainGridColor.a);
                
                // Calculate alpha
                float totalGrid = max(mainGrid, subGrid * 0.5);
                float alpha = max(totalGrid, _BackgroundColor.a);
                
                // Apply overall alpha multiplier
                alpha *= _Alpha;
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}   