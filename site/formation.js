/*
 * 「地球ができるまで」の描画と操作。計算は formation-sim.js が持つ。
 *
 * 天体は数百個になるため、1個ずつ図形を描かない。種別ごとに球の絵を
 * あらかじめ1枚作り、それを拡大縮小して並べる。奥のものから順に描く。
 */
(function () {
  'use strict';

  var Sim = window.FormationSim;
  if (!Sim) { return; }

  var KIND = Sim.KIND;

  /* 種別ごとの色と光り方。Unity版の SimulationView と同じ配色にしてある。 */
  var LOOKS = [];
  LOOKS[KIND.PLANETESIMAL] = { color: [107, 102, 97], glow: 0 };
  LOOKS[KIND.EARTH] = { color: [217, 107, 51], glow: 0.55 };
  LOOKS[KIND.IMPACTOR] = { color: [179, 82, 107], glow: 0.25 };
  LOOKS[KIND.DEBRIS] = { color: [242, 158, 77], glow: 0.7 };
  LOOKS[KIND.MOON] = { color: [184, 186, 194], glow: 0 };

  var COUNT_CHOICES = [120, 200, 280, 380, 520, 700];
  var AIM_CHOICES = [0, 0.4, 0.8, 1.2, 1.6];
  var AIM_LABELS = ['正面から', 'やや斜めから', '斜めから', 'かすめて', '大きくかすめて'];
  var EJECTA_CHOICES = [0.008, 0.014, 0.022, 0.032, 0.045];
  var ATTRACTION_CHOICES = [1, 10, 30, 60, 100];
  var ATTRACTION_LABELS = ['1倍（そのまま）', '10倍', '30倍', '60倍', '100倍'];
  var SPEEDS = [0.5, 1, 2, 4];

  var FOV_Y = 55 * Math.PI / 180;
  var MIN_FRAME_RADIUS = 7;
  var FRAME_MASS_FRACTION = 0.94;
  var FRAME_BUCKETS = 24;
  var FOLLOW_SPEED = 2.4;

  var canvas = document.getElementById('formation-canvas');
  var phaseText = document.getElementById('formation-phase');
  var readout = document.getElementById('formation-readout');
  if (!canvas || !canvas.getContext) { return; }

  var ctx = canvas.getContext('2d');
  var width = 0, height = 0, dpr = 1;

  var options = {
    planetesimalCount: COUNT_CHOICES[3],
    impactParameter: AIM_CHOICES[2],
    ejectaMassFraction: EJECTA_CHOICES[2],
    debrisAttraction: ATTRACTION_CHOICES[3]
  };

  var sim = new Sim(options);

  var reduceMotion = window.matchMedia &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  var paused = reduceMotion;
  var speedIndex = 1;
  var accumulator = 0;
  var lastFrame = 0;
  var histogram = new Float64Array(FRAME_BUCKETS);

  /* 視点 */
  var yaw = 24, pitch = 26, zoom = 1;
  var smoothX = 0, smoothY = 0, smoothZ = 0, smoothRadius = 34;
  var framed = false;

  var sprites = [];
  var stars = null;
  var order = [];

  // ------------------------------------------------------------------
  // 球の絵
  // ------------------------------------------------------------------

  function makeSprite(look) {
    var size = 128;
    var halo = look.glow > 0 ? 2.1 : 1.06;
    var off = document.createElement('canvas');
    off.width = size;
    off.height = size;
    var c = off.getContext('2d');

    var center = size / 2;
    var disc = center / halo;
    var rgb = look.color;
    var base = 'rgba(' + rgb[0] + ',' + rgb[1] + ',' + rgb[2] + ',';

    if (look.glow > 0) {
      // まわりのにじみ。熱を持っている段階を光って見せる。温度の計算ではない。
      var glow = c.createRadialGradient(center, center, disc * 0.8, center, center, center);
      glow.addColorStop(0, base + (0.55 * look.glow).toFixed(3) + ')');
      glow.addColorStop(0.45, base + (0.16 * look.glow).toFixed(3) + ')');
      glow.addColorStop(1, base + '0)');
      c.fillStyle = glow;
      c.fillRect(0, 0, size, size);
    }

    // 球そのもの。左上から当たった光を想定して、明るい点をずらす。
    var lightX = center - disc * 0.32;
    var lightY = center - disc * 0.34;
    var body = c.createRadialGradient(lightX, lightY, 0, center, center, disc);
    body.addColorStop(0, 'rgb(' + mix(rgb, 255, 0.55) + ')');
    body.addColorStop(0.45, 'rgb(' + rgb.join(',') + ')');
    body.addColorStop(0.88, 'rgb(' + mix(rgb, 0, 0.55) + ')');
    body.addColorStop(1, 'rgb(' + mix(rgb, 0, 0.78) + ')');

    c.save();
    c.beginPath();
    c.arc(center, center, disc, 0, Math.PI * 2);
    c.clip();
    c.fillStyle = body;
    c.fillRect(0, 0, size, size);
    c.restore();

    return { image: off, halo: halo };
  }

  function mix(rgb, towards, amount) {
    var out = [];
    for (var i = 0; i < 3; i++) {
      out.push(Math.round(rgb[i] + (towards - rgb[i]) * amount));
    }
    return out.join(',');
  }

  function buildSprites() {
    for (var k = 0; k < LOOKS.length; k++) {
      sprites[k] = LOOKS[k] ? makeSprite(LOOKS[k]) : null;
    }
  }

  /* 背景の星。空間に固定するので、視点を回すと一緒に流れる。 */
  function buildStars() {
    var rng = new Sim({ planetesimalCount: 1, seed: 7 }).rng;
    var list = [];
    for (var i = 0; i < 260; i++) {
      var z = rng.range(-1, 1);
      var a = rng.range(0, Math.PI * 2);
      var r = Math.sqrt(Math.max(0, 1 - z * z));
      list.push({
        x: r * Math.cos(a), y: r * Math.sin(a), z: z,
        size: rng.range(0.6, 1.9),
        alpha: rng.range(0.20, 0.75)
      });
    }
    return list;
  }

  // ------------------------------------------------------------------
  // 画面の大きさ
  // ------------------------------------------------------------------

  function resize() {
    var rect = canvas.getBoundingClientRect();
    // 端末の細かさをそのまま使うと重い端末で落ちる。2倍で頭打ちにする。
    dpr = Math.min(2, window.devicePixelRatio || 1);
    width = Math.max(1, Math.round(rect.width));
    height = Math.max(1, Math.round(rect.height));
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  // ------------------------------------------------------------------
  // 収めたい範囲
  // ------------------------------------------------------------------

  function frameRadius(cx, cy, cz) {
    var n = sim.count;
    var total = sim.totalLiveMass;
    if (n === 0 || total <= 0) { return MIN_FRAME_RADIUS; }

    var farthest = 0;
    var i, d;
    for (i = 0; i < n; i++) {
      d = Math.sqrt(
        (sim.px[i] - cx) * (sim.px[i] - cx) +
        (sim.py[i] - cy) * (sim.py[i] - cy) +
        (sim.pz[i] - cz) * (sim.pz[i] - cz)) + sim.radius[i];
      if (d > farthest) { farthest = d; }
    }
    if (farthest <= 0) { return MIN_FRAME_RADIUS; }

    for (i = 0; i < FRAME_BUCKETS; i++) { histogram[i] = 0; }

    for (i = 0; i < n; i++) {
      d = Math.sqrt(
        (sim.px[i] - cx) * (sim.px[i] - cx) +
        (sim.py[i] - cy) * (sim.py[i] - cy) +
        (sim.pz[i] - cz) * (sim.pz[i] - cz)) + sim.radius[i];
      var bucket = Math.min(FRAME_BUCKETS - 1,
        Math.max(0, Math.floor(d / farthest * (FRAME_BUCKETS - 1))));
      histogram[bucket] += sim.mass[i];
    }

    var wanted = total * FRAME_MASS_FRACTION;
    var running = 0;
    var byMass = farthest;
    for (i = 0; i < FRAME_BUCKETS; i++) {
      running += histogram[i];
      if (running >= wanted) { byMass = (i + 1) / FRAME_BUCKETS * farthest; break; }
    }

    // 質量だけで決めると、地球が全体のほぼすべてを占めた時点で地球へ寄りきってしまい、
    // 月が画面の外へ出る。重い上位2つは必ず入れる。
    // 終盤は地球と月、衝突の前は地球と衝突天体、破片のあいだは地球と最大の塊にあたる。
    return Math.max(byMass, topTwoRadius(cx, cy, cz));
  }

  /* 最も重い2つが収まる半径。少し余白を足す。 */
  function topTwoRadius(cx, cy, cz) {
    var first = -1, second = -1;
    var firstMass = -1, secondMass = -1;
    var i;

    for (i = 0; i < sim.count; i++) {
      if (sim.mass[i] > firstMass) {
        second = first; secondMass = firstMass;
        first = i; firstMass = sim.mass[i];
      } else if (sim.mass[i] > secondMass) {
        second = i; secondMass = sim.mass[i];
      }
    }

    var radius = 0;
    var picks = [first, second];
    for (i = 0; i < picks.length; i++) {
      var k = picks[i];
      if (k < 0) { continue; }
      var d = Math.sqrt(
        (sim.px[k] - cx) * (sim.px[k] - cx) +
        (sim.py[k] - cy) * (sim.py[k] - cy) +
        (sim.pz[k] - cz) * (sim.pz[k] - cz)) + sim.radius[k] * 1.6;
      if (d > radius) { radius = d; }
    }

    return radius * 1.2;
  }

  function distanceFor(radius) {
    var aspect = width / Math.max(1, height);
    var halfV = FOV_Y / 2;
    var halfH = Math.atan(Math.tan(halfV) * Math.max(0.1, aspect));
    var half = Math.min(halfV, halfH);
    return radius / Math.max(0.05, Math.tan(half));
  }

  // ------------------------------------------------------------------
  // 描画
  // ------------------------------------------------------------------

  function draw() {
    ctx.fillStyle = '#04060c';
    ctx.fillRect(0, 0, width, height);

    var target = sim.largestIndex >= 0
      ? [sim.px[sim.largestIndex], sim.py[sim.largestIndex], sim.pz[sim.largestIndex]]
      : [sim.centerX, sim.centerY, sim.centerZ];

    var wanted = Math.max(MIN_FRAME_RADIUS, frameRadius(target[0], target[1], target[2]));

    if (!framed) {
      smoothX = target[0]; smoothY = target[1]; smoothZ = target[2];
      smoothRadius = wanted;
      framed = true;
    } else {
      var t = 1 - Math.exp(-FOLLOW_SPEED * 0.016);
      smoothX += (target[0] - smoothX) * t;
      smoothY += (target[1] - smoothY) * t;
      smoothZ += (target[2] - smoothZ) * t;
      smoothRadius += (wanted - smoothRadius) * t;
    }

    var distance = distanceFor(smoothRadius * zoom);
    var py = pitch * Math.PI / 180;
    var yw = yaw * Math.PI / 180;

    // カメラの位置。注視点から、向きの分だけ離れたところに置く。
    var ox = Math.cos(py) * Math.sin(yw);
    var oy = Math.sin(py);
    var oz = Math.cos(py) * Math.cos(yw);

    var camX = smoothX + ox * distance;
    var camY = smoothY + oy * distance;
    var camZ = smoothZ + oz * distance;

    // 前・右・上の3方向
    var fx = -ox, fy = -oy, fz = -oz;

    // 右 = 前 × 上（上は世界の真上 (0,1,0)）。展開すると (-fz, 0, fx) になる。
    var rx = -fz, ry = 0, rz = fx;
    var rl = Math.sqrt(rx * rx + ry * ry + rz * rz) || 1;
    rx /= rl; ry /= rl; rz /= rl;
    // up = cross(right, forward)
    var ux = ry * fz - rz * fy;
    var uy = rz * fx - rx * fz;
    var uz = rx * fy - ry * fx;

    var focal = (height / 2) / Math.tan(FOV_Y / 2);
    var cx = width / 2, cy = height / 2;
    var near = Math.max(0.05, distance * 0.01);

    drawStars(rx, ry, rz, ux, uy, uz, fx, fy, fz, focal, cx, cy);

    var n = sim.count;
    if (order.length < n) { order = new Array(n); }

    var visible = 0;
    var depth = drawBodiesPrepare(n, camX, camY, camZ, rx, ry, rz, ux, uy, uz, fx, fy, fz, near);
    visible = depth.length;

    // 奥から順に描く。手前のものが後から重なるようにするため。
    depth.sort(function (a, b) { return b.z - a.z; });

    for (var i = 0; i < visible; i++) {
      var item = depth[i];
      var sprite = sprites[item.kind];
      if (!sprite) { continue; }

      var screenR = item.radius * focal / item.z;
      // 1画素を切ると消えてしまう。最小の大きさを持たせる。
      if (screenR < 0.7) { screenR = 0.7; }

      var size = screenR * 2 * sprite.halo;
      var sx = cx + item.x * focal / item.z;
      var sy = cy - item.y * focal / item.z;

      if (sx + size < 0 || sx - size > width || sy + size < 0 || sy - size > height) { continue; }

      ctx.drawImage(sprite.image, sx - size / 2, sy - size / 2, size, size);
    }
  }

  function drawBodiesPrepare(n, camX, camY, camZ, rx, ry, rz, ux, uy, uz, fx, fy, fz, near) {
    var list = [];
    for (var i = 0; i < n; i++) {
      var dx = sim.px[i] - camX;
      var dy = sim.py[i] - camY;
      var dz = sim.pz[i] - camZ;

      var z = dx * fx + dy * fy + dz * fz;
      if (z <= near) { continue; }

      list.push({
        x: dx * rx + dy * ry + dz * rz,
        y: dx * ux + dy * uy + dz * uz,
        z: z,
        radius: sim.radius[i],
        kind: sim.kind[i]
      });
    }
    return list;
  }

  function drawStars(rx, ry, rz, ux, uy, uz, fx, fy, fz, focal, cx, cy) {
    if (!stars) { return; }
    for (var i = 0; i < stars.length; i++) {
      var s = stars[i];
      var z = s.x * fx + s.y * fy + s.z * fz;
      if (z <= 0.001) { continue; }
      var x = s.x * rx + s.y * ry + s.z * rz;
      var y = s.x * ux + s.y * uy + s.z * uz;
      var sx = cx + x / z * focal * 0.9;
      var sy = cy - y / z * focal * 0.9;
      if (sx < 0 || sx > width || sy < 0 || sy > height) { continue; }
      ctx.fillStyle = 'rgba(210,228,245,' + s.alpha.toFixed(2) + ')';
      ctx.fillRect(sx, sy, s.size, s.size);
    }
  }

  // ------------------------------------------------------------------
  // 進める
  // ------------------------------------------------------------------

  function advance(elapsed) {
    if (paused) { accumulator = 0; return; }

    var step = Math.max(0.001, sim.settings.stepSize);
    accumulator += elapsed * SPEEDS[speedIndex];

    var limit = Math.max(1, sim.settings.maxStepsPerFrame);
    var steps = 0;
    while (accumulator >= step && steps < limit) {
      sim.step(step);
      accumulator -= step;
      steps++;
    }

    // 追いつけないときは溜めずに捨てる。溜めると重くなるほど加速して破綻する。
    if (accumulator > step * limit) { accumulator = 0; }
  }

  function refreshText() {
    phaseText.textContent = Sim.phaseLabel(sim.phase);

    var share = sim.totalLiveMass > 0 ? sim.largestMass / sim.totalLiveMass * 100 : 0;
    readout.textContent =
      '天体 ' + sim.count + '個　合体 ' + sim.mergeEvents + '回　' +
      'いちばん重い塊 ' + share.toFixed(0) + '%　経過 ' + sim.time.toFixed(1) +
      '　種 ' + sim.settings.seed;
  }

  function loop(now) {
    var elapsed = lastFrame ? (now - lastFrame) / 1000 : 0;
    lastFrame = now;
    // タブを戻したときに一気に進めない。
    if (elapsed > 0.1) { elapsed = 0.1; }

    advance(elapsed);
    draw();
    refreshText();

    window.requestAnimationFrame(loop);
  }

  // ------------------------------------------------------------------
  // 操作
  // ------------------------------------------------------------------

  var pointers = {};
  var pointerCount = 0;
  var lastPinch = 0;
  var lastX = 0, lastY = 0;

  canvas.addEventListener('pointerdown', function (event) {
    canvas.setPointerCapture(event.pointerId);
    pointers[event.pointerId] = { x: event.clientX, y: event.clientY };
    pointerCount++;
    lastX = event.clientX;
    lastY = event.clientY;
    if (pointerCount === 2) { lastPinch = pinchDistance(); }
  });

  canvas.addEventListener('pointermove', function (event) {
    if (!pointers[event.pointerId]) { return; }
    pointers[event.pointerId].x = event.clientX;
    pointers[event.pointerId].y = event.clientY;

    if (pointerCount >= 2) {
      // 指2本のあいだは回さない。寄せるつもりの操作で視点が振られるのを防ぐ。
      var distance = pinchDistance();
      if (lastPinch > 0) {
        var delta = distance - lastPinch;
        zoom = clamp(zoom * (1 - delta / Math.max(1, height) * 1.6), 0.18, 4);
      }
      lastPinch = distance;
      return;
    }

    var scale = 200 / Math.max(1, height);
    yaw -= (event.clientX - lastX) * scale;
    pitch = clamp(pitch + (event.clientY - lastY) * scale, -82, 82);
    lastX = event.clientX;
    lastY = event.clientY;
  });

  function releasePointer(event) {
    if (pointers[event.pointerId]) {
      delete pointers[event.pointerId];
      pointerCount = Math.max(0, pointerCount - 1);
    }
    if (pointerCount < 2) { lastPinch = 0; }
  }

  canvas.addEventListener('pointerup', releasePointer);
  canvas.addEventListener('pointercancel', releasePointer);

  function pinchDistance() {
    var list = [];
    for (var id in pointers) {
      if (Object.prototype.hasOwnProperty.call(pointers, id)) { list.push(pointers[id]); }
    }
    if (list.length < 2) { return 0; }
    var dx = list[0].x - list[1].x;
    var dy = list[0].y - list[1].y;
    return Math.sqrt(dx * dx + dy * dy);
  }

  canvas.addEventListener('wheel', function (event) {
    event.preventDefault();
    var notches = event.deltaY / 100;
    zoom = clamp(zoom * (1 + notches * 0.12), 0.18, 4);
  }, { passive: false });

  function clamp(value, min, max) {
    return value < min ? min : (value > max ? max : value);
  }

  // ------------------------------------------------------------------
  // 画面の部品
  // ------------------------------------------------------------------

  var playButton = document.getElementById('formation-play');
  var speedButton = document.getElementById('formation-speed');

  playButton.addEventListener('click', function () {
    paused = !paused;
    playButton.textContent = paused ? '再生' : '一時停止';
  });
  playButton.textContent = paused ? '再生' : '一時停止';

  speedButton.addEventListener('click', function () {
    speedIndex = (speedIndex + 1) % SPEEDS.length;
    speedButton.textContent = '速さ ×' + SPEEDS[speedIndex];
  });

  document.getElementById('formation-reset-view').addEventListener('click', function () {
    yaw = 24; pitch = 26; zoom = 1;
  });

  document.getElementById('formation-reseed').addEventListener('click', function () {
    restart(sim.settings.seed + 1);
  });

  document.getElementById('formation-apply').addEventListener('click', function () {
    restart(sim.settings.seed);
  });

  function restart(seed) {
    accumulator = 0;
    framed = false;
    sim = new Sim({
      seed: seed,
      planetesimalCount: options.planetesimalCount,
      impactParameter: options.impactParameter,
      ejectaMassFraction: options.ejectaMassFraction,
      debrisAttraction: options.debrisAttraction
    });
  }

  function bindRange(id, choices, format, apply) {
    var input = document.getElementById(id);
    var label = document.getElementById(id + '-value');
    if (!input || !label) { return; }

    function update() {
      var index = Math.min(choices.length - 1, Math.max(0, parseInt(input.value, 10) || 0));
      label.textContent = format(choices[index], index);
      apply(choices[index]);
    }

    input.addEventListener('input', update);
    update();
  }

  bindRange('formation-count', COUNT_CHOICES,
    function (v) { return v + '個'; },
    function (v) { options.planetesimalCount = v; });

  bindRange('formation-aim', AIM_CHOICES,
    function (v, i) { return AIM_LABELS[i]; },
    function (v) { options.impactParameter = v; });

  bindRange('formation-ejecta', EJECTA_CHOICES,
    function (v) { return (v * 100).toFixed(1) + '%'; },
    function (v) { options.ejectaMassFraction = v; });

  bindRange('formation-attraction', ATTRACTION_CHOICES,
    function (v, i) { return ATTRACTION_LABELS[i]; },
    function (v) { options.debrisAttraction = v; });

  // ------------------------------------------------------------------

  window.addEventListener('resize', resize);

  buildSprites();
  stars = buildStars();
  resize();
  window.requestAnimationFrame(loop);
}());
