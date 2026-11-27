Shader "Hidden/NewtonVR/ColorGrading"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _Exposure;
            float _Contrast;
            float _Saturation;
            float _Temperature;
            float _Tint;
            float4 _ColorFilter;
            float _VignetteIntensity;
            float _VignetteSmoothness;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 ApplyWhiteBalance(float3 color, float temp, float tint)
            {
                // Temperature: -1 (cool/blue) to +1 (warm/orange)
                float3 balance = float3(1.0 + temp * 0.35, 1.0, 1.0 - temp * 0.35);
                // Tint: -1 (green) to +1 (magenta)
                balance *= float3(1.0 + tint * 0.2, 1.0 - tint * 0.3, 1.0 + tint * 0.2);
                return color * balance;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float3 col = src.rgb;

                // 1. Exposure (stops: 2^_Exposure)
                col *= exp2(_Exposure);

                // 2. Color Filter
                col *= _ColorFilter.rgb;

                // 3. White Balance (Temperature & Tint)
                col = ApplyWhiteBalance(col, _Temperature, _Tint);

                // 4. Contrast
                col = (col - 0.5) * _Contrast + 0.5;

                // 5. Saturation
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(float3(luma, luma, luma), col, _Saturation);

                // Clamp to prevent inverted negative colors from extreme contrast
                col = max(col, float3(0.0, 0.0, 0.0));

                // 6. Vignette
                if (_VignetteIntensity > 0.001)
                {
                    float2 coord = (i.uv - 0.5) * 2.0;
                    float dist = length(coord);
                    float vFactor = smoothstep(1.0, 1.0 - _VignetteSmoothness, dist * _VignetteIntensity);
                    col *= vFactor;
                }

                return float4(col, src.a);
            }
            ENDCG
        }
    }
}
