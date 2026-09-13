// 地球に重ねる半透明の層（次の時代の地表・雲・大気）。
//
// **なぜStandardを使わないのか。**
// WebGLビルドでは Standard の地色（_Color）も発光（_EmissionColor）も画面に出ない。
// 実行中のビルドで値を出力させたところ、値は正しく渡っていた。出ていないのは描く側である。
// 常時含めるシェーダーの登録・雛形の材質・バリアントコレクションの3つを試したが、
// いずれも効かなかった。一方、自作シェーダーはWebGLでも確かに効いている。
//
// **キーワードで分岐させない。**
// Standardは _EMISSION や _NORMALMAP の有無で実体が分かれ、その分かれた先が
// ビルドに残らないことが問題だった。ここでは分岐を作らず、実体をひとつだけにする。
// 不透明は別のシェーダー（PlanetOpaque）として分けてある。
//
// 明るさの計算はStandardと同じ照明モデルを使うので、見え方は揃う。
Shader "CivilizationToSpace/PlanetFade"
{
    Properties
    {
        _Color ("色", Color) = (1,1,1,1)
        _MainTex ("地表の絵", 2D) = "white" {}
        _EmissionColor ("自ら光る色", Color) = (0,0,0,0)
        _EmissionMap ("自ら光る絵", 2D) = "white" {}
        _BumpMap ("凹凸", 2D) = "bump" {}
        _BumpScale ("凹凸の強さ", Float) = 0
        _Glossiness ("滑らかさ", Range(0,1)) = 0
        _Metallic ("金属らしさ", Range(0,1)) = 0
        _CutCenter ("削る中心（ワールド座標）", Vector) = (0,0,0,0)
        _CutRadius ("削る半径", Float) = 0

        // 次の時代の絵。_Blend で今の絵と混ぜる。雲の移り変わりに使う。
        _MainTexNext ("次の絵", 2D) = "white" {}
        _Blend ("次の絵の混ざり具合", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MainTexNext;
        half _Blend;
        sampler2D _EmissionMap;
        sampler2D _BumpMap;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _BumpScale;
        half _Glossiness;
        half _Metallic;
        float4 _CutCenter;
        half _CutRadius;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_EmissionMap;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // 衝突で抉れた部分を描かない。中心からこの半径の内側を落とす。
            // 球を小さくするのではなく、実際に欠けた形を作るためである。
            if (_CutRadius > 0)
            {
                clip(distance(IN.worldPos, _CutCenter.xyz) - _CutRadius);
            }

            // 時代が移るあいだ、今の絵と次の絵を混ぜる。
            // 瞬時に差し替えると、そのフレームだけ雲の形が飛ぶ。
            fixed4 c = lerp(
                tex2D(_MainTex, IN.uv_MainTex),
                tex2D(_MainTexNext, IN.uv_MainTex),
                _Blend) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb * _EmissionColor.rgb;

            // 凹凸の絵を渡していない材質では _BumpScale が0のままで、平らな法線になる。
            float3 packed = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            o.Normal = normalize(lerp(float3(0, 0, 1), packed, _BumpScale));
        }
        ENDCG
    }

    FallBack "Diffuse"
}
