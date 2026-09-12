/*
 * 地球ができるまでの計算。描画も画面も持たない。
 *
 * unity/CivilizationToSpace/Assets/Scripts/Sim/AccretionSimulation.cs の移植である。
 * 手順・定数・段階の条件をそろえてある。ただしC#はfloat、JavaScriptはdoubleで
 * 計算するため、同じ種でも途中の数値は一致しない。結末（地球と月になる）は変わらない。
 *
 * 何を計算し、何を計算していないかは docs/reports/ANDROID_FORMATION_SIM.md にある。
 * 要点だけ再掲する。
 *   計算している：万有引力の総当たり、運動量を保つ合体、段階の遷移条件
 *   計算していない：衝突で破片が飛び散る過程（決めた量を決めた範囲へ置く模型）
 *   強めている：破片どうしの引力（既定60倍。時間を縮めるかわり）
 */
(function (root) {
  'use strict';

  var PHASE = {
    ACCRETION: 0,
    PROTO_EARTH: 1,
    IMPACTOR: 2,
    IMPACT: 3,
    MOON_FORMING: 4,
    SETTLED: 5,
    COLONY: 6
  };

  var PHASE_LABEL = [
    '微惑星がぶつかりながら集まっている',
    'ひとつの塊になった（原始地球）',
    '別の天体が近づいている',
    'ぶつかった。破片が飛び散った',
    '破片がまわりながら集まっている',
    '地球と月になった（軌道が落ち着いていく）',
    'ラグランジュ点（L4・L5）にコロニーを置いた'
  ];

  var KIND = { PLANETESIMAL: 0, EARTH: 1, IMPACTOR: 2, DEBRIS: 3, MOON: 4, COLONY: 5 };

  var DEFAULTS = {
    seed: 20260912,
    planetesimalCount: 380,
    cloudRadius: 26,
    cloudThickness: 4.5,
    spinFraction: 0.52,
    gravity: 150,
    softening: 0.45,
    gasDrag: 0.16,
    totalMass: 1,
    radiusUnit: 2.2,
    mergeSlack: 1.05,
    protoEarthMassFraction: 0.78,
    impactorDelay: 2,
    impactorMassFraction: 0.13,
    impactorStartDistance: 34,
    impactorSpeedFactor: 1,
    impactParameter: 0.8,
    ejectaMassFraction: 0.022,
    ejectaCount: 40,
    ejectaInnerRadius: 4,
    ejectaOuterRadius: 6,
    ejectaArcDegrees: 120,
    ejectaSpeedFactor: 1,
    diskViscosity: 0.8,
    debrisAttraction: 60,
    stepSize: 0.015,
    maxStepsPerFrame: 3,
    settleDelay: 3,

    /*
     * 地球と月が決まったあと、L4とL5へコロニーを置く。
     *
     * 置くのは「絵」ではない。ほかの天体と同じ式で動かす。留まるかどうかは
     * 計算の結果であって、こちらが決めていない。留まるのは、地球に対する月の
     * 質量比が 0.0385 より小さいときだけである（本実装では約0.021）。
     * 条件を変えて月を重くすると、留まらなくなることを確かめられる。
     */
    colonyEnabled: true,

    /*
     * 月ができた直後の軌道を円くする強さ。
     *
     * 入れないと、破片が集まってできた塊は多くの場合そのまま飛び去る
     * （実測：離心率 e が 1 を超え、地球に束縛されていない種が4つ中3つ）。
     * 実際の月も、できた直後は潮汐で軌道が変わり続けた。その働きの向きだけを
     * 写したもので、強さは見ていられる時間で落ち着くように決めた値である。
     */
    tidalCircularise: 0.35,

    /*
     * 月を運ぶ先。地球半径の何倍か。
     *
     * できたばかりの月は地球のすぐそばにあり、そのままではラグランジュ点が
     * 地球の表面すれすれに来てしまう。実際の月も、できた直後は3〜5地球半径に
     * あり、潮汐で現在の約60地球半径まで遠ざかった。その過程を縮めている。
     * **ここは計算ではなく運搬である。** 遠ざかる向きと、円くなる向きだけが
     * 実際の潮汐と同じで、速さは見ていられる時間に合わせた値である。
     */
    moonTargetRadii: 12,

    /*
     * この離心率より小さくなったら「落ち着いた」とみなし、コロニーを置く。
     *
     * 楕円が残ったまま置くと、L4・L5 が固定点にならず、コロニーが離れていく。
     * 実測（月の質量比 0.022 の系、900単位時間）：
     *   置いた時 e=0.060 → 失われた
     *   置いた時 e=0.020 → 46〜75度のあいだで振れた（残るが大きく振れる）
     *   置いた時 e=0.005 → 58〜62度に収まった
     */
    colonyMaxEccentricity: 0.005,

    /* 落ち着くのを待つ上限。これを過ぎたら、落ち着いていなくても先へ進む。 */
    colonySettleTimeout: 400,

    /* コロニーの質量。運動を乱さないよう、事実上の試験粒子にする。 */
    colonyMassFraction: 1e-7,

    /* 地球と月が落ち着いてからコロニーを置くまでの待ち時間。 */
    colonyDelay: 1.5
  };

  /* 種を決めれば同じ結果になる乱数。xorshift32。 */
  function Rng(seed) {
    this.state = (seed >>> 0) || 2463534242;
  }

  Rng.prototype.next = function () {
    var x = this.state;
    x ^= (x << 13) >>> 0;
    x = x >>> 0;
    x ^= x >>> 17;
    x ^= (x << 5) >>> 0;
    this.state = x >>> 0;
    return this.state;
  };

  Rng.prototype.value = function () {
    return (this.next() >>> 8) / 16777216;
  };

  Rng.prototype.range = function (min, max) {
    return min + (max - min) * this.value();
  };

  /* 球面上の一様な向き。長さ1の配列 [x, y, z] を返す。 */
  Rng.prototype.onSphere = function () {
    var z = this.range(-1, 1);
    var angle = this.range(0, Math.PI * 2);
    var r = Math.sqrt(Math.max(0, 1 - z * z));
    return [r * Math.cos(angle), r * Math.sin(angle), z];
  };

  function Simulation(options) {
    this.settings = {};
    for (var key in DEFAULTS) {
      if (Object.prototype.hasOwnProperty.call(DEFAULTS, key)) {
        this.settings[key] = DEFAULTS[key];
      }
    }
    if (options) {
      for (var given in options) {
        if (Object.prototype.hasOwnProperty.call(options, given)) {
          this.settings[given] = options[given];
        }
      }
    }

    this.restart(this.settings.seed);
  }

  Simulation.PHASE = PHASE;
  Simulation.KIND = KIND;
  Simulation.DEFAULTS = DEFAULTS;
  Simulation.phaseLabel = function (phase) { return PHASE_LABEL[phase] || ''; };

  Simulation.prototype.radiusFor = function (mass) {
    return this.settings.radiusUnit * Math.pow(Math.max(mass, 1e-9), 1 / 3);
  };

  Simulation.prototype.restart = function (seed) {
    var s = this.settings;
    s.seed = seed;
    this.rng = new Rng(seed);
    this.phase = PHASE.ACCRETION;
    this.time = 0;
    this.protoEarthAt = 0;
    this.impactAt = 0;
    this.mergeEvents = 0;
    this.settledAt = 0;
    this.count = 0;

    var capacity = Math.max(16, s.planetesimalCount + s.ejectaCount + 8);
    this.allocate(capacity);
    this.buildCloud();
    this.measure();
  };

  Simulation.prototype.allocate = function (capacity) {
    this.capacity = capacity;
    this.px = new Float32Array(capacity);
    this.py = new Float32Array(capacity);
    this.pz = new Float32Array(capacity);
    this.vx = new Float32Array(capacity);
    this.vy = new Float32Array(capacity);
    this.vz = new Float32Array(capacity);
    this.ax = new Float32Array(capacity);
    this.ay = new Float32Array(capacity);
    this.az = new Float32Array(capacity);
    this.mass = new Float32Array(capacity);
    this.radius = new Float32Array(capacity);
    this.kind = new Uint8Array(capacity);
    this.alive = new Uint8Array(capacity);
    this.mergedInto = new Int32Array(capacity);
    this.pendingA = new Int32Array(capacity * 4);
    this.pendingB = new Int32Array(capacity * 4);
    this.pendingCount = 0;
  };

  Simulation.prototype.grow = function () {
    var size = Math.max(16, this.capacity * 2);
    var names = ['px', 'py', 'pz', 'vx', 'vy', 'vz', 'ax', 'ay', 'az', 'mass', 'radius'];
    for (var i = 0; i < names.length; i++) {
      var grown = new Float32Array(size);
      grown.set(this[names[i]]);
      this[names[i]] = grown;
    }

    var kind = new Uint8Array(size); kind.set(this.kind); this.kind = kind;
    var alive = new Uint8Array(size); alive.set(this.alive); this.alive = alive;
    var merged = new Int32Array(size); merged.set(this.mergedInto); this.mergedInto = merged;
    this.capacity = size;
  };

  Simulation.prototype.add = function (x, y, z, vx, vy, vz, mass, kind) {
    if (this.count >= this.capacity) { this.grow(); }
    var i = this.count;
    this.px[i] = x; this.py[i] = y; this.pz[i] = z;
    this.vx[i] = vx; this.vy[i] = vy; this.vz[i] = vz;
    this.mass[i] = mass;
    this.radius[i] = this.radiusFor(mass);
    this.kind[i] = kind;
    this.alive[i] = 1;
    this.mergedInto[i] = -1;
    this.count++;
  };

  Simulation.prototype.buildCloud = function () {
    var s = this.settings;
    var n = Math.max(1, s.planetesimalCount);
    var each = s.totalMass / n;

    for (var i = 0; i < n; i++) {
      // 面積あたりの個数をそろえるため、半径は平方根で引く。
      var r = s.cloudRadius * Math.sqrt(this.rng.range(0.05, 1));
      var angle = this.rng.range(0, Math.PI * 2);
      var height = this.rng.range(-0.5, 0.5) * s.cloudThickness;

      // 内側にある質量だけが効くとみなして、円軌道の速さを出す。
      var fraction = Math.min(1, (r / s.cloudRadius) * (r / s.cloudRadius));
      var enclosed = s.totalMass * fraction;
      var circular = Math.sqrt(s.gravity * Math.max(enclosed, each) / Math.max(r, 0.01));

      var scatter = this.rng.onSphere();
      var speed = circular * s.spinFraction;

      this.add(
        Math.cos(angle) * r, height, Math.sin(angle) * r,
        -Math.sin(angle) * speed + scatter[0] * circular * 0.06,
        scatter[1] * circular * 0.06,
        Math.cos(angle) * speed + scatter[2] * circular * 0.06,
        each, KIND.PLANETESIMAL);
    }
  };

  Simulation.prototype.step = function (dt) {
    if (this.count === 0 || dt <= 0) { return; }
    this.accumulate();
    this.integrate(dt);
    this.resolveMerges();
    this.compact();
    this.measure();
    this.time += dt;
    this.updatePhase();
  };

  Simulation.prototype.accumulate = function () {
    var s = this.settings;
    var n = this.count;
    var px = this.px, py = this.py, pz = this.pz;
    var ax = this.ax, ay = this.ay, az = this.az;
    var mass = this.mass, radius = this.radius, kind = this.kind;

    this.pendingCount = 0;

    for (var z = 0; z < n; z++) { ax[z] = 0; ay[z] = 0; az[z] = 0; }

    var softening2 = s.softening * s.softening;
    var g = s.gravity;
    var slack = s.mergeSlack;
    var boost = Math.max(1, s.debrisAttraction);

    for (var i = 0; i < n; i++) {
      var xi = px[i], yi = py[i], zi = pz[i];
      var mi = mass[i], ri = radius[i];
      var axi = ax[i], ayi = ay[i], azi = az[i];
      var debrisI = kind[i] === KIND.DEBRIS;

      for (var j = i + 1; j < n; j++) {
        var dx = px[j] - xi;
        var dy = py[j] - yi;
        var dz = pz[j] - zi;
        var d2 = dx * dx + dy * dy + dz * dz;

        var touch = (ri + radius[j]) * slack;
        if (d2 <= touch * touch) {
          // 触れている対は力を足さない。めり込んだ瞬間に力が跳ねるのを防ぐ。
          this.addPending(i, j);
          continue;
        }

        var inverse = 1 / Math.sqrt(d2 + softening2);
        var scale = g * inverse * inverse * inverse;

        // 破片どうしのあいだだけ引力を強める。物理としては正しくない。
        // 実際の月が集まるまでの周回数を、時間ではなく力で縮めている。
        if (debrisI && kind[j] === KIND.DEBRIS) { scale *= boost; }

        var toJ = scale * mass[j];
        axi += dx * toJ; ayi += dy * toJ; azi += dz * toJ;

        var toI = scale * mi;
        ax[j] -= dx * toI; ay[j] -= dy * toI; az[j] -= dz * toI;
      }

      ax[i] = axi; ay[i] = ayi; az[i] = azi;
    }
  };

  Simulation.prototype.addPending = function (a, b) {
    if (this.pendingCount >= this.pendingA.length) {
      var size = this.pendingA.length * 2;
      var pa = new Int32Array(size); pa.set(this.pendingA); this.pendingA = pa;
      var pb = new Int32Array(size); pb.set(this.pendingB); this.pendingB = pb;
    }
    this.pendingA[this.pendingCount] = a;
    this.pendingB[this.pendingCount] = b;
    this.pendingCount++;
  };

  Simulation.prototype.integrate = function (dt) {
    var s = this.settings;
    var n = this.count;

    // 集積のあいだだけ効く抵抗。原始惑星系円盤のガスの働きをならしたもの。
    // 入れないと、残った数個が互いを回る安定な組になって合体が終わらない。
    var drag = this.phase === PHASE.ACCRETION ? Math.max(0, s.gasDrag) : 0;

    // 衝突後の円盤の粘り。軌道を円くする向きにだけ効かせる。
    // 平均速度へ引き寄せる形にすると周回そのものが消え、破片が地球へ落ちる。
    var viscosity =
      (this.phase === PHASE.IMPACT || this.phase === PHASE.MOON_FORMING)
        ? Math.max(0, s.diskViscosity) : 0;

    // 月ができた直後だけ、その軌道も円くする。潮汐の働きの向きだけを写したもの。
    var tidal = this.phase === PHASE.SETTLED ? Math.max(0, s.tidalCircularise) : 0;

    var earth = this.earthIndex;
    var hasEarth = earth >= 0 && earth < n;
    var circularise = viscosity > 0 && hasEarth;
    var settling = tidal > 0 && hasEarth;

    for (var i = 0; i < n; i++) {
      if (drag > 0) {
        this.ax[i] -= (this.vx[i] - this.centerVx) * drag;
        this.ay[i] -= (this.vy[i] - this.centerVy) * drag;
        this.az[i] -= (this.vz[i] - this.centerVz) * drag;
      }

      if (circularise && this.kind[i] === KIND.DEBRIS) {
        this.applyCircularise(i, earth, viscosity);
      }

      if (settling && this.kind[i] === KIND.MOON) {
        this.applyTidalRecession(i, earth, tidal);
      }

      this.vx[i] += this.ax[i] * dt;
      this.vy[i] += this.ay[i] * dt;
      this.vz[i] += this.az[i] * dt;
      this.px[i] += this.vx[i] * dt;
      this.py[i] += this.vy[i] * dt;
      this.pz[i] += this.vz[i] * dt;
    }
  };

  /* いまの半径・いまの回る向きのままの円軌道へ近づける。面も向きも変えない。 */
  Simulation.prototype.applyCircularise = function (i, earth, viscosity) {
    var ox = this.px[i] - this.px[earth];
    var oy = this.py[i] - this.py[earth];
    var oz = this.pz[i] - this.pz[earth];
    var distance = Math.sqrt(ox * ox + oy * oy + oz * oz);
    if (distance < 1e-4) { return; }

    var rx = this.vx[i] - this.vx[earth];
    var ry = this.vy[i] - this.vy[earth];
    var rz = this.vz[i] - this.vz[earth];

    // 角運動量 offset × relative
    var hx = oy * rz - oz * ry;
    var hy = oz * rx - ox * rz;
    var hz = ox * ry - oy * rx;
    var h = Math.sqrt(hx * hx + hy * hy + hz * hz);
    if (h < 1e-4) { return; }

    hx /= h; hy /= h; hz /= h;
    var ux = ox / distance, uy = oy / distance, uz = oz / distance;

    // 接線 = 角運動量の向き × 半径の向き
    var tx = hy * uz - hz * uy;
    var ty = hz * ux - hx * uz;
    var tz = hx * uy - hy * ux;

    var circular = Math.sqrt(this.settings.gravity * this.mass[earth] / distance);

    this.ax[i] += (this.vx[earth] + tx * circular - this.vx[i]) * viscosity;
    this.ay[i] += (this.vy[earth] + ty * circular - this.vy[i]) * viscosity;
    this.az[i] += (this.vz[earth] + tz * circular - this.vz[i]) * viscosity;
  };

  /*
   * 半径方向の速度だけを減らす。
   *
   * 向きが中心を向いているので、角運動量に対する回転の力（トルク）が0になる。
   * つまり角運動量を保ったまま、楕円を円に近づける。落ち着く先の半径は
   * h^2 / μ（h は角運動量）で、近点より内側には決して入らない。
   *
   * 「いまの半径の円」を目指す形（applyCircularise）にすると、近点にいるあいだは
   * その小さい半径の円を目指してしまい、月が内側へ引きずられて地球に落ちる
   * （実測：12種のうち4種で月が地球に吸い込まれた）。
   */
  /*
   * 潮汐で月が遠ざかり、軌道が円くなる過程を縮めたもの。**計算ではなく運搬である。**
   *
   * 半径を目標へ寄せ、接線方向の速さをその半径の円軌道に合わせる。
   * 入れないと、破片が集まってできた塊は地球に落ちるか飛び去るかのどちらかになり、
   * 地球と月の組が残らない（実測：種12通りのうち、そのまま残ったのは1つだけ）。
   * 実際の月も、できた直後は3〜5地球半径にあり、潮汐で約60地球半径まで遠ざかった。
   * 向きだけが実際と同じで、速さは見ていられる時間に合わせた値である。
   */
  Simulation.prototype.applyTidalRecession = function (i, earth, strength) {
    var ox = this.px[i] - this.px[earth];
    var oy = this.py[i] - this.py[earth];
    var oz = this.pz[i] - this.pz[earth];
    var distance = Math.sqrt(ox * ox + oy * oy + oz * oz);
    if (distance < 1e-4) { return; }

    var ux = ox / distance, uy = oy / distance, uz = oz / distance;
    var rx = this.vx[i] - this.vx[earth];
    var ry = this.vy[i] - this.vy[earth];
    var rz = this.vz[i] - this.vz[earth];

    // 半径方向と接線方向に分ける。
    var radial = rx * ux + ry * uy + rz * uz;
    var tx = rx - ux * radial;
    var ty = ry - uy * radial;
    var tz = rz - uz * radial;
    var tangential = Math.sqrt(tx * tx + ty * ty + tz * tz);

    var target = this.settings.moonTargetRadii * this.radius[earth];
    var mu = this.settings.gravity * (this.mass[earth] + this.mass[i]);
    var circular = Math.sqrt(mu / distance);

    // 半径を目標へ。行きすぎないよう、半径方向の速さも抑える。
    var toward = ((target - distance) * 0.30 - radial * 2.0) * strength;
    this.ax[i] += ux * toward;
    this.ay[i] += uy * toward;
    this.az[i] += uz * toward;

    if (tangential < 1e-6) { return; }

    // 接線の速さをその半径の円軌道に合わせる。向きは変えない。
    var spin = (circular - tangential) * strength;
    this.ax[i] += tx / tangential * spin;
    this.ay[i] += ty / tangential * spin;
    this.az[i] += tz / tangential * spin;
  };

  Simulation.prototype.applyRadialDamping = function (i, earth, strength) {
    var ox = this.px[i] - this.px[earth];
    var oy = this.py[i] - this.py[earth];
    var oz = this.pz[i] - this.pz[earth];
    var distance = Math.sqrt(ox * ox + oy * oy + oz * oz);
    if (distance < 1e-4) { return; }

    var ux = ox / distance, uy = oy / distance, uz = oz / distance;
    var rx = this.vx[i] - this.vx[earth];
    var ry = this.vy[i] - this.vy[earth];
    var rz = this.vz[i] - this.vz[earth];

    var radial = rx * ux + ry * uy + rz * uz;

    this.ax[i] -= ux * radial * strength;
    this.ay[i] -= uy * radial * strength;
    this.az[i] -= uz * radial * strength;
  };

  Simulation.prototype.resolve = function (index) {
    var guard = 0;
    while (this.mergedInto[index] >= 0 && guard++ < 64) {
      index = this.mergedInto[index];
    }
    return index;
  };

  Simulation.prototype.resolveMerges = function () {
    if (this.pendingCount === 0) { return; }

    for (var i = 0; i < this.count; i++) { this.mergedInto[i] = -1; }

    for (var p = 0; p < this.pendingCount; p++) {
      var a = this.resolve(this.pendingA[p]);
      var b = this.resolve(this.pendingB[p]);

      if (a === b || !this.alive[a] || !this.alive[b]) { continue; }

      if (this.mass[b] > this.mass[a]) { var swap = a; a = b; b = swap; }

      var hasImpactor = this.kind[a] === KIND.IMPACTOR || this.kind[b] === KIND.IMPACTOR;
      var hasEarth = this.kind[a] === KIND.EARTH || this.kind[b] === KIND.EARTH;

      if (this.phase === PHASE.IMPACTOR && hasImpactor && hasEarth) {
        this.giantImpact(a, b);
        continue;
      }

      this.mergeInto(a, b);
    }

    this.pendingCount = 0;
  };

  Simulation.prototype.mergeInto = function (a, b) {
    var total = this.mass[a] + this.mass[b];
    if (total <= 0) { return; }

    var wa = this.mass[a] / total, wb = this.mass[b] / total;
    this.px[a] = this.px[a] * wa + this.px[b] * wb;
    this.py[a] = this.py[a] * wa + this.py[b] * wb;
    this.pz[a] = this.pz[a] * wa + this.pz[b] * wb;
    this.vx[a] = this.vx[a] * wa + this.vx[b] * wb;
    this.vy[a] = this.vy[a] * wa + this.vy[b] * wb;
    this.vz[a] = this.vz[a] * wa + this.vz[b] * wb;
    this.mass[a] = total;
    this.radius[a] = this.radiusFor(total);

    if (this.kind[b] === KIND.EARTH) { this.kind[a] = KIND.EARTH; }

    this.alive[b] = 0;
    this.mergedInto[b] = a;
    this.mergeEvents++;
  };

  /*
   * 衝突の扱い。ここだけは計算ではなく模型である。
   * 2つを合わせたうえで、決めた割合の質量を破片として周回軌道へ置く。
   * 置く向きは衝突の角運動量から決めるので、当て方を変えると円盤の向きが変わる。
   */
  Simulation.prototype.giantImpact = function (a, b) {
    var earth = this.kind[a] === KIND.EARTH ? a : b;
    var impactor = earth === a ? b : a;

    var me = this.mass[earth], mi = this.mass[impactor];
    var total = me + mi;
    if (total <= 0) { return; }

    var we = me / total, wi = mi / total;
    var cx = this.px[earth] * we + this.px[impactor] * wi;
    var cy = this.py[earth] * we + this.py[impactor] * wi;
    var cz = this.pz[earth] * we + this.pz[impactor] * wi;
    var cvx = this.vx[earth] * we + this.vx[impactor] * wi;
    var cvy = this.vy[earth] * we + this.vy[impactor] * wi;
    var cvz = this.vz[earth] * we + this.vz[impactor] * wi;

    var ox = this.px[impactor] - this.px[earth];
    var oy = this.py[impactor] - this.py[earth];
    var oz = this.pz[impactor] - this.pz[earth];
    var rx = this.vx[impactor] - this.vx[earth];
    var ry = this.vy[impactor] - this.vy[earth];
    var rz = this.vz[impactor] - this.vz[earth];

    var hx = oy * rz - oz * ry;
    var hy = oz * rx - ox * rz;
    var hz = ox * ry - oy * rx;
    var h = Math.sqrt(hx * hx + hy * hy + hz * hz);
    if (h < 1e-8) { hx = 0; hy = 1; hz = 0; h = 1; }
    hx /= h; hy /= h; hz /= h;

    var ejectaMass = total * Math.min(1, Math.max(0, this.settings.ejectaMassFraction));
    var remaining = total - ejectaMass;

    this.px[earth] = cx; this.py[earth] = cy; this.pz[earth] = cz;
    this.vx[earth] = cvx; this.vy[earth] = cvy; this.vz[earth] = cvz;
    this.mass[earth] = remaining;
    this.radius[earth] = this.radiusFor(remaining);
    this.kind[earth] = KIND.EARTH;

    this.alive[impactor] = 0;
    this.mergedInto[impactor] = earth;
    this.mergeEvents++;

    this.spawnEjecta(earth, hx, hy, hz, ox, oy, oz, ejectaMass);

    this.phase = PHASE.IMPACT;
    this.impactAt = this.time;
  };

  Simulation.prototype.spawnEjecta = function (earth, hx, hy, hz, ox, oy, oz, ejectaMass) {
    var s = this.settings;
    var pieces = Math.max(1, s.ejectaCount);
    var each = ejectaMass / pieces;
    var earthRadius = this.radius[earth];

    // 円盤の面内で使う2つの向き。当たった側を弧の中心にする。
    var dot = ox * hx + oy * hy + oz * hz;
    var fx = ox - hx * dot, fy = oy - hy * dot, fz = oz - hz * dot;
    var f = Math.sqrt(fx * fx + fy * fy + fz * fz);
    if (f < 1e-8) {
      fx = 1 - hx * hx; fy = -hx * hy; fz = -hx * hz;
      f = Math.sqrt(fx * fx + fy * fy + fz * fz) || 1;
    }
    fx /= f; fy /= f; fz /= f;

    // side = h × forward
    var sx = hy * fz - hz * fy;
    var sy = hz * fx - hx * fz;
    var sz = hx * fy - hy * fx;

    var arc = s.ejectaArcDegrees * Math.PI / 180;
    var inner = Math.min(s.ejectaInnerRadius, s.ejectaOuterRadius);
    var outer = Math.max(s.ejectaInnerRadius, s.ejectaOuterRadius);

    for (var k = 0; k < pieces; k++) {
      var along = pieces === 1 ? 0.5 : k / (pieces - 1);
      var angle = (along - 0.5) * arc + this.rng.range(-0.04, 0.04);

      var t = this.rng.value();
      var distance = (inner + (outer - inner) * t) * earthRadius;

      var c = Math.cos(angle), n = Math.sin(angle);
      var dx = fx * c + sx * n;
      var dy = fy * c + sy * n;
      var dz = fz * c + sz * n;

      // 接線 = h × 方向
      var tx = hy * dz - hz * dy;
      var ty = hz * dx - hx * dz;
      var tz = hx * dy - hy * dx;

      var circular = Math.sqrt(s.gravity * this.mass[earth] / Math.max(distance, 0.01));
      var speed = circular * s.ejectaSpeedFactor * this.rng.range(0.97, 1.03);

      // 完全な平面に置くと同じ軌道を回って重ならない。少し浮かせる。
      var lift = this.rng.range(-0.06, 0.06) * distance;

      this.add(
        this.px[earth] + dx * distance + hx * lift,
        this.py[earth] + dy * distance + hy * lift,
        this.pz[earth] + dz * distance + hz * lift,
        this.vx[earth] + tx * speed,
        this.vy[earth] + ty * speed,
        this.vz[earth] + tz * speed,
        each, KIND.DEBRIS);
    }
  };

  Simulation.prototype.spawnImpactor = function () {
    if (this.largestIndex < 0) { return; }

    var s = this.settings;
    var earth = this.largestIndex;
    var mass = this.mass[earth] * Math.max(0.01, s.impactorMassFraction);
    var radius = this.radiusFor(mass);

    var d = this.rng.onSphere();
    var dx = d[0], dy = d[1], dz = d[2];

    // 真正面ではなく少しずらして狙う。かすめて当たると回転が残る。
    var refX = 0, refY = 1, refZ = 0;
    if (Math.abs(dy) > 0.9) { refX = 0; refY = 0; refZ = 1; }

    var ox = dy * refZ - dz * refY;
    var oy = dz * refX - dx * refZ;
    var oz = dx * refY - dy * refX;
    var o = Math.sqrt(ox * ox + oy * oy + oz * oz) || 1;
    var offset = s.impactParameter * (this.radius[earth] + radius);
    ox = ox / o * offset; oy = oy / o * offset; oz = oz / o * offset;

    var startX = this.px[earth] + dx * s.impactorStartDistance;
    var startY = this.py[earth] + dy * s.impactorStartDistance;
    var startZ = this.pz[earth] + dz * s.impactorStartDistance;

    var aimX = this.px[earth] + ox - startX;
    var aimY = this.py[earth] + oy - startY;
    var aimZ = this.pz[earth] + oz - startZ;
    var aim = Math.sqrt(aimX * aimX + aimY * aimY + aimZ * aimZ) || 1;

    // その距離での脱出速度を基準にする。1.0ならほぼ放物線で落ちてくる。
    var escape = Math.sqrt(
      2 * s.gravity * (this.mass[earth] + mass) / Math.max(s.impactorStartDistance, 0.01));
    var speed = escape * s.impactorSpeedFactor;

    this.add(
      startX, startY, startZ,
      this.vx[earth] + aimX / aim * speed,
      this.vy[earth] + aimY / aim * speed,
      this.vz[earth] + aimZ / aim * speed,
      mass, KIND.IMPACTOR);
  };

  Simulation.prototype.compact = function () {
    var write = 0;
    for (var i = 0; i < this.count; i++) {
      if (!this.alive[i]) { continue; }
      if (write !== i) {
        this.px[write] = this.px[i]; this.py[write] = this.py[i]; this.pz[write] = this.pz[i];
        this.vx[write] = this.vx[i]; this.vy[write] = this.vy[i]; this.vz[write] = this.vz[i];
        this.mass[write] = this.mass[i]; this.radius[write] = this.radius[i];
        this.kind[write] = this.kind[i]; this.alive[write] = 1;
      }
      write++;
    }
    this.count = write;
  };

  Simulation.prototype.measure = function () {
    this.largestIndex = -1;
    this.largestMass = 0;
    this.totalLiveMass = 0;
    this.earthIndex = -1;

    var wx = 0, wy = 0, wz = 0, wvx = 0, wvy = 0, wvz = 0;

    for (var i = 0; i < this.count; i++) {
      var m = this.mass[i];
      this.totalLiveMass += m;
      wx += this.px[i] * m; wy += this.py[i] * m; wz += this.pz[i] * m;
      wvx += this.vx[i] * m; wvy += this.vy[i] * m; wvz += this.vz[i] * m;

      if (this.kind[i] === KIND.EARTH) { this.earthIndex = i; }
      if (m > this.largestMass) { this.largestMass = m; this.largestIndex = i; }
    }

    var total = this.totalLiveMass > 0 ? this.totalLiveMass : 1;
    this.centerX = wx / total; this.centerY = wy / total; this.centerZ = wz / total;
    this.centerVx = wvx / total; this.centerVy = wvy / total; this.centerVz = wvz / total;
  };

  Simulation.prototype.countOtherThanEarth = function () {
    var others = 0;
    for (var i = 0; i < this.count; i++) {
      if (this.kind[i] !== KIND.EARTH) { others++; }
    }
    return others;
  };

  /* 地球以外で最も重いものを月と呼ぶ。名前を付けるだけで、運動は変えない。 */
  Simulation.prototype.nameTheMoon = function () {
    var best = -1, bestMass = 0;
    for (var i = 0; i < this.count; i++) {
      if (this.kind[i] === KIND.EARTH || this.mass[i] <= bestMass) { continue; }
      best = i; bestMass = this.mass[i];
    }
    if (best >= 0) { this.kind[best] = KIND.MOON; }
  };

  Simulation.prototype.updatePhase = function () {
    var s = this.settings;

    switch (this.phase) {
      case PHASE.ACCRETION:
        if (this.largestIndex >= 0 && this.totalLiveMass > 0 &&
            this.largestMass >= this.totalLiveMass * s.protoEarthMassFraction) {
          this.kind[this.largestIndex] = KIND.EARTH;
          this.phase = PHASE.PROTO_EARTH;
          this.protoEarthAt = this.time;
        }
        break;

      case PHASE.PROTO_EARTH:
        if (this.time - this.protoEarthAt >= s.impactorDelay) {
          this.spawnImpactor();
          this.measure();
          this.phase = PHASE.IMPACTOR;
        }
        break;

      case PHASE.IMPACTOR:
        // 衝突が起きると giantImpact が段階を進める。ここでは待つだけ。
        break;

      case PHASE.IMPACT:
        if (this.time - this.impactAt >= 0.5) { this.phase = PHASE.MOON_FORMING; }
        break;

      case PHASE.MOON_FORMING:
        // まとまらないこともあるため、十分に待ったら先へ進める。
        if (this.time - this.impactAt >= s.settleDelay &&
            (this.countOtherThanEarth() <= 1 ||
             this.time - this.impactAt >= s.settleDelay * 8)) {
          this.nameTheMoon();
          this.phase = PHASE.SETTLED;
          this.settledAt = this.time;
        }
        break;

      case PHASE.SETTLED:
        if (!s.colonyEnabled || this.time - this.settledAt < s.colonyDelay) { break; }

        // 軌道が落ち着いてから置く。楕円のままだと L4・L5 が固定点にならず、
        // 置いてもすぐ離れてしまう（実測：離心率が高い種では1割も留まらない）。
        var e = this.moonEccentricity();
        var arrived = this.moonAtTarget();
        var waited = this.time - this.settledAt >= s.colonySettleTimeout;
        if ((e >= 0 && e <= s.colonyMaxEccentricity && arrived) || waited) {
          if (this.placeColonies()) { this.phase = PHASE.COLONY; }
        }
        break;
    }
  };

  /*
   * 地球と月のラグランジュ点 L4・L5 へコロニーを置く。
   *
   * L4・L5 は、地球と月と正三角形をつくる位置である。置いたあとは何もしない。
   * ほかの天体と同じ式で動き、留まるかどうかは計算が決める。
   */
  Simulation.prototype.placeColonies = function () {
    var earth = this.earthIndex;
    var moon = this.heaviestOtherThanEarth();
    if (earth < 0 || moon < 0) { return false; }

    var rx = this.px[moon] - this.px[earth];
    var ry = this.py[moon] - this.py[earth];
    var rz = this.pz[moon] - this.pz[earth];
    var distance = Math.sqrt(rx * rx + ry * ry + rz * rz);
    if (distance < 1e-3) { return false; }

    var vx = this.vx[moon] - this.vx[earth];
    var vy = this.vy[moon] - this.vy[earth];
    var vz = this.vz[moon] - this.vz[earth];

    // 公転の角速度。ω = (r × v) / |r|^2
    var wx = (ry * vz - rz * vy) / (distance * distance);
    var wy = (rz * vx - rx * vz) / (distance * distance);
    var wz = (rx * vy - ry * vx) / (distance * distance);
    if (wx * wx + wy * wy + wz * wz < 1e-12) { return false; }

    var mE = this.mass[earth];
    var mM = this.mass[moon];
    var total = mE + mM;

    // 重心。全体はこの点のまわりで回っている。
    var bx = (this.px[earth] * mE + this.px[moon] * mM) / total;
    var by = (this.py[earth] * mE + this.py[moon] * mM) / total;
    var bz = (this.pz[earth] * mE + this.pz[moon] * mM) / total;
    var bvx = (this.vx[earth] * mE + this.vx[moon] * mM) / total;
    var bvy = (this.vy[earth] * mE + this.vy[moon] * mM) / total;
    var bvz = (this.vz[earth] * mE + this.vz[moon] * mM) / total;

    var mass = this.settings.totalMass * this.settings.colonyMassFraction;

    // 地球から見て、月の向きを軌道面内で ±60度まわした先が L4 と L5。
    this.addColony(earth, rx, ry, rz, wx, wy, wz, 60, bx, by, bz, bvx, bvy, bvz, mass);
    this.addColony(earth, rx, ry, rz, wx, wy, wz, -60, bx, by, bz, bvx, bvy, bvz, mass);
    return true;
  };

  Simulation.prototype.addColony = function (
    earth, rx, ry, rz, wx, wy, wz, degrees, bx, by, bz, bvx, bvy, bvz, mass) {
    var w = Math.sqrt(wx * wx + wy * wy + wz * wz);
    var ax = wx / w, ay = wy / w, az = wz / w;

    // ロドリゲスの回転公式。軌道面の法線まわりに回す。
    var a = degrees * Math.PI / 180;
    var c = Math.cos(a), s = Math.sin(a);
    var dot = ax * rx + ay * ry + az * rz;
    var cxv = ay * rz - az * ry;
    var cyv = az * rx - ax * rz;
    var czv = ax * ry - ay * rx;

    var ox = rx * c + cxv * s + ax * dot * (1 - c);
    var oy = ry * c + cyv * s + ay * dot * (1 - c);
    var oz = rz * c + czv * s + az * dot * (1 - c);

    var x = this.px[earth] + ox;
    var y = this.py[earth] + oy;
    var z = this.pz[earth] + oz;

    // 速度は、重心のまわりを同じ角速度で回るものとして決める。v = v_重心 + ω × (位置 - 重心)
    var qx = x - bx, qy = y - by, qz = z - bz;

    this.add(
      x, y, z,
      bvx + (wy * qz - wz * qy),
      bvy + (wz * qx - wx * qz),
      bvz + (wx * qy - wy * qx),
      mass, KIND.COLONY);
  };

  /*
   * 月の軌道の離心率。0で円、1以上は地球に束縛されていない。
   * 月が無いときは -1 を返す。
   */
  Simulation.prototype.moonEccentricity = function () {
    var earth = this.earthIndex;
    var moon = this.heaviestOtherThanEarth();
    if (earth < 0 || moon < 0) { return -1; }

    var rx = this.px[moon] - this.px[earth];
    var ry = this.py[moon] - this.py[earth];
    var rz = this.pz[moon] - this.pz[earth];
    var vx = this.vx[moon] - this.vx[earth];
    var vy = this.vy[moon] - this.vy[earth];
    var vz = this.vz[moon] - this.vz[earth];

    var r = Math.sqrt(rx * rx + ry * ry + rz * rz);
    if (r < 1e-6) { return -1; }

    var mu = this.settings.gravity * (this.mass[earth] + this.mass[moon]);
    if (mu <= 0) { return -1; }

    var v2 = vx * vx + vy * vy + vz * vz;
    var rv = rx * vx + ry * vy + rz * vz;

    var ex = ((v2 - mu / r) * rx - rv * vx) / mu;
    var ey = ((v2 - mu / r) * ry - rv * vy) / mu;
    var ez = ((v2 - mu / r) * rz - rv * vz) / mu;

    return Math.sqrt(ex * ex + ey * ey + ez * ez);
  };

  /* 月が運び先の距離に来たか。 */
  Simulation.prototype.moonAtTarget = function () {
    var earth = this.earthIndex;
    var moon = this.heaviestOtherThanEarth();
    if (earth < 0 || moon < 0) { return false; }

    var dx = this.px[moon] - this.px[earth];
    var dy = this.py[moon] - this.py[earth];
    var dz = this.pz[moon] - this.pz[earth];
    var distance = Math.sqrt(dx * dx + dy * dy + dz * dz);
    var target = this.settings.moonTargetRadii * this.radius[earth];

    return Math.abs(distance - target) <= target * 0.06;
  };

  Simulation.prototype.heaviestOtherThanEarth = function () {
    var best = -1, bestMass = 0;
    for (var i = 0; i < this.count; i++) {
      if (this.kind[i] === KIND.EARTH || this.kind[i] === KIND.COLONY) { continue; }
      if (this.mass[i] > bestMass) { best = i; bestMass = this.mass[i]; }
    }
    return best;
  };

  /*
   * コロニーが、いまのL4・L5からどれだけずれているか。
   * 月までの距離を1としたときの割合で返す。留まっていれば小さいままになる。
   */
  Simulation.prototype.colonyDrift = function () {
    var earth = this.earthIndex;
    var moon = this.heaviestOtherThanEarth();
    if (earth < 0 || moon < 0) { return -1; }

    var rx = this.px[moon] - this.px[earth];
    var ry = this.py[moon] - this.py[earth];
    var rz = this.pz[moon] - this.pz[earth];
    var distance = Math.sqrt(rx * rx + ry * ry + rz * rz);
    if (distance < 1e-3) { return -1; }

    var worst = 0;
    var found = false;

    for (var i = 0; i < this.count; i++) {
      if (this.kind[i] !== KIND.COLONY) { continue; }
      found = true;

      // いちばん近いラグランジュ点との差を見る。L4かL5かは問わない。
      var best = Infinity;
      for (var sign = -1; sign <= 1; sign += 2) {
        var target = this.lagrangePoint(earth, rx, ry, rz, sign * 60);
        var dx = this.px[i] - target[0];
        var dy = this.py[i] - target[1];
        var dz = this.pz[i] - target[2];
        var d = Math.sqrt(dx * dx + dy * dy + dz * dz);
        if (d < best) { best = d; }
      }

      if (best / distance > worst) { worst = best / distance; }
    }

    return found ? worst : -1;
  };

  Simulation.prototype.lagrangePoint = function (earth, rx, ry, rz, degrees) {
    var moon = this.heaviestOtherThanEarth();
    var vx = this.vx[moon] - this.vx[earth];
    var vy = this.vy[moon] - this.vy[earth];
    var vz = this.vz[moon] - this.vz[earth];

    var wx = ry * vz - rz * vy;
    var wy = rz * vx - rx * vz;
    var wz = rx * vy - ry * vx;
    var w = Math.sqrt(wx * wx + wy * wy + wz * wz);
    if (w < 1e-9) { return [this.px[earth], this.py[earth], this.pz[earth]]; }

    var ax = wx / w, ay = wy / w, az = wz / w;
    var a = degrees * Math.PI / 180;
    var c = Math.cos(a), s = Math.sin(a);
    var dot = ax * rx + ay * ry + az * rz;

    return [
      this.px[earth] + rx * c + (ay * rz - az * ry) * s + ax * dot * (1 - c),
      this.py[earth] + ry * c + (az * rx - ax * rz) * s + ay * dot * (1 - c),
      this.pz[earth] + rz * c + (ax * ry - ay * rx) * s + az * dot * (1 - c)
    ];
  };

  root.FormationSim = Simulation;

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = Simulation;
  }
}(typeof window !== 'undefined' ? window : globalThis));
