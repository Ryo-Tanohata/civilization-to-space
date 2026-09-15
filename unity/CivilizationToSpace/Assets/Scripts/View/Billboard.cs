using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// いつもカメラのほうを向く板。
    ///
    /// **写真計測の木は、面を減らすと葉が成立しない。**
    /// 葉は1枚ずつの板に絵を貼ったもので、Web に載る容量まで枚数を落とすと
    /// 枝だけの木になる。そこで木を丸ごと1枚の絵に焼き、板に貼っている。
    /// 板はカメラを向いていないと、横から見たときに紙のように消える。
    ///
    /// **縦は倒さない。** 上下にも向けると、見下ろしたときに木が寝てしまう。
    /// 横向きだけ合わせる。木は地面から立っているものだからである。
    ///
    /// **近づけば板だと分かる。** この作品では木は常に遠くにあり、
    /// カメラも時代ごとに決まった場所から動かないので成り立っている。
    /// </summary>
    public sealed class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var toCamera = camera.transform.position - transform.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }
    }
}
