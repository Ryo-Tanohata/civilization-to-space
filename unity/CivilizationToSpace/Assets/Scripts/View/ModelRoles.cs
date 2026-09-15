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
    }
}
