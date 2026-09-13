// 地球・月の地表（不透明）。
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
// 半透明は別のシェーダー（PlanetFade）として分けてある。
//
// 明るさの計算はStandardと同じ照明モデルを使うので、見え方は揃う。
Shader "CivilizationToSpace/PlanetOpaque"
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

        // 次の時代の絵。_Blend で今の絵と混ぜる。
        _MainTexNext ("次の地表の絵", 2D) = "white" {}
        _EmissionMapNext ("次の自ら光る絵", 2D) = "white" {}
        _BumpMapNext ("次の凹凸", 2D) = "bump" {}
        _Blend ("次の絵の混ざり具合", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _EmissionMap;
        sampler2D _BumpMap;
        sampler2D _MainTexNext;
        sampler2D _EmissionMapNext;
        sampler2D _BumpMapNext;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _BumpScale;
        half _Glossiness;
        half _Metallic;
        half _Blend;
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

            // 時代の移り変わりは、ここで絵どうしを混ぜる。
            //
            // **なぜ層を重ねないのか。**
            // 以前は次の時代の地表を半透明の殻として上に重ね、不透明度を上げていた。
            // 重ね終わったところで殻を消して本体の絵を差し替えるのだが、
            // 半透明の層と不透明の層は明るさの出方が揃わないため、
            // その差し替えの1フレームだけ見た目が飛んでいた。
            // 時代が変わるたびに画面が一瞬暗くなる、という形で出ていた。
            //
            // 1枚の中で絵を混ぜれば、混ぜ終わった姿と差し替えた姿が同じ式になる。
            // lerp(A, B, 1) と B は等しいので、差し替えても何も変わらない。
            fixed4 albedo = lerp(
                tex2D(_MainTex, IN.uv_MainTex),
                tex2D(_MainTexNext, IN.uv_MainTex),
                _Blend);
            fixed4 c = albedo * _Color;
            o.Albedo = c.rgb;
            o.Alpha = 1;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            fixed3 glow = lerp(
                tex2D(_EmissionMap, IN.uv_EmissionMap).rgb,
                tex2D(_EmissionMapNext, IN.uv_EmissionMap).rgb,
                _Blend);
            o.Emission = glow * _EmissionColor.rgb;

            // 凹凸の絵を渡していない材質では _BumpScale が0のままで、平らな法線になる。
            float3 packed = lerp(
                UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap)),
                UnpackNormal(tex2D(_BumpMapNext, IN.uv_BumpMap)),
                _Blend);
            o.Normal = normalize(lerp(float3(0, 0, 1), packed, _BumpScale));
        }
        ENDCG
    }

    FallBack "Diffuse"
}
