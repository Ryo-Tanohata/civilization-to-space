namespace CivilizationToSpace.Sim
{
    /// <summary>
    /// 形成の段階。台本ではなく、計算の状態から遷移する。
    /// 何秒で次へ進むかは決めていない。条件を満たしたときに進む。
    /// </summary>
    public enum SimPhase
    {
        /// <summary>微惑星が重力で寄り集まり、ぶつかって合体していく。</summary>
        Accretion,

        /// <summary>ひとつの塊が全体の大半を占めた。原始地球と呼べる状態。</summary>
        ProtoEarth,

        /// <summary>衝突天体が原始地球へ向かって落ちてきている。</summary>
        Impactor,

        /// <summary>衝突した直後。破片が放出された。</summary>
        Impact,

        /// <summary>破片が周回しながら集まっていく。</summary>
        MoonForming,

        /// <summary>地球と月が残り、落ち着いた。</summary>
        Settled
    }

    /// <summary>
    /// 天体の種別。色分けと、衝突時の扱いを分けるために持つ。
    /// 物理量ではない。質量と半径だけが運動を決める。
    /// </summary>
    public enum BodyKind
    {
        Planetesimal,
        Earth,
        Impactor,
        Debris,
        Moon
    }
}
