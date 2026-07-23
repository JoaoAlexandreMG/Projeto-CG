// Shader de REPLACEMENT para os modos de depuracao visual (Computacao Grafica).
// A camera aplica este shader em TODOS os objetos (Camera.SetReplacementShader),
// e um inteiro global _CGDebugMode escolhe o que visualizar:
//
//   1 = Normais : normal do mundo mapeada em cor (RGB = XYZ)
//   2 = UVs     : coordenadas de textura (R = U, G = V)
//   3 = Depth   : profundidade (distancia ate a camera) em tons de cinza
//
// Sem Properties: os valores vem de variaveis GLOBAIS setadas pelo controller
// (Shader.SetGlobalInt / SetGlobalFloat), pois no replacement nao ha material.
Shader "CG/DebugViews"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            int   _CGDebugMode;
            float _CGDepthScale;   // escala do cinza no modo Depth

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNormal: TEXCOORD0;
                float2 uv         : TEXCOORD1;
                float  eyeDepth   : TEXCOORD2;   // distancia ao longo do eixo da camera
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;   // view space Z (positivo para frente)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_CGDebugMode == 1)
                {
                    // Normais: [-1,1] -> [0,1] para virar cor visivel.
                    float3 n = normalize(i.worldNormal) * 0.5 + 0.5;
                    return fixed4(n, 1);
                }
                else if (_CGDebugMode == 2)
                {
                    // UVs: U no vermelho, V no verde. Mostra como a textura e mapeada.
                    return fixed4(frac(i.uv.x), frac(i.uv.y), 0, 1);
                }
                else
                {
                    // Depth: mais perto = claro, mais longe = escuro.
                    float g = saturate(i.eyeDepth * _CGDepthScale);
                    return fixed4(1 - g, 1 - g, 1 - g, 1);
                }
            }
            ENDCG
        }
    }
    FallBack Off
}
