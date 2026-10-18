Shader "Pesca/MarCaribe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fotografía", 2D) = "white" {}
        _Intensity ("Movimiento en píxeles", Range(0, 8)) = 2.6
        _GlintStrength ("Reflejos suaves", Range(0, 0.3)) = 0.188
        _Waterline ("Límite del agua", Range(0.05, 0.5)) = 0.478
        [HideInInspector] _RippleTime ("Tiempo", Float) = 0
        [HideInInspector] _RippleUvPerPixel ("UV por píxel", Vector) = (0.001, 0.001, 0, 0)
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _RippleUvPerPixel;
            float _Intensity, _GlintStrength, _Waterline, _RippleTime;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Ondas direccionales de diferentes longitudes y velocidades.
            // La derivada produce una pendiente para la refracción y la luz.
            void Onda(float2 p, float2 direccion, float frecuencia, float velocidad,
                float amplitud, float fase, inout float2 pendiente, inout float altura)
            {
                float angulo = dot(p, direccion) * frecuencia - _RippleTime * velocidad + fase;
                float seno, coseno;
                sincos(angulo, seno, coseno);
                pendiente += direccion * coseno * amplitud;
                altura += seno * amplitud;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float profundidad = saturate((_Waterline - uv.y) / _Waterline);
                // Coordenadas de la foto, independientes del recorte de pantalla.
                // La región por encima del límite permanece inmóvil.
                float mascara = smoothstep(0.0, 0.16, profundidad);
                mascara *= smoothstep(0.0, 0.018, min(uv.x, 1.0 - uv.x));
                mascara *= smoothstep(0.0, 0.018, uv.y);
                if (mascara <= 0.0 || _Intensity <= 0.0) return tex2D(_MainTex, uv);

                // Proyección aproximada del plano del mar: ondas pequeñas a lo lejos.
                float distancia = 0.35 + profundidad;
                float2 p = float2((uv.x - 0.5) * 3.0, 1.0) / distancia;
                float2 pendiente = 0;
                float altura = 0;
                Onda(p, float2(0.28, 0.96), 8.7, 1.12, 0.52, 0.4, pendiente, altura);
                Onda(p, float2(-0.46, 0.89), 14.1, 1.63, 0.27, 2.1, pendiente, altura);
                Onda(p, float2(0.76, 0.65), 22.3, 2.07, 0.14, 4.3, pendiente, altura);
                Onda(p, float2(-0.85, 0.53), 37.9, 2.73, 0.07, 1.2, pendiente, altura);

                float amplitud = _Intensity * mascara * lerp(0.12, 1.0, profundidad);
                float2 refraccion = float2(pendiente.x, altura * 0.45) * amplitud * _RippleUvPerPixel.xy;

                // Advección local en dos fases complementarias: reinicios invisibles.
                // Desplazamiento acotado para preservar el detalle de la fotografía.
                float fase = frac(_RippleTime * 0.11 + dot(p, float2(0.07, 0.13)));
                float faseB = frac(fase + 0.5);
                float mezcla = 0.5 - 0.5 * cos(fase * 6.2831853);
                float2 corriente = float2(0.45 + pendiente.x * 0.18, -0.8);
                corriente *= _RippleUvPerPixel.xy * amplitud * 2.0;
                float2 uvA = saturate(uv + refraccion - corriente * (fase - 0.5));
                float2 uvB = saturate(uv + refraccion - corriente * (faseB - 0.5));
                fixed4 color = lerp(tex2D(_MainTex, uvB), tex2D(_MainTex, uvA), mezcla);

                // Luz tenue sobre las crestas existentes: sin espuma falsa ni brillo plástico.
                float cresta = pow(saturate(0.5 + dot(pendiente, float2(-0.35, 0.65))), 6.0);
                float detalle = smoothstep(0.4, 0.85, dot(color.rgb, float3(0.2126, 0.7152, 0.0722)));
                float luz = (cresta * (0.35 + detalle) - 0.035) * _GlintStrength * mascara;
                color.rgb = saturate(color.rgb + luz * float3(0.72, 0.94, 1.0));
                return color;
            }
            ENDCG
        }
    }
}
