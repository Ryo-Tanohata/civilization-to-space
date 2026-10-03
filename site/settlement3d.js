/* 集落の暮らし：3D表示（Three.js）
   シミュレーションの中身は settlement.html のものをそのまま使い、ここでは見せ方だけを受け持つ。
   世界の (x, y) は 3D の (x, z) に、高さは地形から求める。 */
'use strict';
(function () {
  function createSettlement3D(api) {
    const T = window.THREE;
    if (!T) return null;
    const host = api.host;
    const renderer = new T.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    renderer.setPixelRatio(Math.min(2, window.devicePixelRatio || 1));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = T.PCFSoftShadowMap;
    renderer.toneMapping = T.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.05;
    const el = renderer.domElement;
    el.className = 'view3d';
    el.setAttribute('aria-label', '立体の集落。ドラッグで回り込み、ピンチやホイールで寄る。人をタップすると観察できる');
    el.style.display = 'none';
    host.appendChild(el);

    const scene = new T.Scene();
    const camera = new T.PerspectiveCamera(45, 1, 0.3, 900);
    const world = new T.Group(); scene.add(world);
    const hemi = new T.HemisphereLight(0xcfe3ff, 0x5a5236, 1.1); scene.add(hemi);
    const sun = new T.DirectionalLight(0xfff1d6, 2.6);
    sun.castShadow = true;
    sun.shadow.mapSize.set(2048, 2048);
    sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.04;
    scene.add(sun); scene.add(sun.target);
    scene.fog = new T.Fog(0xb9d3e6, 140, 560);

    // 空：上から下へ色が変わる大きな球
    const skyGeo = new T.SphereGeometry(600, 24, 12);
    const skyCol = new Float32Array(skyGeo.attributes.position.count * 3);
    skyGeo.setAttribute('color', new T.BufferAttribute(skyCol, 3));
    const sky = new T.Mesh(skyGeo, new T.MeshBasicMaterial({ vertexColors: true, side: T.BackSide, fog: false, depthWrite: false }));
    scene.add(sky);
    function paintSky(top, horizon) {
      const p = skyGeo.attributes.position, a = new T.Color(top), b = new T.Color(horizon), c = new T.Color();
      for (let i = 0; i < p.count; i++) {
        const t = Math.max(0, Math.min(1, p.getY(i) / 600 * 1.6 + 0.05));
        c.copy(b).lerp(a, Math.pow(t, 0.7));
        skyCol[i * 3] = c.r; skyCol[i * 3 + 1] = c.g; skyCol[i * 3 + 2] = c.b;
      }
      skyGeo.attributes.color.needsUpdate = true;
    }

    /* ---------- 材質と形の小道具 ---------- */
    let mats = new Map(), geos = [];
    function M(color, o) {
      o = o || {};
      const key = color + JSON.stringify(o);
      if (!mats.has(key)) mats.set(key, new T.MeshStandardMaterial(Object.assign({ color, roughness: 0.88, metalness: 0 }, o)));
      return mats.get(key);
    }
    function G(g) { geos.push(g); return g; }
    function mesh(geo, mat, x, y, z, o) {
      const m = new T.Mesh(geo, mat);
      m.position.set(x || 0, y || 0, z || 0);
      if (o) { if (o.rx) m.rotation.x = o.rx; if (o.ry) m.rotation.y = o.ry; if (o.rz) m.rotation.z = o.rz; if (o.s) m.scale.setScalar(o.s); if (o.sv) m.scale.set(...o.sv); }
      m.castShadow = !(o && o.noCast); m.receiveShadow = true;
      return m;
    }
    function prism(w, h, len) {   // 切妻屋根の三角柱
      const sh = new T.Shape(); sh.moveTo(-w / 2, 0); sh.lineTo(w / 2, 0); sh.lineTo(0, h); sh.lineTo(-w / 2, 0);
      const g = new T.ExtrudeGeometry(sh, { depth: len, bevelEnabled: false }); g.translate(0, 0, -len / 2); return G(g);
    }
    function textSprite(text, opts) {
      opts = opts || {};
      const c = document.createElement('canvas'), g = c.getContext('2d'), fs = 44;
      g.font = `${opts.bold ? 700 : 500} ${fs}px system-ui, sans-serif`;
      const w = Math.ceil(g.measureText(text).width) + 36;
      c.width = opts.circle ? 64 : w; c.height = 64;
      g.font = `${opts.bold ? 700 : 500} ${fs}px system-ui, sans-serif`;
      g.fillStyle = opts.bg || 'rgba(20,16,24,.66)';
      g.beginPath(); opts.circle ? g.arc(32, 32, 30, 0, 7) : (g.roundRect ? g.roundRect(0, 4, c.width, 56, 28) : g.rect(0, 4, c.width, 56)); g.fill();
      g.fillStyle = opts.fg || '#f1e9da'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(text, c.width / 2, 34);
      const tex = new T.CanvasTexture(c); tex.colorSpace = T.SRGBColorSpace;
      const sp = new T.Sprite(new T.SpriteMaterial({ map: tex, depthTest: false, depthWrite: false, sizeAttenuation: false }));
      const hgt = opts.size || 0.045;
      sp.scale.set(hgt * c.width / 64, hgt, 1); sp.renderOrder = 10;
      sp.userData.dispose = () => { tex.dispose(); sp.material.dispose(); };
      return sp;
    }
    function stripeTex(a, b) {
      const c = document.createElement('canvas'); c.width = 64; c.height = 8; const g = c.getContext('2d');
      for (let i = 0; i < 8; i++) { g.fillStyle = i % 2 ? b : a; g.fillRect(i * 8, 0, 8, 8); }
      const t = new T.CanvasTexture(c); t.colorSpace = T.SRGBColorSpace; return t;
    }

    /* ---------- 地形 ---------- */
    let sim, W, H, hgtFn, riverPts = null, groundTex = null;
    const toV = (x, y, up) => new T.Vector3(x - W / 2, (up || 0) + hgtFn(x, y), y - H / 2);
    function makeHeight(L) {
      const paved = !!L.paved, amp = paved ? 0.25 : 1.1;
      const smooth = (a, b, v) => { const t = Math.max(0, Math.min(1, (v - a) / (b - a))); return t * t * (3 - 2 * t); };
      const riverD = (x, y) => { if (!riverPts) return 99; let d = 99; for (const [px, py] of riverPts) d = Math.min(d, Math.hypot(px - x, py - y)); return d; };
      const base = (x, y) => {
        let n = Math.sin(x * 0.15 + 1.3) * Math.sin(y * 0.18 + 0.7) * 0.7 + Math.sin(x * 0.37 + 2) * Math.cos(y * 0.29) * 0.3;
        let f = 1; for (const l of sim.locs) f *= smooth(l.r + 1, l.r + 6, Math.hypot(l.x - x, l.y - y));
        const e = Math.min(x, W - x, y, H - y);
        return (n + 0.6) * amp * f + Math.pow(Math.max(0, 9 - e) / 9, 2) * (paved ? 1.2 : 3.5);
      };
      const fn = (x, y) => {
        let h = base(x, y);
        const d = riverD(x, y);
        if (d < 4.2) h -= 0.9 * smooth(4.2, 1.8, d);
        return h;
      };
      fn.base = base;
      return fn;
    }

    /* ---------- 場所 ---------- */
    const animated = [];   // 毎フレーム動かすもの
    let fireLights = [], seatMarks = [];
    function buildLocation(l, L) {
      const g = new T.Group();
      const c = toV(l.x, l.y); g.position.copy(c);
      const add = (m) => (g.add(m), m);
      const stone = M('#9a958a'), wood = M('#6a4a2c'), darkWood = M('#4a3420');
      switch (l.kind) {
        case 'fire': {
          const sg = G(new T.DodecahedronGeometry(0.38, 0));
          for (let i = 0; i < 11; i++) { const a = i / 11 * Math.PI * 2; add(mesh(sg, i % 2 ? stone : M('#aaa597'), Math.cos(a) * 1.7, 0.2, Math.sin(a) * 1.7, { ry: a, s: 0.9 + (i % 3) * 0.1 })); }
          const logG = G(new T.CylinderGeometry(0.28, 0.3, 2.1, 8));
          l.seatList.forEach(st => {
            const a = Math.atan2(st.y - l.y, st.x - l.x), r = Math.hypot(st.x - l.x, st.y - l.y) + 0.9;
            add(mesh(logG, wood, Math.cos(a) * r, 0.28, Math.sin(a) * r, { rz: Math.PI / 2, ry: -a + Math.PI / 2 }));
          });
          const kindling = G(new T.CylinderGeometry(0.08, 0.1, 1.6, 5));
          for (let i = 0; i < 5; i++) add(mesh(kindling, darkWood, 0, 0.35, 0, { rz: 1.1, ry: i * 1.25 }));
          const flames = new T.Group(); flames.position.y = 0.3; add(flames);
          const f1 = new T.Mesh(G(new T.ConeGeometry(0.7, 1.9, 7)), M('#e2621e', { emissive: '#e2621e', emissiveIntensity: 1.6, transparent: true, opacity: 0.9 }));
          const f2 = new T.Mesh(G(new T.ConeGeometry(0.42, 1.3, 7)), M('#ffd25a', { emissive: '#ffc040', emissiveIntensity: 2, transparent: true, opacity: 0.95 }));
          f1.position.y = 0.9; f2.position.y = 0.7; flames.add(f1, f2);
          const light = new T.PointLight(0xff9a4a, 40, 26, 2); light.position.y = 1.6; light.castShadow = false; add(light);
          fireLights.push({ light, base: 40, night: 160 });
          animated.push(t => { const k = 0.85 + Math.sin(t * 11 + l.x) * 0.1 + Math.sin(t * 27) * 0.06; flames.scale.set(1, k, 1); flames.rotation.y = t * 0.7; light.intensity = light.userData.level * (0.9 + (k - 0.85)); });
          light.userData.level = 40;
          addSmoke(g, 0, 2.2, 0, 1);
          break; }
        case 'water':
          if (l.id === 'fountain') {
            add(mesh(G(new T.CylinderGeometry(3.6, 3.8, 0.8, 28)), M('#c4bcab'), 0, 0.4, 0));
            add(mesh(G(new T.CylinderGeometry(3.05, 3.05, 0.1, 28)), M('#3f7d9c', { roughness: 0.15, metalness: 0.1 }), 0, 0.68, 0, { noCast: true }));
            add(mesh(G(new T.CylinderGeometry(0.45, 0.6, 2.2, 12)), M('#d8d1c0'), 0, 1.4, 0));
            add(mesh(G(new T.CylinderGeometry(1.1, 0.4, 0.4, 16)), M('#d8d1c0'), 0, 2.5, 0));
            const jet = new T.Mesh(G(new T.ConeGeometry(0.25, 1.2, 8)), M('#d9f0ff', { transparent: true, opacity: 0.6, roughness: 0.1 }));
            jet.position.y = 3.2; add(jet);
            animated.push(t => { jet.scale.y = 0.85 + Math.sin(t * 6) * 0.15; });
          }
          break;
        case 'bush': {
          const bg = G(new T.IcosahedronGeometry(1.25, 1)), berry = G(new T.SphereGeometry(0.16, 6, 4));
          for (let i = 0; i < 6; i++) {
            const bx = Math.cos(i * 1.1) * l.r * 0.45, bz = Math.sin(i * 1.1) * l.r * 0.3;
            add(mesh(bg, M(i % 2 ? '#3d5a2c' : '#4c6a36', { flatShading: true }), bx, 0.9, bz, { sv: [1, 0.8, 1] }));
            for (let j = 0; j < 5; j++) { const a = j * 1.3 + i; add(mesh(berry, M(j % 2 ? '#8a2a5a' : '#b04a6a'), bx + Math.cos(a) * 1.1, 0.9 + Math.sin(j) * 0.5, bz + Math.sin(a) * 1.0, { noCast: true })); }
          }
          break; }
        case 'camp': {
          const tent = mesh(G(new T.ConeGeometry(2.4, 3.4, 9)), M('#b89a72', { flatShading: true }), -1.4, 1.7, -1.2); add(tent);
          add(mesh(G(new T.CylinderGeometry(0.06, 0.06, 1, 5)), darkWood, -1.4, 3.6, -1.2));
          const post = G(new T.CylinderGeometry(0.12, 0.14, 2.6, 6));
          add(mesh(post, darkWood, 1.6, 1.3, -1.8)); add(mesh(post, darkWood, 1.6, 1.3, 1.4));
          add(mesh(G(new T.CylinderGeometry(0.08, 0.08, 3.4, 6)), darkWood, 1.6, 2.5, -0.2, { rx: Math.PI / 2 }));
          const meat = G(new T.BoxGeometry(0.35, 0.8, 0.5));
          for (let i = 0; i < 3; i++) add(mesh(meat, M(['#9a5040', '#8a4030', '#a8604a'][i]), 1.6, 1.9, -1.2 + i * 1.0));
          add(mesh(G(new T.CylinderGeometry(0.6, 0.7, 0.7, 10)), wood, 0.3, 0.35, 1.7));
          break; }
        case 'field': {
          const w = l.r * 1.9, d = l.r * 1.25;
          add(mesh(G(new T.BoxGeometry(w, 0.3, d)), M('#5e4a30'), 0, 0.05, 0, { noCast: true }));
          const crop = G(new T.ConeGeometry(0.16, 0.9, 5));
          const rows = Math.floor(d / 0.9), cols = Math.floor((w - 0.6) / 0.45);
          const inst = new T.InstancedMesh(crop, M('#c9b048', { flatShading: true }), rows * cols);
          const dm = new T.Object3D(); let k = 0;
          for (let r = 0; r < rows; r++) for (let q = 0; q < cols; q++) {
            dm.position.set(-w / 2 + 0.5 + q * 0.45, 0.6, -d / 2 + 0.55 + r * 0.9);
            dm.rotation.set(Math.sin(k) * 0.12, 0, Math.cos(k * 1.3) * 0.12); dm.scale.setScalar(0.8 + ((k * 37) % 7) / 20);
            dm.updateMatrix(); inst.setMatrixAt(k++, dm.matrix);
          }
          inst.castShadow = true; inst.receiveShadow = true; add(inst);
          animated.push(t => { inst.rotation.z = Math.sin(t * 1.2) * 0.008; });
          const fp = G(new T.CylinderGeometry(0.08, 0.1, 1.1, 5));
          for (let u = -w / 2 - 0.4; u <= w / 2 + 0.5; u += 2) { add(mesh(fp, darkWood, u, 0.55, -d / 2 - 0.5)); add(mesh(fp, darkWood, u, 0.55, d / 2 + 0.5)); }
          const rail = G(new T.BoxGeometry(w + 1, 0.08, 0.08));
          add(mesh(rail, wood, 0, 0.85, -d / 2 - 0.5)); add(mesh(rail, wood, 0, 0.85, d / 2 + 0.5));
          break; }
        case 'mill': {
          add(mesh(G(new T.BoxGeometry(3.4, 0.6, 3.2)), M('#8e897d'), 0, 0.3, 0));
          add(mesh(G(new T.CylinderGeometry(1.35, 1.35, 0.5, 20)), M('#b5b0a5'), 0, 0.85, 0));
          const arm = new T.Group(); arm.position.y = 1.15; add(arm);
          arm.add(mesh(G(new T.BoxGeometry(1.8, 0.14, 0.14)), wood, 0.9, 0, 0));
          arm.add(mesh(G(new T.CylinderGeometry(0.07, 0.07, 0.7, 6)), wood, 1.7, 0.3, 0));
          animated.push((t, dt) => { arm.rotation.y += dt * (sim.npcs.some(n => n.loc === l && n.state === 'use') ? 2 : 0.15); });
          break; }
        case 'oven': {
          add(mesh(G(new T.SphereGeometry(2, 16, 10, 0, Math.PI * 2, 0, Math.PI / 2)), M('#8a5a36', { flatShading: true }), 0, 0, 0));
          add(mesh(G(new T.BoxGeometry(1.1, 0.9, 0.6)), M('#24160e'), 0, 0.45, 1.8, { noCast: true }));
          const glow = mesh(G(new T.PlaneGeometry(0.9, 0.6)), M('#ff8a3a', { emissive: '#ff6a20', emissiveIntensity: 1.5 }), 0, 0.4, 2.11, { noCast: true }); add(glow);
          add(mesh(G(new T.CylinderGeometry(0.3, 0.35, 1.4, 8)), M('#7a5034'), 0.8, 2.2, -0.6));
          const lg = G(new T.CylinderGeometry(0.16, 0.16, 1.6, 6));
          for (let i = 0; i < 6; i++) add(mesh(lg, i % 2 ? wood : M('#7a5838'), 2.7, 0.18 + Math.floor(i / 3) * 0.3, -0.4 + (i % 3) * 0.33, { rx: Math.PI / 2 }));
          const light = new T.PointLight(0xff8a3a, 15, 14, 2); light.position.set(0, 0.8, 2.6); add(light);
          light.userData.level = 15; fireLights.push({ light, base: 15, night: 70 });
          animated.push(t => { light.intensity = light.userData.level * (0.9 + Math.sin(t * 9) * 0.08); });
          addSmoke(g, 0.8, 3, -0.6, 0.6);
          break; }
        case 'well': {
          add(mesh(G(new T.CylinderGeometry(1.9, 2, 1.0, 20, 1, true)), M('#a39d90', { side: T.DoubleSide }), 0, 0.5, 0));
          add(mesh(G(new T.TorusGeometry(1.95, 0.18, 6, 24)), M('#b5afa2'), 0, 1.0, 0, { rx: Math.PI / 2 }));
          add(mesh(G(new T.CircleGeometry(1.8, 20)), M('#1f3342', { roughness: 0.2 }), 0, 0.3, 0, { rx: -Math.PI / 2, noCast: true }));
          const post = G(new T.BoxGeometry(0.25, 3, 0.25));
          add(mesh(post, wood, -2.1, 1.5, 0)); add(mesh(post, wood, 2.1, 1.5, 0));
          add(mesh(prism(2.4, 1.0, 5), M('#7a5a3a', { flatShading: true }), 0, 3.0, 0, { ry: Math.PI / 2 }));
          add(mesh(G(new T.CylinderGeometry(0.1, 0.1, 4.2, 6)), wood, 0, 2.3, 0, { rz: Math.PI / 2 }));
          add(mesh(G(new T.CylinderGeometry(0.32, 0.26, 0.5, 10)), M('#7a5a3a'), 0.7, 1.3, 0.9));
          break; }
        case 'bench':
        case 'house': {
          const isTav = l.id === 'tavern', thatch = l.kind === 'bench';
          const back = -(l.r * 0.55 + (thatch ? 2.6 : 2.8));
          const hw = 8, hd = thatch ? 4.4 : 5, hh = thatch ? 2.8 : 3.4;
          const wall = M(thatch ? '#c9b48a' : isTav ? '#d8c7a8' : '#b8b0a2');
          add(mesh(G(new T.BoxGeometry(hw, hh, hd)), wall, 0, hh / 2, back));
          const beam = M('#5a4026');
          if (!thatch) for (const bx of [-hw / 2, hw / 2]) add(mesh(G(new T.BoxGeometry(0.25, hh, 0.25)), beam, bx, hh / 2, back + hd / 2));
          add(mesh(prism(hd + 0.9, thatch ? 2.6 : 2.2, hw + 0.8), M(thatch ? '#b89a5a' : isTav ? '#9a4a32' : '#6f6f78', { flatShading: true }), 0, hh, back, { ry: Math.PI / 2 }));
          add(mesh(G(new T.BoxGeometry(1.1, 1.9, 0.12)), M('#4a3020'), 0, 0.95, back + hd / 2 + 0.03));
          const winM = M('#2a2420', { emissive: '#ffb050', emissiveIntensity: 0 });
          const winG = G(new T.PlaneGeometry(0.9, 0.8));
          for (const wx of [-2.5, 2.5]) add(mesh(winG, winM, wx, 1.7, back + hd / 2 + 0.02, { noCast: true }));
          fireLights.push({ win: winM });
          if (!thatch) add(mesh(G(new T.BoxGeometry(0.8, 1.6, 0.8)), M('#5a5248'), 2, hh + 1.6, back - 0.6));
          const light = new T.PointLight(0xffb060, 0, 12, 2); light.position.set(0, 1.6, back + hd / 2 + 1.2); add(light);
          fireLights.push({ light, base: 0, night: 18, house: true }); light.userData.level = 0;
          if (thatch) {
            add(mesh(G(new T.BoxGeometry(5.6, 0.18, 0.8)), M('#8a6440'), 0, 0.55, back + hd / 2 + 1.2));
            const leg = G(new T.BoxGeometry(0.15, 0.5, 0.6));
            add(mesh(leg, darkWood, -2.5, 0.25, back + hd / 2 + 1.2)); add(mesh(leg, darkWood, 2.5, 0.25, back + hd / 2 + 1.2));
            const pot = G(new T.SphereGeometry(0.45, 10, 8));
            for (let i = 0; i < 3; i++) add(mesh(pot, M('#9a6a44'), -1.2 + i * 1.2, 0.4, 0.5, { sv: [1, 1.2, 1] }));
          } else if (isTav) {
            const barrel = G(new T.CylinderGeometry(0.75, 0.75, 1.4, 14));
            add(mesh(barrel, M('#7a5432'), -1.7, 0.7, 0.4)); add(mesh(barrel, M('#7a5432'), 1.7, 0.7, 0.4));
            const sign = textSprite('酒', { size: 0.035, bg: 'rgba(201,162,74,.9)', fg: '#2a1a0a', circle: true }); sign.material.sizeAttenuation = true; sign.scale.set(1.2, 1.2, 1); sign.position.set(-hw / 2 - 0.7, 2.6, back + hd / 2); add(sign);
          } else {
            const crate = G(new T.BoxGeometry(1, 1, 1));
            for (let i = 0; i < 3; i++) add(mesh(crate, M('#8a7452'), -2 + i * 1.3, 0.5, 0.6, { ry: i * 0.3 }));
            add(mesh(G(new T.BoxGeometry(1.8, 1.0, 0.9)), M('#4a4440', { metalness: 0.4, roughness: 0.5 }), 0.2, 0.5, -1.4));
          }
          break; }
        case 'market': {
          const cols = [['#b0402e', '#ece2c8'], ['#2e5a8a', '#ece2c8'], ['#c9962a', '#ece2c8']];
          const postG = G(new T.CylinderGeometry(0.08, 0.08, 2.6, 6)), table = G(new T.BoxGeometry(2.6, 0.15, 1.5));
          for (let i = 0; i < 3; i++) {
            const a = -0.9 + i * 0.9, sx = Math.sin(a) * 3.4, sz = -Math.cos(a) * 2.4;
            const st = new T.Group(); st.position.set(sx, 0, sz); st.rotation.y = -a; add(st);
            for (const [px, pz] of [[-1.3, -0.75], [1.3, -0.75], [-1.3, 0.75], [1.3, 0.75]]) st.add(mesh(postG, darkWood, px, 1.3, pz));
            st.add(mesh(table, M('#8a6a44'), 0, 0.9, 0.2));
            const tex = stripeTex(cols[i][0], cols[i][1]);
            const aw = mesh(G(new T.PlaneGeometry(3.0, 2.0)), new T.MeshStandardMaterial({ map: tex, side: T.DoubleSide, roughness: 0.9 }), 0, 2.6, 0.1, { rx: -Math.PI / 2 + 0.25 });
            mats.set('aw' + i, aw.material); st.add(aw);
            const fruit = G(new T.SphereGeometry(0.15, 6, 5));
            for (let j = 0; j < 9; j++) st.add(mesh(fruit, M(['#c0402a', '#d9a83a', '#6a8a3a'][j % 3]), -0.9 + (j % 5) * 0.45, 1.08, 0.0 + Math.floor(j / 5) * 0.4, { noCast: true }));
          }
          break; }
        case 'temple': {
          const w = l.r * 1.7, d = l.r * 0.95;
          ['#a39c8b', '#b6af9e', '#cac3b2'].forEach((cc, i) => add(mesh(G(new T.BoxGeometry(w - i * 1.8, 0.45, d - i * 1.2)), M(cc), 0, 0.22 + i * 0.45, -i * 0.3)));
          const colG = G(new T.CylinderGeometry(0.42, 0.48, 4.6, 14));
          const backZ = -d / 2 + 1.0;
          for (let i = 0; i < 6; i++) add(mesh(colG, M('#e6dfce'), -w / 2 + 2.4 + i * (w - 4.8) / 5, 1.35 + 2.3, backZ));
          add(mesh(G(new T.BoxGeometry(w - 3.2, 0.6, 1.4)), M('#ddd5c2'), 0, 1.35 + 4.9, backZ));
          add(mesh(prism(1.6, 1.3, w - 3.2), M('#d0c8b4'), 0, 1.35 + 5.2, backZ, { ry: Math.PI / 2, sv: [1, 1, 1] }));
          // 祭壇の水晶（灯りを持つ）
          const cr = new T.Mesh(G(new T.OctahedronGeometry(0.6, 0)), M('#8fd8ff', { emissive: '#5ab8ff', emissiveIntensity: 1.2, roughness: 0.2 }));
          cr.position.set(0, 1.35 + 1.4, backZ + 0.2); cr.scale.set(1, 1.6, 1); add(cr);
          animated.push(t => { cr.rotation.y = t * 0.6; cr.position.y = 1.35 + 1.4 + Math.sin(t * 1.5) * 0.12; });
          const light = new T.PointLight(0x8fd0ff, 6, 16, 2); light.position.copy(cr.position); add(light);
          fireLights.push({ light, base: 6, night: 30, house: true }); light.userData.level = 6;
          break; }
        default:
          add(mesh(G(new T.BoxGeometry(5, 3, 4)), M('#8a7a5a'), 0, 1.5, 0));
      }
      // 席の印（「席と欲求」を押したときだけ見える）
      const ringG = G(new T.RingGeometry(0.35, 0.55, 16));
      l.seatList.forEach(st => {
        const m = new T.Mesh(ringG, new T.MeshBasicMaterial({ color: 0xf1e9da, transparent: true, opacity: 0.85, depthTest: false }));
        m.rotation.x = -Math.PI / 2; m.renderOrder = 5;
        const p = toV(st.x, st.y, 0.12); m.position.copy(p); m.visible = false; world.add(m);
        seatMarks.push({ m, st });
      });
      const label = textSprite(l.label); label.position.copy(toV(l.x, l.y + l.r * 0.65, 0)).add(new T.Vector3(0, 4.2, 0));
      world.add(label); labels.push({ sp: label, l });
      world.add(g);
    }

    // 煙：少数の半透明の球を上へ流して使い回す
    function addSmoke(parent, x, y, z, k) {
      const geo = G(new T.SphereGeometry(0.5, 8, 6)), puffs = [];
      for (let i = 0; i < 7; i++) {
        const m = new T.Mesh(geo, new T.MeshStandardMaterial({ color: 0xd8d2c8, transparent: true, opacity: 0, depthWrite: false, roughness: 1 }));
        mats.set('smoke' + Math.random(), m.material);
        m.userData.p = i / 7; parent.add(m); puffs.push(m);
      }
      animated.push((t, dt) => puffs.forEach(m => {
        m.userData.p = (m.userData.p + dt * 0.12) % 1; const p = m.userData.p;
        m.position.set(x + p * 2.4 * k + Math.sin(p * 9 + t) * 0.2, y + p * 7 * k, z + p * 0.8);
        m.scale.setScalar((0.6 + p * 2.6) * k); m.material.opacity = 0.32 * Math.sin(p * Math.PI) * (p < 0.08 ? p / 0.08 : 1);
      }));
    }

    function buildTree(x, y, size) {
      const g = new T.Group(); g.position.copy(toV(x, y));
      const h = 3 + size * 1.6;
      g.add(mesh(treeTrunk, M('#5a4026'), 0, h * 0.3, 0, { sv: [size * 0.7, h * 0.6, size * 0.7] }));
      const greens = ['#3b5a29', '#47672f', '#52703a'];
      for (let i = 0; i < 3; i++) {
        const a = i * 2.1 + x, r = size * 0.45;
        g.add(mesh(treeLeaf, M(greens[i], { flatShading: true }), Math.cos(a) * r, h * (0.62 + i * 0.13), Math.sin(a) * r, { s: size * (1.05 - i * 0.18), ry: a }));
      }
      world.add(g);
    }
    let treeTrunk, treeLeaf, outerTrees = [];

    /* ---------- 人物 ---------- */
    const P = 2.2;   // 背の高さ（世界の単位）
    let people = new Map();
    function buildPerson(n, era) {
      const jobs = api.JOBS[era], J = api.JOB_LOOK[jobs[n.id % jobs.length]];
      const skin = api.SKIN[n.id % 5], hair = api.HAIR[(n.id * 3) % 5];
      const root = new T.Group(), body = new T.Group(); root.add(body);
      const cloth = M(J.cloth), legM = M(J.robe ? api.shade(J.cloth, 0.8) : J.legs || api.shade(J.cloth, 0.6)), skinM = M(skin), trimM = M(J.trim);
      const hipY = P * 0.49, shY = P * 0.8;
      const mk = (geo, mat, x, y, z, o) => mesh(geo, mat, x, y, z, o);
      // 脚：股関節 → 膝 → 足
      const legs = [-1, 1].map(side => {
        const hip = new T.Group(); hip.position.set(side * P * 0.045, hipY, 0); body.add(hip);
        hip.add(mk(G(new T.CylinderGeometry(P * 0.034, P * 0.028, P * 0.25, 8)), legM, 0, -P * 0.125, 0));
        const knee = new T.Group(); knee.position.y = -P * 0.25; hip.add(knee);
        knee.add(mk(G(new T.CylinderGeometry(P * 0.027, P * 0.022, P * 0.23, 8)), legM, 0, -P * 0.115, 0));
        knee.add(mk(G(new T.BoxGeometry(P * 0.05, P * 0.03, P * 0.1)), M('#2e241c'), 0, -P * 0.235, P * 0.025));
        return { hip, knee };
      });
      // 胴
      if (J.robe) {
        const robe = mk(G(new T.CylinderGeometry(P * 0.085, P * 0.15, P * 0.74, 12)), cloth, 0, P * 0.43, 0);
        body.add(robe);
        body.add(mk(G(new T.CylinderGeometry(P * 0.152, P * 0.152, P * 0.03, 12)), trimM, 0, P * 0.075, 0));
        body.add(mk(G(new T.BoxGeometry(P * 0.02, P * 0.6, P * 0.01)), trimM, 0, P * 0.45, P * 0.112, { rx: 0.09 }));
        legs.forEach(L => L.hip.visible = true);
        body.userData.robe = robe;
      } else {
        body.add(mk(G(new T.CylinderGeometry(P * 0.095, P * 0.075, P * 0.33, 10)), cloth, 0, P * 0.635, 0, { sv: [1, 1, 0.7] }));
        body.add(mk(G(new T.CylinderGeometry(P * 0.078, P * 0.078, P * 0.04, 10)), trimM, 0, hipY + P * 0.02, 0, { sv: [1, 1, 0.75] }));
      }
      // 腕：肩 → 肘 → 手
      const arms = [-1, 1].map(side => {
        const sh = new T.Group(); sh.position.set(side * P * 0.115, shY - P * 0.02, 0); body.add(sh);
        sh.add(mk(G(new T.CylinderGeometry(P * 0.025, P * 0.022, P * 0.15, 7)), cloth, 0, -P * 0.075, 0));
        const el = new T.Group(); el.position.y = -P * 0.15; sh.add(el);
        el.add(mk(G(new T.CylinderGeometry(P * 0.021, P * 0.018, P * 0.14, 7)), side < 0 && !J.robe ? cloth : cloth, 0, -P * 0.07, 0));
        const hand = mk(G(new T.SphereGeometry(P * 0.024, 8, 6)), skinM, 0, -P * 0.15, 0); el.add(hand);
        return { sh, el, hand };
      });
      // 頭
      const head = new T.Group(); head.position.y = P * 0.905; body.add(head);
      head.add(mk(G(new T.CylinderGeometry(P * 0.024, P * 0.028, P * 0.07, 8)), skinM, 0, -P * 0.07, 0));
      head.add(mk(G(new T.SphereGeometry(P * 0.062, 14, 10)), skinM, 0, 0, 0, { sv: [0.92, 1.1, 1] }));
      const eye = G(new T.SphereGeometry(P * 0.008, 6, 4)), eyeM = M('#1a1410');
      head.add(mk(eye, eyeM, -P * 0.022, P * 0.005, P * 0.055, { noCast: true })); head.add(mk(eye, eyeM, P * 0.022, P * 0.005, P * 0.055, { noCast: true }));
      const cap = (r, color, tilt, len) => mk(G(new T.SphereGeometry(r, 14, 8, 0, Math.PI * 2, 0, Math.PI * (len || 0.55))), M(color), 0, 0, 0, { rx: tilt || -0.35 });
      if (J.hood) {
        head.add(cap(P * 0.082, J.hood, -0.55, 0.62));
        body.add(mk(G(new T.ConeGeometry(P * 0.13, P * 0.12, 12)), M(J.hood), 0, shY + P * 0.02, 0));
      } else {
        head.add(cap(P * 0.066, hair, -0.4, 0.5));
        head.add(mk(G(new T.SphereGeometry(P * 0.055, 10, 8)), M(hair), 0, -P * 0.01, -P * 0.022, { sv: [1.05, 0.95, 0.8] }));
      }
      if (J.fur) { head.add(cap(P * 0.074, '#8f6a44', -0.2, 0.45)); body.add(mk(G(new T.TorusGeometry(P * 0.09, P * 0.03, 6, 14)), M('#c9b08a'), 0, shY, 0, { rx: Math.PI / 2 })); }
      if (J.band) head.add(mk(G(new T.CylinderGeometry(P * 0.064, P * 0.064, P * 0.022, 14, 1, true)), M(J.band, { side: T.DoubleSide }), 0, P * 0.025, 0));
      if (J.kerchief) head.add(cap(P * 0.07, J.kerchief, -0.25, 0.5));
      if (J.feathers) ['#c0402a', '#e0c070', '#3a6a8a'].forEach((c, i) => head.add(mk(G(new T.BoxGeometry(P * 0.012, P * 0.11, P * 0.03)), M(c), (i - 1) * P * 0.03, P * 0.1, -P * 0.03, { rz: (i - 1) * 0.35, rx: -0.3 })));
      if (J.strawHat) { head.add(mk(G(new T.CylinderGeometry(P * 0.16, P * 0.16, P * 0.012, 18)), M('#d9c27a'), 0, P * 0.045, 0)); head.add(mk(G(new T.ConeGeometry(P * 0.075, P * 0.08, 12)), M('#c9b06a'), 0, P * 0.085, 0)); }
      if (J.cap) { head.add(cap(P * 0.07, J.cap, -0.15, 0.45)); head.add(mk(G(new T.BoxGeometry(P * 0.08, P * 0.01, P * 0.06)), M(api.shade(J.cap, 0.8)), 0, P * 0.03, P * 0.075)); }
      if (J.circlet) { head.add(mk(G(new T.TorusGeometry(P * 0.064, P * 0.007, 6, 20)), M('#d9b44a', { metalness: 0.7, roughness: 0.35 }), 0, P * 0.03, 0, { rx: Math.PI / 2 + 0.15 })); head.add(mk(G(new T.OctahedronGeometry(P * 0.012)), M('#7fd0ff', { emissive: '#4ab0ff', emissiveIntensity: 1 }), 0, P * 0.035, P * 0.066)); }
      if (J.helmet) { head.add(cap(P * 0.075, '#9aa0aa', 0, 0.5)); head.add(mk(G(new T.TorusGeometry(P * 0.075, P * 0.008, 6, 20)), M('#c0c4cc', { metalness: 0.6, roughness: 0.4 }), 0, P * 0.0, 0, { rx: Math.PI / 2 })); body.add(mk(G(new T.BoxGeometry(P * 0.2, P * 0.18, P * 0.13)), M('#a0a4ae', { metalness: 0.55, roughness: 0.45 }), 0, P * 0.68, 0)); }
      if (J.bag) body.add(mk(G(new T.BoxGeometry(P * 0.07, P * 0.09, P * 0.04)), M('#8a6a3a'), -P * 0.1, hipY, P * 0.02));
      // マント
      let cape = null;
      if (J.cape) {
        cape = new T.Group(); cape.position.set(0, shY + P * 0.01, -P * 0.06); body.add(cape);
        const cg = G(new T.PlaneGeometry(P * 0.24, P * 0.62)); cg.translate(0, -P * 0.31, 0);
        cape.add(mk(cg, M(J.cape, { side: T.DoubleSide }), 0, 0, 0));
      }
      // 手に持つ道具（左手）・背負う剣
      const tool = new T.Group(); arms[0].hand.add(tool);
      const stick = (len, color) => tool.add(mk(G(new T.CylinderGeometry(P * 0.012, P * 0.012, len, 6)), M(color), 0, len * 0.35, 0));
      switch (J.weapon) {
        case 'spear': stick(P * 1.15, '#6a4a2c'); tool.add(mk(G(new T.ConeGeometry(P * 0.025, P * 0.12, 6)), M('#c8ccd2', { metalness: 0.7, roughness: 0.3 }), 0, P * 0.81, 0)); break;
        case 'staff': stick(P * 0.95, '#7a5a3a'); tool.add(mk(G(new T.SphereGeometry(P * 0.028, 8, 6)), M('#c9b48a'), 0, P * 0.64, 0)); break;
        case 'crystalStaff': {
          stick(P * 0.95, '#5a4026');
          const cr = mk(G(new T.OctahedronGeometry(P * 0.045)), M('#7fd0ff', { emissive: '#4ab0ff', emissiveIntensity: 1.4, roughness: 0.2 }), 0, P * 0.7, 0, { sv: [1, 1.5, 1] }); tool.add(cr);
          animated.push(t => { cr.rotation.y = t * 1.5; }); break; }
        case 'rod': stick(P * 0.6, '#c9a24a'); tool.add(mk(G(new T.SphereGeometry(P * 0.03, 8, 6)), M('#f2e6b0', { emissive: '#f2e6b0', emissiveIntensity: 0.4 }), 0, P * 0.42, 0)); break;
        case 'totem': stick(P * 0.95, '#6a4a2c'); ['#b04a3a', '#e0d0a0', '#3a6a8a'].forEach((c, i) => tool.add(mk(G(new T.BoxGeometry(P * 0.01, P * 0.08, P * 0.025)), M(c), (i - 1) * P * 0.02, P * 0.62, 0, { rz: (i - 1) * 0.5 }))); break;
        case 'hoe': stick(P * 0.95, '#7a5a3a'); tool.add(mk(G(new T.BoxGeometry(P * 0.02, P * 0.03, P * 0.12)), M('#8a8e94', { metalness: 0.5 }), 0, P * 0.66, P * 0.05)); break;
        case 'sword': {
          const sw = new T.Group(); sw.position.set(0, P * 0.65, -P * 0.07); sw.rotation.z = 0.6; body.add(sw);
          sw.add(mk(G(new T.BoxGeometry(P * 0.035, P * 0.5, P * 0.012)), M('#c8ccd4', { metalness: 0.8, roughness: 0.25 }), 0, P * 0.1, 0));
          sw.add(mk(G(new T.BoxGeometry(P * 0.12, P * 0.022, P * 0.03)), M('#c9a24a', { metalness: 0.6, roughness: 0.4 }), 0, -P * 0.15, 0));
          sw.add(mk(G(new T.CylinderGeometry(P * 0.012, P * 0.012, P * 0.09, 6)), M('#3a2a1a'), 0, -P * 0.21, 0));
          break; }
      }
      tool.rotation.x = -Math.PI / 2 + 0.05;   // 手から上へ立てる（腕の向きに合わせて後で補正）
      // 持ち物（右手）と足もとの荷
      const itemM = new T.MeshStandardMaterial({ color: 0x999999, roughness: 0.7 });
      mats.set('item' + n.id, itemM);
      const item = mk(G(new T.SphereGeometry(P * 0.045, 10, 8)), itemM, 0, -P * 0.17, P * 0.03); arms[1].el.add(item);
      const extras = [0, 1].map(i => { const m2 = new T.MeshStandardMaterial({ color: 0x999999 }); mats.set('ex' + n.id + i, m2); const e = mk(G(new T.SphereGeometry(P * 0.04, 8, 6)), m2, -P * 0.18 - i * P * 0.09, P * 0.04, -P * 0.05); root.add(e); return e; });
      // 選択の輪と吹き出し
      const ring = new T.Mesh(selRingG, selRingM); ring.rotation.x = -Math.PI / 2; ring.position.y = 0.06; ring.visible = false; root.add(ring);
      const bubble = textSprite('…', { bg: 'rgba(250,246,236,.95)', fg: '#5a3a22', size: 0.04 }); bubble.position.y = P * 1.25; bubble.visible = false; root.add(bubble);
      root.traverse(o => { if (o.isMesh && o !== ring) { o.castShadow = true; } });
      world.add(root);
      return { root, body, legs, arms, head, cape, tool, item, extras, ring, bubble, J, dir: Math.random() * 6, phase: Math.random() * 6 };
    }
    let selRingG, selRingM;

    /* ---------- 道順の線と番号 ---------- */
    let routeNow, routeNext, routeMarks = [];
    function makeRoute() {
      const mk = (color, dashed) => {
        const geo = new T.BufferGeometry(); geo.setAttribute('position', new T.BufferAttribute(new Float32Array(3 * 8), 3));
        const mat = dashed ? new T.LineDashedMaterial({ color, dashSize: 1.2, gapSize: 0.9, depthTest: false, transparent: true }) : new T.LineBasicMaterial({ color, depthTest: false, transparent: true });
        const line = new T.Line(geo, mat); line.renderOrder = 8; line.frustumCulled = false; world.add(line); return line;
      };
      routeNow = mk(0x4aa3ff, false); routeNext = mk(0x9a6ad8, true);
      routeMarks = [1, 2, 3, 4].map(i => { const sp = textSprite(String(i), { circle: true, bg: '#9a6ad8', fg: '#0b1320', size: 0.04, bold: true }); sp.visible = false; world.add(sp); return sp; });
      routeMarks.forEach((sp, i) => sp.userData.now = textSprite(String(i + 1), { circle: true, bg: '#4aa3ff', fg: '#0b1320', size: 0.04, bold: true }));
      routeMarks.forEach(sp => { sp.userData.now.visible = false; world.add(sp.userData.now); });
    }
    function setLine(line, pts) {
      const a = line.geometry.attributes.position;
      for (let i = 0; i < 8; i++) { const p = pts[Math.min(i, pts.length - 1)] || new T.Vector3(); a.setXYZ(i, p.x, p.y, p.z); }
      a.needsUpdate = true; line.geometry.setDrawRange(0, pts.length); line.visible = pts.length > 1;
      if (line.material.isLineDashedMaterial) line.computeLineDistances();
    }

    /* ---------- 組み立て ---------- */
    let labels = [];
    function disposeAll() {
      world.traverse(o => { if (o.userData && o.userData.dispose) o.userData.dispose(); });
      world.clear();
      geos.forEach(g => g.dispose()); geos = [];
      mats.forEach(m => { if (m.map) m.map.dispose(); m.dispose(); }); mats = new Map();
      if (groundTex) { groundTex.dispose(); groundTex = null; }
      animated.length = 0; fireLights = []; seatMarks = []; labels = []; people = new Map();
    }
    function build() {
      disposeAll();
      sim = api.getSim(); W = sim.w; H = sim.h;
      const era = api.ERAS.indexOf(sim.era), L = api.LOOK[era];
      const river = sim.locs.find(l => l.id === 'river');
      riverPts = river ? api.riverPath(river) : null;
      hgtFn = makeHeight(L);
      // 地面：2D と同じ描き方の絵を貼り、起伏を付ける
      const cv = document.createElement('canvas');
      const placed = api.paintGround(cv, 18, true);
      groundTex = new T.CanvasTexture(cv); groundTex.colorSpace = T.SRGBColorSpace;
      groundTex.anisotropy = renderer.capabilities.getMaxAnisotropy();
      const seg = 2;
      const gg = G(new T.PlaneGeometry(W, H, W * seg, H * seg)); gg.rotateX(-Math.PI / 2);
      const pos = gg.attributes.position;
      for (let i = 0; i < pos.count; i++) pos.setY(i, hgtFn(pos.getX(i) + W / 2, pos.getZ(i) + H / 2));
      gg.computeVertexNormals();
      const ground = new T.Mesh(gg, new T.MeshStandardMaterial({ map: groundTex, roughness: 0.95 }));
      mats.set('ground', ground.material); ground.receiveShadow = true; world.add(ground);
      // 地図の外：同じ色の野と、遠くの山並み
      // 地図の外：地図の縁から続く野と丘（内側は地図の下へ沈めて隠す）
      const SP = 5, EXT = 300;
      const sg = G(new T.PlaneGeometry(W + EXT * 2, H + EXT * 2, (W + EXT * 2) / SP, (H + EXT * 2) / SP)); sg.rotateX(-Math.PI / 2);
      const sp2 = sg.attributes.position;
      for (let i = 0; i < sp2.count; i++) {
        const x = sp2.getX(i) + W / 2, y = sp2.getZ(i) + H / 2;
        const inside = x > 0.01 && x < W - 0.01 && y > 0.01 && y < H - 0.01;
        if (inside) { sp2.setY(i, -6); continue; }
        const cx = Math.max(0, Math.min(W, x)), cy = Math.max(0, Math.min(H, y)), out = Math.hypot(x - cx, y - cy);
        const hills = (0.5 + 0.35 * Math.sin(x * 0.045) * Math.cos(y * 0.05) + 0.15 * Math.sin(x * 0.11 + 1)) * Math.min(1, out / 40) * (L.paved ? 3 : 9);
        sp2.setY(i, hgtFn(cx, cy) - 0.08 + out * (L.paved ? 0.03 : 0.07) + hills);
      }
      sg.computeVertexNormals();
      const skirt = new T.Mesh(sg, M(api.shade(L.ground, L.paved ? 0.8 : 0.9), { flatShading: false }));
      skirt.receiveShadow = true; world.add(skirt);
      // 外の野にも木を散らす
      const outRnd = mulberry(77 + era);
      for (let i = 0; i < (L.paved ? 60 : 160); i++) {
        const x = -60 + outRnd() * (W + 120), y = -60 + outRnd() * (H + 120);
        if (x > -3 && x < W + 3 && y > -3 && y < H + 3) continue;
        const cx = Math.max(0, Math.min(W, x)), cy = Math.max(0, Math.min(H, y));
        outerTrees.push([x, y]);
      }
      const rimRnd = mulberry(5 + era);
      const mtn = G(new T.ConeGeometry(1, 1, 7, 1));
      for (let i = 0; i < 46; i++) {
        const a = i / 46 * Math.PI * 2 + rimRnd() * 0.1, r = 270 + rimRnd() * 140, h = 50 + rimRnd() * 90;
        const m = mesh(mtn, M(i % 3 ? '#61707c' : '#71808a', { flatShading: true }), Math.cos(a) * r, h / 2 + 1, Math.sin(a) * r, { sv: [h * (0.9 + rimRnd() * 0.6), h, h * (0.9 + rimRnd() * 0.6)], ry: rimRnd() * 3, noCast: true });
        m.receiveShadow = false; world.add(m);
        if (h > 100) { const cap = mesh(mtn, M('#e8ecef', { flatShading: true }), m.position.x, h * 0.82 + 1, m.position.z, { sv: [m.scale.x * 0.36, h * 0.36, m.scale.z * 0.36], ry: m.rotation.y, noCast: true }); world.add(cap); }
      }
      // 川の水面
      if (riverPts) {
        const n = riverPts.length, posA = new Float32Array(n * 2 * 3), idx = [];
        for (let i = 0; i < n; i++) {
          const [x, y] = riverPts[i], [x2, y2] = riverPts[Math.min(n - 1, i + 1)], [x0, y0] = riverPts[Math.max(0, i - 1)];
          let nx = -(y2 - y0), ny = x2 - x0; const nl = Math.hypot(nx, ny) || 1; nx /= nl; ny /= nl;
          const h = hgtFn.base(x, y) - 0.35;
          [[x + nx * 3.1, y + ny * 3.1], [x - nx * 3.1, y - ny * 3.1]].forEach(([px, py], j) => { posA.set([px - W / 2, h, py - H / 2], (i * 2 + j) * 3); });
          if (i < n - 1) idx.push(i * 2, i * 2 + 1, i * 2 + 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
        }
        const wg = G(new T.BufferGeometry()); wg.setAttribute('position', new T.BufferAttribute(posA, 3)); wg.setIndex(idx); wg.computeVertexNormals();
        const water = new T.Mesh(wg, new T.MeshStandardMaterial({ color: 0x3f7590, roughness: 0.12, metalness: 0.25, transparent: true, opacity: 0.88, side: T.DoubleSide }));
        mats.set('water', water.material); water.receiveShadow = true; world.add(water);
        animated.push(t => { water.material.color.setHSL(0.55, 0.4, 0.38 + Math.sin(t * 0.8) * 0.02); });
      }
      treeTrunk = G(new T.CylinderGeometry(0.22, 0.32, 1, 7));
      treeLeaf = G(new T.IcosahedronGeometry(1.3, 0));
      placed.trees.forEach(tr => buildTree(tr.x, tr.y, tr.size));
      outerTrees.forEach(([x, y], i) => {
        const g = new T.Group(), sz = 1.8 + (i * 7 % 10) / 6;
        g.add(mesh(treeTrunk, M('#5a4026'), 0, (3 + sz * 1.6) * 0.3, 0, { sv: [sz * 0.7, (3 + sz * 1.6) * 0.6, sz * 0.7], noCast: true }));
        g.add(mesh(treeLeaf, M(i % 2 ? '#3b5a29' : '#47672f', { flatShading: true }), 0, (3 + sz * 1.6) * 0.75, 0, { s: sz * 1.1, noCast: true }));
        g.position.copy(skirtPoint(x, y)); world.add(g);
      });
      outerTrees = [];
      sim.locs.forEach(l => buildLocation(l, L));
      selRingG = G(new T.RingGeometry(0.55, 0.75, 24)); selRingM = new T.MeshBasicMaterial({ color: 0x4aa3ff, transparent: true, opacity: 0.95, depthTest: false });
      mats.set('sel', selRingM);
      sim.npcs.forEach(n => people.set(n, buildPerson(n, era)));
      makeRoute();
      // 影を落とす範囲を地図に合わせる
      const sc = sun.shadow.camera, half = Math.max(W, H) * 0.62;
      sc.left = -half; sc.right = half; sc.top = half; sc.bottom = -half; sc.near = 1; sc.far = 400; sc.updateProjectionMatrix();
      orbit.target.set(0, 0, 0); orbit.dist = fitDist(); orbit.yaw = 0.55; orbit.pitch = 0.9;
      built = true;
    }
    function skirtPoint(x, y) {
      const L = api.LOOK[api.ERAS.indexOf(sim.era)];
      const cx = Math.max(0, Math.min(W, x)), cy = Math.max(0, Math.min(H, y)), out = Math.hypot(x - cx, y - cy);
      const hills = (0.5 + 0.35 * Math.sin(x * 0.045) * Math.cos(y * 0.05) + 0.15 * Math.sin(x * 0.11 + 1)) * Math.min(1, out / 40) * (L.paved ? 3 : 9);
      return new T.Vector3(x - W / 2, hgtFn(cx, cy) - 0.08 + out * (L.paved ? 0.03 : 0.07) + hills - 0.3, y - H / 2);
    }
    function mulberry(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }

    /* ---------- カメラ ---------- */
    const orbit = { target: new T.Vector3(), yaw: 0.55, pitch: 0.78, dist: 100, userUntil: 0 };
    let built = false, followPrev = false;
    function fitDist() { return Math.max(W, H) * (camera.aspect < 1 ? 1.2 : 0.85); }
    function clampOrbit() {
      orbit.pitch = Math.max(0.12, Math.min(1.45, orbit.pitch));
      orbit.dist = Math.max(4, Math.min(fitDist() * 1.6, orbit.dist));
      orbit.target.x = Math.max(-W / 2, Math.min(W / 2, orbit.target.x));
      orbit.target.z = Math.max(-H / 2, Math.min(H / 2, orbit.target.z));
    }
    function placeCamera() {
      clampOrbit();
      const t = orbit.target, cp = Math.cos(orbit.pitch);
      camera.position.set(t.x + Math.sin(orbit.yaw) * cp * orbit.dist, t.y + Math.sin(orbit.pitch) * orbit.dist, t.z + Math.cos(orbit.yaw) * cp * orbit.dist);
      // 地面にめり込まない
      const gy = hgtFn ? hgtFn(camera.position.x + W / 2, camera.position.z + H / 2) : 0;
      if (camera.position.y < gy + 1.2) camera.position.y = gy + 1.2;
      camera.lookAt(t);
    }
    function zoom(f) { orbit.dist /= f; orbit.userUntil = performance.now() + 2500; clampOrbit(); }
    function resetView() { orbit.target.set(0, 0, 0); orbit.dist = fitDist(); orbit.pitch = 0.9; orbit.yaw = 0.55; }

    // 操作：1本指（左ボタン）で回り込み、2本指で拡大・移動、右ボタンかShiftで移動。動かさずに離せばタップ
    const pts = new Map(); let drag = null;
    const rel = e => { const r = el.getBoundingClientRect(); return [e.clientX - r.left, e.clientY - r.top]; };
    el.addEventListener('contextmenu', e => e.preventDefault());
    el.addEventListener('pointerdown', e => {
      el.setPointerCapture(e.pointerId); pts.set(e.pointerId, rel(e));
      const [x, y] = rel(e);
      drag = { x, y, moved: pts.size > 1, pan: e.button === 2 || e.shiftKey, dist: 0, mid: null };
      if (pts.size === 2) { const [a, b] = [...pts.values()]; drag.dist = Math.hypot(a[0] - b[0], a[1] - b[1]); drag.mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]; }
    });
    function panBy(dx, dy) {
      const k = orbit.dist / el.clientHeight * 0.9;
      const fx = -Math.sin(orbit.yaw), fz = -Math.cos(orbit.yaw), rx = Math.cos(orbit.yaw), rz = -Math.sin(orbit.yaw);
      orbit.target.x -= (rx * dx - fx * dy) * k; orbit.target.z -= (rz * dx - fz * dy) * k;
      api.setFollow(false);
    }
    el.addEventListener('pointermove', e => {
      if (!pts.has(e.pointerId) || !drag) return;
      const prev = pts.get(e.pointerId), p = rel(e); pts.set(e.pointerId, p);
      if (pts.size === 2) {
        const [a, b] = [...pts.values()], d = Math.hypot(a[0] - b[0], a[1] - b[1]), mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
        if (drag.dist > 0) zoom(d / drag.dist);
        if (drag.mid) panBy(mid[0] - drag.mid[0], mid[1] - drag.mid[1]);
        drag.dist = d; drag.mid = mid; drag.moved = true; return;
      }
      if (Math.hypot(p[0] - drag.x, p[1] - drag.y) > 8) drag.moved = true;
      if (!drag.moved) return;
      const dx = p[0] - prev[0], dy = p[1] - prev[1];
      if (drag.pan) panBy(dx, dy);
      else { orbit.yaw -= dx * 0.006; orbit.pitch += dy * 0.005; orbit.userUntil = performance.now() + 3000; }
    });
    const up = e => {
      if (!pts.has(e.pointerId)) return; pts.delete(e.pointerId);
      if (pts.size || !drag) return;
      const tap = !drag.moved; drag = null;
      if (!tap || e.type === 'pointercancel') return;
      const [px, py] = rel(e); let best = null, bd = 34;
      const v = new T.Vector3();
      for (const [n, p] of people) {
        v.copy(p.root.position); v.y += P * 0.55; v.project(camera);
        if (v.z > 1) continue;
        const sx = (v.x + 1) / 2 * el.clientWidth, sy = (1 - v.y) / 2 * el.clientHeight, d = Math.hypot(sx - px, sy - py);
        if (d < bd) { bd = d; best = n; }
      }
      api.select(best);
    };
    el.addEventListener('pointerup', up); el.addEventListener('pointercancel', up);
    el.addEventListener('wheel', e => { e.preventDefault(); zoom(Math.exp(-e.deltaY * 0.0015)); }, { passive: false });

    /* ---------- 毎フレーム ---------- */
    const tmp = new T.Vector3(), skyDay = new T.Color(0x6fa8dc), skyNight = new T.Color(0x0b1430), horDay = new T.Color(0xcfe2ee), horNight = new T.Color(0x1e2a48), fogC = new T.Color();
    let lastSkyKey = -1;
    const moonC = new T.Color(0x8ea2d8), sunC = new T.Color(0xfff1d6), nightSkyC = new T.Color(0x5868a0), daySkyC = new T.Color(0xcfe3ff),
      nightGroundC = new T.Color(0x2a2a30), dayGroundC = new T.Color(0x5a5236);
    function animatePerson(n, p, dt, t) {
      const sp = Math.hypot(n.vx, n.vy);
      const walking = sp > 0.3 && n.state !== 'use';
      const cur = sim.current(n), working = n.state === 'use' && cur && cur.work, seated = n.state === 'use' && !working;
      const l = n.loc;
      let want = p.dir;
      if (n.state === 'use' && l) want = Math.atan2(l.x - n.x, l.y - n.y);
      else if (sp > 0.2) want = Math.atan2(n.vx, n.vy);
      let d = ((want - p.dir + Math.PI * 3) % (Math.PI * 2)) - Math.PI; p.dir += d * Math.min(1, dt * 7);
      if (walking) p.phase += sp * dt * 2.3;
      const s = walking ? Math.sin(p.phase) : 0, ls = p.J.robe ? s * 0.4 : s;
      const gy = hgtFn(n.x, n.y);
      p.root.position.set(n.x - W / 2, gy, n.y - H / 2);
      p.root.rotation.y = p.dir;
      const bob = walking ? Math.abs(Math.cos(p.phase)) * 0.05 : 0;
      p.body.position.y = (seated ? -P * 0.22 : 0) + bob;
      // 脚
      p.legs.forEach((L, i) => {
        const sg = i ? 1 : -1;
        if (seated) { L.hip.rotation.x = -1.45; L.knee.rotation.x = 1.4; }
        else { const a = ls * 0.55 * sg; L.hip.rotation.x = -a; L.knee.rotation.x = 0.1 + Math.max(0, (sg * ls)) * 0.9; }
      });
      // 腕
      const talking = n.state === 'use' && l && l.speaker === n.id;
      p.arms.forEach((A, i) => {
        const sg = i ? 1 : -1;
        let a = -ls * 0.5 * sg, b = -0.25;
        if (working) { a = -0.4 - Math.abs(Math.sin(t * 5 + n.id + i)) * 1.0; b = -0.9; }
        else if (i === 0 && p.J.weapon && p.J.weapon !== 'sword') { a = -0.15; b = -1.1; }
        else if (i === 1 && n.items.length) { a = -0.3; b = -1.2; }
        else if (i === 1 && talking) { a = -0.5 - Math.sin(t * 4) * 0.3; b = -1.3; }
        A.sh.rotation.x = a; A.el.rotation.x = b; A.sh.rotation.z = sg * 0.06;
      });
      // 杖・槍は肘の角度に関わらず、ほぼ垂直に立てる
      const armA = p.arms[0].sh.rotation.x + p.arms[0].el.rotation.x;
      p.tool.rotation.x = -armA;
      if (p.cape) p.cape.rotation.x = 0.12 + (walking ? 0.25 + Math.sin(p.phase * 2) * 0.05 : Math.sin(t * 1.4 + n.id) * 0.03) + (seated ? 0.9 : 0);
      // 持ち物
      p.item.visible = n.items.length > 0;
      if (n.items.length) p.item.material.color.set(api.ITEM_COLORS[n.items[0]] || '#999');
      p.extras.forEach((e, i) => { const it = n.items[i + 1]; e.visible = !!it; if (it) e.material.color.set(api.ITEM_COLORS[it] || '#999'); });
      p.ring.visible = api.getSelected() === n;
      p.bubble.visible = !!talking;
      p.bubble.position.y = P * 1.2 + (seated ? -P * 0.22 : 0);
    }
    function render(dt) {
      if (!built) return;
      const t = sim.t, night = api.nightness() / 0.62;
      for (const f of animated) f(t, dt);
      // 光：太陽が回り、夜は灯りだけが残る
      const a = t / 120 * Math.PI * 2, day = 1 - night;
      sun.position.set(Math.cos(a * 0.5 + 0.6) * 90, 25 + 95 * day, 50 + Math.sin(a * 0.5) * 30);
      sun.target.position.set(0, 0, 0);
      // 夜は月明かり（青白く弱い光）に切り替える
      sun.intensity = 0.8 + 1.95 * day; sun.color.copy(moonC).lerp(sunC, day);
      hemi.intensity = 1.0 + 0.2 * day;
      hemi.color.copy(nightSkyC).lerp(daySkyC, day); hemi.groundColor.copy(nightGroundC).lerp(dayGroundC, day);
      for (const f of fireLights) {
        if (f.win) { f.win.emissiveIntensity = night * 1.6; continue; }
        f.light.userData.level = f.base + (f.night - f.base) * night;
        if (f.house) f.light.intensity = f.light.userData.level;
      }
      const key = Math.round(night * 40);
      if (key !== lastSkyKey) {
        lastSkyKey = key;
        const top = skyDay.clone().lerp(skyNight, night), hor = horDay.clone().lerp(horNight, night);
        paintSky(top, hor); fogC.copy(hor); scene.fog.color.copy(fogC);
      }
      // 人
      for (const [n, p] of people) animatePerson(n, p, dt, t);
      // 席の印
      const dbg = api.isDebug();
      for (const s of seatMarks) { s.m.visible = dbg; if (dbg) s.m.material.color.set(s.st.by === null ? 0xf1e9da : 0xe0603a); }
      // 道順
      const sel = api.getSelected();
      const showRoute = sel && sel.plan;
      routeMarks.forEach(sp => { sp.visible = false; sp.userData.now.visible = false; });
      if (showRoute) {
        const pnow = [toV(sel.x, sel.y, 0.4)], pnext = [];
        for (let i = sel.stepIdx; i < sel.plan.length; i++) {
          const l = sim.locs.find(o => o.id === sel.plan[i].at), v = toV(l.x, l.y, 0.4);
          if (i === sel.stepIdx) pnow.push(v); else { if (!pnext.length) pnext.push(pnow[1]); pnext.push(v); }
          const mk = i === sel.stepIdx ? routeMarks[i].userData.now : routeMarks[i];
          if (mk) { mk.visible = true; mk.position.copy(v).add(tmp.set(0, 6, 0)); }
        }
        setLine(routeNow, pnow); setLine(routeNext, pnext);
      } else { routeNow.visible = false; routeNext.visible = false; }
      // カメラ：「カメラで追う」では、観察している人の後ろへ回り込む
      const follow = api.getFollow() && sel;
      if (follow) {
        const p = people.get(sel);
        const k = Math.min(1, dt * 3 + 0.02);
        orbit.target.lerp(tmp.copy(p.root.position).add(new T.Vector3(0, P * 0.7, 0)), k);
        if (!followPrev) { orbit.dist = Math.min(orbit.dist, 14); orbit.pitch = 0.42; }
        if (performance.now() > orbit.userUntil && Math.hypot(sel.vx, sel.vy) > 0.3) {
          const behind = p.dir + Math.PI;
          let d = ((behind - orbit.yaw + Math.PI * 3) % (Math.PI * 2)) - Math.PI;
          orbit.yaw += d * Math.min(1, dt * 0.9);
        }
      } else orbit.target.y += (0 - orbit.target.y) * Math.min(1, dt * 2);
      followPrev = !!follow;
      placeCamera();
      // 影の範囲を注視点に寄せ、寄ったときは細かく
      const half = Math.min(Math.max(W, H) * 0.62, Math.max(18, orbit.dist * 0.9));
      const sc = sun.shadow.camera;
      if (Math.abs(sc.right - half) > 0.5) { sc.left = -half; sc.right = half; sc.top = half; sc.bottom = -half; sc.updateProjectionMatrix(); }
      sun.target.position.copy(orbit.target); sun.position.add(orbit.target);
      renderer.render(scene, camera);
      if (api.clock) api.clock.textContent = api.timeLabel();
    }
    function resize() {
      const w = host.clientWidth, h = host.clientHeight;
      if (!w || !h) return;
      renderer.setSize(w, h, false); el.style.width = w + 'px'; el.style.height = h + 'px';
      camera.aspect = w / h; camera.updateProjectionMatrix();
    }
    function setVisible(v) { el.style.display = v ? 'block' : 'none'; if (v) resize(); }
    return { el, build, render, resize, setVisible, zoom, resetView, get built() { return built; } };
  }
  window.createSettlement3D = createSettlement3D;
})();
