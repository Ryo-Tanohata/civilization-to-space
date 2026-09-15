using System.Text;
using UnityEditor;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 外から持ってきた形が、Unity の中でどうなっているかを出す。
    ///
    /// **推測で直さない。** 大きさが合わない・絵が貼られないという症状は、
    /// 取り込みの単位、入れ子の作り、面と材質の対応のどれでも起きる。
    /// どれなのかを見てから直す。
    /// </summary>
    public static class ModelProbe
    {
        private static readonly string[] Names =
        {
            "ph_tree_broad", "ph_tree_quiver", "ph_fern", "ph_rock", "ph_dead_trunk",
            "tree_cone", "grass_large",
        };

        public static void RunFromCommandLine()
        {
            var report = new StringBuilder();
            foreach (var name in Names)
            {
                report.AppendLine("=== " + name);
                var prefab = Resources.Load<GameObject>("Nature/" + name);
                if (prefab == null)
                {
                    report.AppendLine("  読み込めない");
                    continue;
                }

                var item = Object.Instantiate(prefab);
                report.AppendLine("  根の拡大 " + item.transform.localScale
                                  + " 位置 " + item.transform.localPosition);

                foreach (var renderer in item.GetComponentsInChildren<Renderer>())
                {
                    var t = renderer.transform;
                    report.AppendLine("  面 " + t.name
                                      + " 拡大 " + t.localScale
                                      + " 世界拡大 " + t.lossyScale
                                      + " 大きさ " + renderer.bounds.size);

                    var mats = renderer.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                    {
                        report.AppendLine("    材質[" + i + "] "
                                          + (mats[i] != null ? mats[i].name : "なし")
                                          + " shader "
                                          + (mats[i] != null ? mats[i].shader.name : "-"));
                    }

                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        report.AppendLine("    面の数 " + filter.sharedMesh.subMeshCount
                                          + " 三角形 " + (filter.sharedMesh.triangles.Length / 3)
                                          + " UV " + filter.sharedMesh.uv.Length
                                          + " 頂点 " + filter.sharedMesh.vertexCount
                                          + " 生の大きさ " + filter.sharedMesh.bounds.size);
                    }
                }

                Object.DestroyImmediate(item);
            }

            report.AppendLine("=== 絵が見つかるか");
            foreach (var stem in new[]
                     {
                         "ph_tree_broad_main", "ph_tree_broad_leaves", "ph_tree_broad_branches",
                         "ph_tree_quiver_leaf", "ph_tree_quiver_trunk",
                         "ph_fern_main", "ph_rock_main", "ph_dead_trunk_main",
                     })
            {
                var texture = Resources.Load<Texture2D>("Nature/" + stem);
                report.AppendLine("  " + stem + " -> "
                                  + (texture != null ? texture.width + "x" + texture.height : "見つからない"));
            }

            Debug.Log("[ModelProbe]\n" + report);
            EditorApplication.Exit(0);
        }
    }
}
