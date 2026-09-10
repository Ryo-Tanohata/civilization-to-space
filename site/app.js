'use strict';

/*
 * Earth Through Time / R1 P0 ブラウザモック
 *
 * 時代データの正本は data/earth-eras.json のみとする。
 * このファイルは時代の名称・年代ラベル・解説・視覚値を一切保持しない。
 * NEUTRAL_VISUAL と CSS_VARS は表示側の都合であり、特定の時代を表すデータではない。
 * 外部ライブラリ・CDN・外部APIは使用しない。
 */
(() => {
  const DATA_URL = 'data/earth-eras.json';
  const SUPPORTED_SCHEMA_VERSIONS = ['1.0.0'];
  const REQUIRED_ERA_COUNT = 6;
  const BASE_STEP_MS = 4000; // 1x=4秒 / 0.5x=8秒 / 2x=2秒
  const ALLOWED_SPEEDS = [0.5, 1, 2];
  const DEFAULT_SPEED = 1;

  const RATIO_KEYS = ['oceanLevel', 'cloudDensity', 'iceCoverage', 'vegetation', 'volcanicActivity', 'cityLights'];
  const COLOR_KEYS = ['earthColor', 'emissionColor'];
  // CSSとして有効な16進色の桁数だけを受け付ける（5桁・7桁は無効な色になるため弾く）
  const COLOR_PATTERN = /^#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/;

  // 演出強度をCSSカスタムプロパティへ写す対応表（レンダリング都合であり共通データではない）
  const CSS_VARS = {
    earthColor: '--earth-color',
    emissionColor: '--emission-color',
    oceanLevel: '--ocean',
    cloudDensity: '--cloud',
    iceCoverage: '--ice',
    vegetation: '--vegetation',
    volcanicActivity: '--volcano',
    cityLights: '--cities'
  };

  // 視覚パラメータが欠損したときに使う中立の地球。いずれの時代も表さない。
  const NEUTRAL_VISUAL = {
    earthColor: '#697887',
    emissionColor: '#384b60',
    oceanLevel: 0.45,
    cloudDensity: 0.2,
    iceCoverage: 0.1,
    vegetation: 0.2,
    volcanicActivity: 0,
    cityLights: 0,
    satelliteCount: 0
  };

  const byId = (id) => document.getElementById(id);
  const dom = {
    catalogTitle: byId('catalog-title'),
    disclaimer: byId('disclaimer'),
    parameterNote: byId('parameter-note'),
    error: byId('error'),
    errorDetail: byId('error-detail'),
    retry: byId('retry'),
    loading: byId('loading'),
    planet: byId('planet'),
    eraId: byId('era-id'),
    eraName: byId('era-name'),
    rangeLabel: byId('range-label'),
    summary: byId('summary'),
    tags: byId('tags'),
    eventsSection: byId('events-section'),
    events: byId('events'),
    qualityStatus: byId('quality-status'),
    visualWarning: byId('visual-warning'),
    future: byId('future'),
    futureIntro: byId('future-intro'),
    scenarios: byId('scenarios'),
    playbackStatus: byId('playback-status'),
    controls: byId('controls'),
    eraButtons: byId('era-buttons'),
    slider: byId('era-slider'),
    sliderLabel: byId('slider-label'),
    previous: byId('previous'),
    play: byId('play'),
    next: byId('next'),
    speed: byId('speed')
  };

  // 状態は一箇所で管理し、表示はすべてここから導く（DOMごとに別の時代状態を持たない）
  const state = { eras: [], index: 0, playing: false, speed: DEFAULT_SPEED, ready: false };
  let timerId = null; // 常に最大1つ
  let satelliteNodes = [];
  let unexpectedShown = false;

  const isText = (value) => typeof value === 'string' && value.trim() !== '';
  const isRatio = (value) => typeof value === 'number' && Number.isFinite(value) && value >= 0 && value <= 1;
  const isColor = (value) => isText(value) && COLOR_PATTERN.test(value.trim());
  const isCount = (value) => Number.isInteger(value) && value >= 0;
  const clamp = (value, min, max) => Math.min(Math.max(value, min), max);
  const pad2 = (value) => String(value).padStart(2, '0');
  const stepMs = () => Math.round(BASE_STEP_MS / state.speed);

  // 利用者へ見せてよい文言だけを userMessage に持たせる（内部パスや例外全文は表示しない）
  function dataError(message) {
    const error = new Error('era catalog validation failed');
    error.userMessage = message;
    return error;
  }

  function guard(fn) {
    return function guarded(...args) {
      try {
        return fn.apply(this, args);
      } catch (error) {
        handleUnexpected();
        return undefined;
      }
    };
  }

  /* ---------------- データ検証と正規化 ---------------- */

  function normalizeVisual(source) {
    const visual = {};
    let degraded = false;
    COLOR_KEYS.forEach((key) => {
      if (isColor(source[key])) {
        visual[key] = source[key].trim();
      } else {
        visual[key] = NEUTRAL_VISUAL[key];
        degraded = true;
      }
    });
    RATIO_KEYS.forEach((key) => {
      if (isRatio(source[key])) {
        visual[key] = source[key];
      } else {
        visual[key] = NEUTRAL_VISUAL[key];
        degraded = true;
      }
    });
    if (isCount(source.satelliteCount)) {
      visual.satelliteCount = source.satelliteCount;
    } else {
      visual.satelliteCount = NEUTRAL_VISUAL.satelliteCount;
      degraded = true;
    }
    return { visual, degraded };
  }

  function normalizeEra(raw, position) {
    const where = `${position + 1}番目の時代`;
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
      throw dataError(`${where}のデータ形式が正しくありません。`);
    }
    ['id', 'displayName', 'rangeLabel', 'summary', 'status'].forEach((key) => {
      if (!isText(raw[key])) throw dataError(`${where}に必須の情報がありません。`);
    });
    if (!isCount(raw.sortOrder) || raw.sortOrder < 1) {
      throw dataError(`${where}の並び順が正しくありません。`);
    }
    if (!raw.visual || typeof raw.visual !== 'object' || Array.isArray(raw.visual)) {
      throw dataError(`${where}の視覚情報がありません。`);
    }
    if (!raw.presentation || typeof raw.presentation !== 'object' || Array.isArray(raw.presentation)) {
      throw dataError(`${where}の表示情報がありません。`);
    }
    if (!Array.isArray(raw.events)) {
      throw dataError(`${where}のイベント情報の形式が正しくありません。`);
    }

    const presentation = raw.presentation;
    const normalized = normalizeVisual(raw.visual);
    const candidates = Array.isArray(presentation.scenarioCandidates) ? presentation.scenarioCandidates : [];

    return {
      id: raw.id.trim(),
      displayName: raw.displayName.trim(),
      rangeLabel: raw.rangeLabel.trim(),
      sortOrder: raw.sortOrder,
      summary: raw.summary.trim(),
      status: raw.status.trim(),
      visual: normalized.visual,
      visualDegraded: normalized.degraded,
      caption: isText(presentation.caption) ? presentation.caption.trim() : '',
      tags: (Array.isArray(presentation.tags) ? presentation.tags : []).filter(isText).map((tag) => tag.trim()),
      futureNote: isText(presentation.futureNote) ? presentation.futureNote.trim() : '',
      // R1 P0では読み取り専用の候補表示のみ。選択・分岐ロジックは実装しない。
      scenarios: candidates
        .filter((item) => item && typeof item === 'object' && isText(item.name) && isText(item.assumption))
        .map((item) => ({ name: item.name.trim(), assumption: item.assumption.trim() })),
      events: raw.events.filter(isText).map((text) => text.trim())
    };
  }

  function normalizeCatalog(raw) {
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
      throw dataError('時代データの形式が正しくありません。');
    }
    if (!isText(raw.schemaVersion) || !SUPPORTED_SCHEMA_VERSIONS.includes(raw.schemaVersion.trim())) {
      throw dataError('このモックが対応していないデータ形式です。');
    }
    if (!isText(raw.catalogTitle) || !isText(raw.disclaimer)) {
      throw dataError('カタログの見出しまたは注意書きがありません。');
    }
    if (!Array.isArray(raw.eras) || raw.eras.length !== REQUIRED_ERA_COUNT) {
      throw dataError(`時代は${REQUIRED_ERA_COUNT}件必要ですが、件数が一致しません。`);
    }

    const eras = raw.eras.map((era, index) => normalizeEra(era, index));

    const ids = eras.map((era) => era.id);
    if (new Set(ids).size !== ids.length) throw dataError('時代のIDが重複しています。');
    const orders = eras.map((era) => era.sortOrder);
    if (new Set(orders).size !== orders.length) throw dataError('時代の並び順が重複しています。');

    eras.sort((a, b) => a.sortOrder - b.sortOrder);
    eras.forEach((era, index) => {
      if (era.sortOrder !== index + 1) {
        throw dataError(`時代の並び順が1から${REQUIRED_ERA_COUNT}の連番になっていません。`);
      }
    });

    // eraOrderは必須6 IDの明細。存在する場合は順序まで一致することを求める。
    if (Array.isArray(raw.eraOrder)) {
      const expected = raw.eraOrder.filter(isText).map((id) => id.trim());
      if (expected.length !== eras.length || expected.some((id, index) => id !== eras[index].id)) {
        throw dataError('時代のIDまたは並び順が想定と一致しません。');
      }
    }

    return {
      title: raw.catalogTitle.trim(),
      disclaimer: raw.disclaimer.trim(),
      parameterNote: isText(raw.parameterNote) ? raw.parameterNote.trim() : '',
      eras
    };
  }

  /* ---------------- 表示 ---------------- */

  function fillList(list, items) {
    list.textContent = '';
    items.forEach((text) => {
      const li = document.createElement('li');
      li.textContent = text;
      list.append(li);
    });
  }

  function buildEraButtons() {
    dom.eraButtons.textContent = '';
    state.eras.forEach((era, index) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.setAttribute('aria-pressed', 'false');

      const step = document.createElement('span');
      step.className = 'era-step';
      step.textContent = `${pad2(era.sortOrder)} ${era.id}`;

      const title = document.createElement('span');
      title.className = 'era-title';
      title.textContent = era.displayName;

      button.append(step, title);
      button.addEventListener('click', guard(() => selectEra(index)));
      dom.eraButtons.append(button);
    });
  }

  function renderPlanet(era) {
    const visual = era.visual;
    Object.keys(CSS_VARS).forEach((key) => {
      const value = visual[key];
      dom.planet.style.setProperty(CSS_VARS[key], typeof value === 'number' ? String(value) : value);
    });
    dom.planet.style.setProperty('--satellites', visual.satelliteCount > 0 ? '1' : '0');
    satelliteNodes.forEach((node, index) => {
      node.hidden = index >= visual.satelliteCount;
    });
    // 色だけに頼らず、地球の意味を短い説明で支援技術へ渡す
    dom.planet.setAttribute('aria-label', era.caption || `${era.displayName}の象徴的な地球`);
  }

  function renderFuture(era) {
    const hasScenarios = era.scenarios.length > 0;
    dom.future.hidden = !hasScenarios;
    dom.futureIntro.textContent = hasScenarios ? era.futureNote : '';
    dom.scenarios.textContent = '';
    if (!hasScenarios) return;
    era.scenarios.forEach((scenario) => {
      const dt = document.createElement('dt');
      dt.textContent = scenario.name;
      const dd = document.createElement('dd');
      dd.textContent = scenario.assumption;
      dom.scenarios.append(dt, dd);
    });
  }

  function playbackText(era, total) {
    const position = `${era.sortOrder} / ${total} ${era.displayName}`;
    if (!state.playing) return `停止中 ・ ${position}`;
    return `再生中 ${state.speed}x（1時代あたり約${Math.round(stepMs() / 1000)}秒）・ ${position}`;
  }

  function render() {
    const era = state.eras[state.index];
    if (!era) return;
    const total = state.eras.length;
    const last = total - 1;

    renderPlanet(era);

    dom.eraId.textContent = `${pad2(era.sortOrder)} / ${pad2(total)} ・ ${era.id}`;
    dom.eraName.textContent = era.displayName;
    dom.rangeLabel.textContent = era.rangeLabel;
    dom.summary.textContent = era.summary;
    dom.qualityStatus.textContent = era.status;
    dom.visualWarning.hidden = !era.visualDegraded;

    fillList(dom.tags, era.tags);
    dom.tags.hidden = era.tags.length === 0;
    fillList(dom.events, era.events);
    dom.eventsSection.hidden = era.events.length === 0;

    renderFuture(era);

    Array.from(dom.eraButtons.children).forEach((button, index) => {
      button.setAttribute('aria-pressed', index === state.index ? 'true' : 'false');
    });

    if (dom.slider.value !== String(state.index)) dom.slider.value = String(state.index);
    dom.slider.setAttribute('aria-valuetext', `${era.sortOrder} / ${total}：${era.displayName}`);
    dom.sliderLabel.textContent = `（${era.sortOrder} / ${total}：${era.displayName}）`;

    dom.previous.disabled = state.index === 0; // Hadeanの前は無効（循環しない）
    dom.next.disabled = state.index === last;  // Futureの次は無効（循環しない）
    dom.play.textContent = state.playing ? '停止' : (state.index === last ? '最初から再生' : '再生');
    dom.play.setAttribute('aria-pressed', state.playing ? 'true' : 'false');
    dom.playbackStatus.textContent = playbackText(era, total);
  }

  /* ---------------- 操作と再生 ---------------- */

  function clearTimer() {
    if (timerId !== null) {
      window.clearTimeout(timerId);
      timerId = null;
    }
  }

  function scheduleNext() {
    clearTimer(); // 予約前に必ず解除し、タイマーの多重起動を防ぐ
    timerId = window.setTimeout(guard(advance), stepMs());
  }

  function advance() {
    timerId = null;
    if (!state.playing) return;
    const last = state.eras.length - 1;
    if (state.index >= last) {
      stopPlayback();
      return;
    }
    state.index += 1;
    if (state.index >= last) {
      // Futureに到達したら自動再生を停止する
      state.playing = false;
      clearTimer();
    }
    render();
    if (state.playing) scheduleNext();
  }

  function stopPlayback() {
    clearTimer();
    if (!state.playing) return;
    state.playing = false;
    render();
  }

  function togglePlayback() {
    if (!state.ready) return;
    if (state.playing) {
      stopPlayback();
      return;
    }
    // Futureで再生を開始した場合はHadeanへ戻してから再生する
    if (state.index >= state.eras.length - 1) state.index = 0;
    state.playing = true;
    render();
    scheduleNext();
  }

  function selectEra(index) {
    if (!state.ready) return;
    // 手動で時代を変更したら自動再生を停止する
    state.playing = false;
    clearTimer();
    state.index = clamp(index, 0, state.eras.length - 1);
    render();
  }

  function changeSpeed() {
    if (!state.ready) return;
    const value = Number(dom.speed.value);
    state.speed = ALLOWED_SPEEDS.includes(value) ? value : DEFAULT_SPEED;
    dom.speed.value = String(state.speed);
    render();
    // 再生状態は保ち、変更時点から新しい間隔で計時し直す
    if (state.playing) scheduleNext();
  }

  /* ---------------- エラー処理 ---------------- */

  function showFatalError(message) {
    state.ready = false;
    state.playing = false;
    clearTimer();
    dom.controls.disabled = true; // 操作を無効化して安全な状態を保つ
    dom.loading.hidden = true;
    dom.errorDetail.textContent = message;
    dom.error.hidden = false;
    dom.qualityStatus.textContent = '読み込めません';
    dom.playbackStatus.textContent = '停止中（操作できません）';
  }

  function handleUnexpected() {
    if (unexpectedShown) return;
    unexpectedShown = true;
    showFatalError('画面の更新中に問題が発生しました。ページを再読み込みしてください。');
  }

  /* ---------------- 読み込み ---------------- */

  function applyCatalog(catalog) {
    dom.catalogTitle.textContent = catalog.title;
    dom.disclaimer.textContent = catalog.disclaimer;
    dom.parameterNote.textContent = catalog.parameterNote;

    state.eras = catalog.eras;
    state.index = 0;             // 初期状態：Hadean
    state.playing = false;       // 初期状態：停止
    state.speed = DEFAULT_SPEED; // 初期状態：1x

    dom.speed.value = String(DEFAULT_SPEED);
    dom.slider.min = '0';
    dom.slider.max = String(catalog.eras.length - 1);
    dom.slider.step = '1';
    dom.slider.value = '0';

    buildEraButtons();
    state.ready = true;
    dom.loading.hidden = true;
    dom.error.hidden = true;
    dom.controls.disabled = false;
    render();
  }

  async function load() {
    dom.error.hidden = true;
    dom.errorDetail.textContent = '';
    dom.loading.hidden = false;
    dom.controls.disabled = true;
    state.ready = false;
    state.playing = false;
    clearTimer();

    let response;
    try {
      response = await fetch(DATA_URL, { cache: 'no-store' });
    } catch (error) {
      showFatalError('時代データを取得できませんでした。ローカルHTTPサーバー経由で開いているか確認してください。');
      return;
    }
    if (!response.ok) {
      showFatalError('時代データのファイルを取得できませんでした。data/earth-eras.json があるか確認してください。');
      return;
    }

    let payload;
    try {
      payload = await response.json();
    } catch (error) {
      showFatalError('時代データのファイル形式が正しくありません。');
      return;
    }

    let catalog;
    try {
      catalog = normalizeCatalog(payload);
    } catch (error) {
      showFatalError(error && error.userMessage ? error.userMessage : '時代データの内容を確認できませんでした。');
      return;
    }

    applyCatalog(catalog);
  }

  /* ---------------- 初期化 ---------------- */

  function wireEvents() {
    dom.previous.addEventListener('click', guard(() => selectEra(state.index - 1)));
    dom.next.addEventListener('click', guard(() => selectEra(state.index + 1)));
    dom.play.addEventListener('click', guard(togglePlayback));
    dom.slider.addEventListener('input', guard(() => selectEra(Number(dom.slider.value))));
    dom.speed.addEventListener('change', guard(changeSpeed));
    dom.retry.addEventListener('click', guard(() => { load(); }));
    // タブが非表示になったら自動再生を停止する（戻っても自動再開しない）
    document.addEventListener('visibilitychange', guard(() => {
      if (document.hidden) stopPlayback();
    }));
    // 予期しないエラーでも画面が無反応にならないようにする
    window.addEventListener('error', handleUnexpected);
    window.addEventListener('unhandledrejection', handleUnexpected);
  }

  function init() {
    const missing = Object.keys(dom).filter((key) => !dom[key]);
    if (missing.length > 0) return; // 画面構造が想定と異なる場合は何もしない
    satelliteNodes = Array.from(dom.planet.querySelectorAll('.satellites i'));
    dom.speed.value = String(DEFAULT_SPEED);
    dom.slider.value = '0';
    dom.playbackStatus.textContent = '停止中';
    wireEvents();
    load();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', guard(init));
  } else {
    guard(init)();
  }
})();
