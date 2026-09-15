using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 外から持ってきた形の、面ごとの役。
    ///
    /// **持ってきた形は素材側の色を連れてくる。** そのまま置くと、
    /// 水色の木や桃色の幹が並び、時代ごとに決めた色調から外れる。
    /// 面ごとに「葉」「幹」「岩」のどれかを覚えておき、
    /// 置いたあとで時代の色へ塗り直す。
    ///
    /// 役は持ってきた形の材質名から決める。Kenney の素材では
    /// leafsGreen / woodBark のように、名前が中身を表している。
    /// </summary>
    public sealed class ModelRoles : MonoBehaviour
    {
        /// <summary>面と同じ並び順の役の名前。<see cref="SurfaceView"/> が読む。</summary>
        public string[] Roles;

        /// <summary>
        /// 真なら、置いたあとの塗り直しをしない。
        ///
        /// **写真の絵を貼った形を、時代の色で塗りつぶしてはいけない。**
        /// 遠さで色を薄める処理は、面の名前から役を引いて材質を差し替える。
        /// 写真から起こした形は役の名前を持たないので、
        /// 行き先の分からないものとして地面の色で塗られていた。
        /// 木が砂色の塊になって見えていたのはこれが原因である。
        /// </summary>
        public bool KeepOwnMaterials;
    }
}
