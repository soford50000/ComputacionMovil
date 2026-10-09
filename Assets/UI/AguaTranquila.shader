Shader "Pesca/AguaTranquila"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fotografía", 2D) = "white" {}
        _Intensity ("Amplitud en píxeles de pantalla", Range(0, 5)) = 2.5
        [HideInInspector] _RippleTime ("Tiempo de las ondas", Float) = 0
        [HideInInspector] _RippleUvPerPixel ("UV por píxel de pantalla", Vector) = (0.001, 0.001, 0, 0)
        _Waterline ("Límite del agua", Range(0.05, 0.5)) = 0.34
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _RippleUvPerPixel;
            float _Intensity, _RippleTime, _Waterline;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // Sin deformación por encima del agua ni en el borde de la foto.
                float profundidad = saturate((_Waterline - uv.y) / max(_Waterline, 0.001));
                float mascara = smoothstep(0.0, 0.22, profundidad);
                mascara *= smoothstep(0.0, 0.015, min(uv.x, 1.0 - uv.x));
                mascara *= smoothstep(0.0, 0.015, uv.y);
                float t = _RippleTime;
                float onda = sin(uv.y * 190.0 + uv.x * 15.0 + t * 2.0);
                onda += 0.45 * sin(uv.y * 310.0 - uv.x * 23.0 - t * 1.3);
                float2 desplazamiento = float2(onda, 0.5 * sin(uv.y * 220.0 + uv.x * 18.0 + t));
                uv += desplazamiento * _RippleUvPerPixel.xy * _Intensity * mascara
                    * lerp(0.45, 1.0, profundidad);
                return tex2D(_MainTex, saturate(uv));
            }
            ENDCG
        }
    }
}
