"""試作（docs/prototypes/mammoth.html）の距離関数から、毛皮込みの網目を焼く。

試作はシェーダーで距離関数を光線でたどって描いている。Unity の地表は
球と円柱の組み立てなので、毛の分だけ表面を外へ押し出すことができない。
ここでは同じ距離関数を格子で評価し、表面を網目として取り出して OBJ にする。

- 立ち姿で固める（地表の生きものは動かさない）
- 前はUnityの+Z、横は±X、上は+Y。足の裏がy=0
- 材質は2つ。creature（胴・毛皮）と tusk（牙・角）
"""
import sys
import numpy as np
from skimage import measure
import fast_simplification


def fract(x):
    return x - np.floor(x)


def hash3(x, y, z):
    x = fract(x * 0.3183099 + 0.1) * 17.0
    y = fract(y * 0.3183099 + 0.2) * 17.0
    z = fract(z * 0.3183099 + 0.3) * 17.0
    return fract(x * y * z * (x + y + z))


def noise(x, y, z):
    ix, iy, iz = np.floor(x), np.floor(y), np.floor(z)
    fx, fy, fz = x - ix, y - iy, z - iz
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    fz = fz * fz * (3 - 2 * fz)

    def h(a, b, c):
        return hash3(ix + a, iy + b, iz + c)

    def mix(a, b, t):
        return a + (b - a) * t

    return mix(mix(mix(h(0, 0, 0), h(1, 0, 0), fx), mix(h(0, 1, 0), h(1, 1, 0), fx), fy),
               mix(mix(h(0, 0, 1), h(1, 0, 1), fx), mix(h(0, 1, 1), h(1, 1, 1), fx), fy), fz)


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b + (a - b) * h - k * h * (1 - h)


def cap(P, a, b, ra, rb):
    a = np.array(a, float)
    b = np.array(b, float)
    pa = P - a
    ba = b - a
    h = np.clip((pa @ ba) / (ba @ ba), 0, 1)
    return np.linalg.norm(pa - h[:, None] * ba, axis=1) - (ra + (rb - ra) * h)


def ell(P, c, r):
    r = np.array(r, float)
    q = P - np.array(c, float)
    k0 = np.linalg.norm(q / r, axis=1)
    k1 = np.linalg.norm(q / (r * r), axis=1)
    return k0 * (k0 - 1) / np.maximum(k1, 1e-9)


def leg(P, hip, foot, r0, r1, r2):
    hip = np.array(hip, float)
    foot = np.array(foot, float)
    knee = (hip + foot) * 0.5 + np.array([0.05, 0, 0])
    return np.minimum(cap(P, hip, knee, r0, r1), cap(P, knee, foot, r1, r2))


def fur(P, d, a0, a1, b0, b1, base, extra):
    y = P[:, 1]
    fx, fy, fz = P[:, 0] * 10, y * 2.4, P[:, 2] * 10
    n = 0.6 * noise(fx, fy, fz) + 0.4 * noise(fx * 2.3 + 7.1, fy * 2.3 + 7.1, fz * 2.3 + 7.1)

    def ss(e0, e1, v):
        t = np.clip((v - e0) / (e1 - e0), 0, 1)
        return t * t * (3 - 2 * t)

    skirt = (1 - ss(a0, a1, y)) * ss(b0, b1, y)
    near = d < 0.35
    return np.where(near, d - (base + extra * skirt) * n, d)


def mirror(P):
    return np.stack([P[:, 0], P[:, 1], np.abs(P[:, 2])], axis=1)


# 立ち姿：対角の2本を少し前後にずらす。持ち上げはしない
STANCE = 0.12


