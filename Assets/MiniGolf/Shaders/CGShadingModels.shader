// Shader didatico para a disciplina de Computacao Grafica.
// Implementa, num unico arquivo, os modelos de reflexao classicos para que
// seja possivel comparar a MATEMATICA de cada um lado a lado:
//
//   _Mode = 0  Lambert       -> difuso puro:            I = N . L
//   _Mode = 1  Phong         -> especular com reflexao: (R . V)^shininess
//   _Mode = 2  Blinn-Phong   -> especular com halfway:  (N . H)^shininess
//
// O modelo PBR nao esta aqui: usamos o "Standard" shader nativo do Unity,
// trocado pelo controller, para comparar o classico com o fisicamente correto.
Shader "CG/ShadingModels"
{
    Properties
    {
        _Color       ("Cor difusa (albedo)", Color) = (0.6, 0.7, 1.0, 1.0)
        _SpecTint    ("Cor especular", Color) = (1.0, 1.0, 1.0, 1.0)
        _Shininess   ("Brilho (expoente especular)", Range(1, 128)) = 32
        [Enum(Lambert,0,Phong,1,BlinnPhong,2)] _Mode ("Modelo", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"   // fornece _LightColor0 e _WorldSpaceLightPos0

            fixed4 _Color;
            fixed4 _SpecTint;
            float  _Shininess;
            float  _Mode;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNormal: TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Vetores base do modelo de iluminacao.
                float3 N = normalize(i.worldNormal);                          // normal da superficie
                float3 L = normalize(_WorldSpaceLightPos0.xyz);               // direcao da luz (directional)
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);      // direcao para a camera

                // Termo difuso (Lambert): quanto a face "encara" a luz.
                float NdotL = max(0.0, dot(N, L));
                fixed3 diffuse = _Color.rgb * _LightColor0.rgb * NdotL;

                // Ambiente vindo do RenderSettings (liga com os presets de luz).
                fixed3 ambient = _Color.rgb * ShadeSH9(float4(N, 1.0));

                // Termo especular depende do modelo escolhido.
                fixed3 specular = fixed3(0, 0, 0);

                if (_Mode >= 0.5 && _Mode < 1.5)
                {
                    // PHONG: reflete a luz na normal e compara com a camera (R . V).
                    float3 R = reflect(-L, N);
                    float spec = pow(max(0.0, dot(R, V)), _Shininess);
                    specular = _SpecTint.rgb * _LightColor0.rgb * spec * (NdotL > 0.0 ? 1.0 : 0.0);
                }
                else if (_Mode >= 1.5)
                {
                    // BLINN-PHONG: usa o vetor halfway H entre luz e camera (N . H).
                    // Mais barato que Phong (sem reflect) e comum em tempo real.
                    float3 H = normalize(L + V);
                    float spec = pow(max(0.0, dot(N, H)), _Shininess);
                    specular = _SpecTint.rgb * _LightColor0.rgb * spec * (NdotL > 0.0 ? 1.0 : 0.0);
                }
                // _Mode == 0 (Lambert): sem especular.

                return fixed4(ambient + diffuse + specular, _Color.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