def mammoth(P):
    lift = 0.2
    w = P
    p = P - np.array([0, lift, 0])
    q = mirror(p)
    d = ell(p, (-0.2, 1.95, 0), (1.55, 0.95, 0.95))
    d = smin(d, ell(p, (0.75, 2.45, 0), (0.85, 0.70, 0.80)), 0.5)
    d = smin(d, ell(p, (-1.25, 1.85, 0), (0.75, 0.75, 0.82)), 0.4)
    h = ell(p, (1.75, 2.55, 0), (0.62, 0.72, 0.60))
    h = smin(h, ell(p, (1.62, 3.12, 0), (0.42, 0.40, 0.36)), 0.3)
    h = smin(h, ell(q, (1.45, 2.62, 0.56), (0.20, 0.26, 0.07)), 0.1)
    t = [(2.22, 2.45, 0), (2.52, 1.85, 0), (2.62, 1.20, 0), (2.50, 0.62, 0), (2.68, 0.36, 0)]
    tr = cap(p, t[0], t[1], 0.28, 0.21)
    tr = smin(tr, cap(p, t[1], t[2], 0.21, 0.16), 0.05)
    tr = smin(tr, cap(p, t[2], t[3], 0.16, 0.12), 0.05)
    tr = smin(tr, cap(p, t[3], t[4], 0.12, 0.10), 0.04)
    h = smin(h, tr, 0.18)
    d = smin(d, h, 0.45)
    d = smin(d, cap(p, (-1.95, 2.15, 0), (-2.2, 1.55, 0), 0.09, 0.05), 0.12)
    s = STANCE
    lg = leg(w, (0.95, 1.65 + lift, 0.50), (0.95 + s, 0.30, 0.52), 0.42, 0.34, 0.30)
    lg = np.minimum(lg, leg(w, (0.95, 1.65 + lift, -0.50), (0.95 - s, 0.30, -0.52), 0.42, 0.34, 0.30))
    lg = np.minimum(lg, leg(w, (-1.25, 1.55 + lift, 0.50), (-1.30 - s, 0.30, 0.52), 0.42, 0.34, 0.30))
    lg = np.minimum(lg, leg(w, (-1.25, 1.55 + lift, -0.50), (-1.30 + s, 0.30, -0.52), 0.42, 0.34, 0.30))
    d = smin(d, lg, 0.3)
    d = fur(w, d, 1.2, 2.2, 0.45, 1.0, 0.03, 0.14)
    k = [(2.05, 2.20, 0.28), (2.30, 1.72, 0.40), (2.65, 1.38, 0.56), (3.05, 1.30, 0.70),
         (3.42, 1.48, 0.68), (3.62, 1.85, 0.52), (3.58, 2.22, 0.28)]
    r = [0.13, 0.12, 0.11, 0.095, 0.08, 0.06, 0.03]
    tk = cap(q, k[0], k[1], r[0], r[1])
    for i in range(1, 6):
        tk = smin(tk, cap(q, k[i], k[i + 1], r[i], r[i + 1]), 0.04 if i < 4 else 0.03)
    return d, tk


def rhino(P):
    w = P
    p = P
    q = mirror(p)
    d = ell(p, (-0.2, 1.25, 0), (1.45, 0.62, 0.62))
    d = smin(d, ell(p, (0.55, 1.52, 0), (0.62, 0.45, 0.55)), 0.4)
    d = smin(d, ell(p, (-1.2, 1.22, 0), (0.55, 0.55, 0.58)), 0.35)
    h = cap(p, (1.05, 1.30, 0), (1.95, 0.82, 0), 0.36, 0.22)
    h = smin(h, ell(p, (1.86, 0.80, 0), (0.30, 0.26, 0.25)), 0.14)
    h = smin(h, ell(p, (1.70, 0.66, 0), (0.26, 0.12, 0.18)), 0.1)
    h = smin(h, ell(q, (1.28, 1.48, 0.20), (0.08, 0.14, 0.05)), 0.06)
    d = smin(d, h, 0.3)
    d = smin(d, cap(p, (-1.68, 1.38, 0), (-1.88, 0.98, 0), 0.06, 0.03), 0.08)
    s = STANCE * 0.75
    lg = leg(w, (0.70, 1.10, 0.38), (0.72 + s, 0.18, 0.40), 0.25, 0.19, 0.17)
    lg = np.minimum(lg, leg(w, (0.70, 1.10, -0.38), (0.72 - s, 0.18, -0.40), 0.25, 0.19, 0.17))
    lg = np.minimum(lg, leg(w, (-1.15, 1.05, 0.38), (-1.20 - s, 0.18, 0.40), 0.25, 0.19, 0.17))
    lg = np.minimum(lg, leg(w, (-1.15, 1.05, -0.38), (-1.20 + s, 0.18, -0.40), 0.25, 0.19, 0.17))
    d = smin(d, lg, 0.22)
    d = fur(w, d, 0.75, 1.45, 0.30, 0.65, 0.025, 0.12)
    hp = np.stack([p[:, 0], p[:, 1], p[:, 2] * 2.2], axis=1)
    hn = cap(hp, (2.00, 0.92, 0), (2.42, 1.10, 0), 0.15, 0.11)
    hn = smin(hn, cap(hp, (2.42, 1.10, 0), (2.80, 1.38, 0), 0.11, 0.07), 0.05)
    hn = smin(hn, cap(hp, (2.80, 1.38, 0), (3.00, 1.72, 0), 0.07, 0.015), 0.04)
    hn = np.minimum(hn, cap(hp, (1.62, 1.12, 0), (1.78, 1.46, 0), 0.12, 0.02))
    hn = hn / 2.2
    return d, hn


SPECIES = {
    # 名前: (関数, 格子の範囲 [x0,x1], [y0,y1], [z0,z1], 格子の間隔, 面の目標数)
    "sdf_mammoth": (mammoth, (-2.6, 3.9), (-0.05, 3.75), (-1.35, 1.35), 0.028, 7000),
    "sdf_rhino": (rhino, (-2.1, 3.2), (-0.05, 2.15), (-0.95, 0.95), 0.020, 6000),
}


def bake(name, out_dir):
    fn, xr, yr, zr, step, target = SPECIES[name]
    xs = np.arange(xr[0], xr[1] + step, step)
    ys = np.arange(yr[0], yr[1] + step, step)
    zs = np.arange(zr[0], zr[1] + step, step)
    X, Y, Z = np.meshgrid(xs, ys, zs, indexing="ij")
    P = np.stack([X.ravel(), Y.ravel(), Z.ravel()], axis=1)
    body = np.empty(len(P))
    horn = np.empty(len(P))
    for i in range(0, len(P), 400000):
        b, h = fn(P[i:i + 400000])
        body[i:i + 400000] = b
        horn[i:i + 400000] = h
    field = np.minimum(body, horn).reshape(X.shape)
    # 地面の下は切る（足の裏をy=0で平らにする）
    field = np.maximum(field, (-Y).reshape(X.shape))

    verts, faces, _, _ = measure.marching_cubes(field, 0.0, spacing=(step, step, step))
    verts += np.array([xr[0], yr[0], zr[0]])
    ratio = max(0.0, 1.0 - target / len(faces))
    v2, f2 = fast_simplification.simplify(verts.astype(np.float32), faces.astype(np.int32), ratio)

    # 面ごとの材質：重心で胴と牙のどちらが近いか
    cent = v2[f2].mean(axis=1)
    b, h = fn(cent.astype(float))
    is_horn = h < b

    # 前後の中心を0へ寄せ、足の裏をy=0へ
    v2 = v2.astype(float)
    v2[:, 0] -= (v2[:, 0].min() + v2[:, 0].max()) * 0.5
    v2[:, 1] -= v2[:, 1].min()

    # 頂点の法線（面の法線を面積で重み付けして足す）
    tri = v2[f2]
    fn_ = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    vn = np.zeros_like(v2)
    for k in range(3):
        np.add.at(vn, f2[:, k], fn_)
    vn /= np.maximum(np.linalg.norm(vn, axis=1, keepdims=True), 1e-9)

    # 向き：試作の（前, 上, 横）→ Unity の（横, 上, 前）。
    # Unity は OBJ の X を反転して読むので、書くときに X を反転しておく。
    def to_obj(a):
        return np.stack([-a[:, 2], a[:, 1], a[:, 0]], axis=1)

    ov = to_obj(v2)
    on = to_obj(vn)

    path = f"{out_dir}/{name}.obj"
    with open(path, "w") as f:
        f.write(f"# {name}: docs/prototypes/mammoth.html の距離関数から焼いた網目\n")
        f.write("# 立ち姿・毛皮込み。前は+Z（Unity）、足の裏はy=0。単位はメートル相当\n")
        f.write(f"mtllib {name}.mtl\no {name}\n")
        for v in ov:
            f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
        for n in on:
            f.write(f"vn {n[0]:.3f} {n[1]:.3f} {n[2]:.3f}\n")
        for mat, mask in (("creature", ~is_horn), ("tusk", is_horn)):
            if not mask.any():
                continue
            f.write(f"usemtl {mat}\n")
            for a, b_, c in f2[mask] + 1:
                f.write(f"f {a}//{a} {b_}//{b_} {c}//{c}\n")
    with open(f"{out_dir}/{name}.mtl", "w") as f:
        f.write("newmtl creature\nKd 0.35 0.22 0.12\n\nnewmtl tusk\nKd 0.80 0.74 0.60\n")
    size = ov.max(axis=0) - ov.min(axis=0)
    print(f"{name}: 格子 {X.shape} → 面 {len(faces)} → {len(f2)}（牙・角 {int(is_horn.sum())}）"
          f" 大きさ 横{size[0]:.2f} 高{size[1]:.2f} 長{size[2]:.2f}")
    return path


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    for n in SPECIES:
        bake(n, out)
