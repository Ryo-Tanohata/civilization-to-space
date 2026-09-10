'use strict';

/*
 * R2「生命圏の比較」P0
 *
 * 比較条件データの正本は data/biosphere-scenarios.json のみとする。
 * このファイルは比較条件の名称・説明・象徴値を一切保持しない。
 *
 * R1保護の原則（site/app.js には一切触れない）:
 *   - app.js の内部状態・関数・dom マップを参照しない
 *   - showFatalError / guard / #error / #error-detail / #retry / #controls に触れない
 *   - window に error / unhandledrejection のリスナーを登録しない
 *   - 例外を外へ再throwしない（漏れると app.js のグローバルハンドラがR1を全面停止させる）
 *   - R1の再生停止は #play の aria-pressed を読み、再生中のときだけ合成クリックで行う
 *   - タイマー・自動再生を作らない
 * 外部ライブラリ・CDN・外部APIは使用しない。
 */
(() => {
  const R2_DATA_URL = 'data/biosphere-scenarios.json';
  const ERA_DATA_URL = 'data/earth-eras.json';
  const SUPPORTED_SCHEMA_VERSIONS = ['r2-1.0.0'];
  const REQUIRED_CONDITION_COUNT = 4;
  const REQUIRED_VARIABLE_COUNT = 6;
  const ALLOWED_VALUES = [0, 25, 50, 75, 100];
  const BAND_STEP = 25;
  const DEFAULT_BASELINE_ID = 'bio_ocean';
  const DEFAULT_COMPARISON_ID = 'bio_oxygenation';

  // 象徴値をR2専用のCSSカスタムプロパティへ写す対応表（表示側の都合であり比較データではない）
  const GLOBE_VARS = {
    liquidOceanPresence: '--r2-ocean',
    iceCover: '--r2-ice',
    landVegetation: '--r2-veg',
    oxygenationStage: '--r2-oxy'
  };

  const byId = (id) => document.getElementById(id);
  const r2dom = {
    viewSwitch: byId('view-switch'),
    toR1: byId('to-r1'),
    toR2: byId('to-r2'),
    r1Panel: byId('r1-panel'),
    panel: byId('r2-panel'),
    heading: byId('r2-heading'),
    lead: byId('r2-lead'),
    notes: byId('r2-notes'),
    loading: byId('r2-loading'),
    error: byId('r2-error'),
    errorDetail: byId('r2-error-detail'),
    retry: byId('r2-retry'),
    body: byId('r2-body'),
    baselineOptions: byId('r2-baseline-options'),
    comparisonOptions: byId('r2-comparison-options'),
    aName: byId('r2-a-name'),
    bName: byId('r2-b-name'),
    aGlobe: byId('r2-a-globe'),
    bGlobe: byId('r2-b-globe'),
    aSummary: byId('r2-a-summary'),
    bSummary: byId('r2-b-summary'),
    caption: byId('r2-caption'),
    thA: byId('r2-th-a'),
    thB: byId('r2-th-b'),
    tbody: byId('r2-tbody'),
    limitations: byId('r2-limitations')
  };

  // R2専用の状態。R1の state（app.js内）には触れない。
  const state = {
    view: 'r1',
    variables: [],
    conditions: [],
    baselineId: DEFAULT_BASELINE_ID,
    comparisonId: DEFAULT_COMPARISON_ID,
    ready: false
  };
  let r2UnexpectedShown = false; // R1の unexpectedShown とは別のラッチ

  const isText = (value) => typeof value === 'string' && value.trim() !== '';
  const isBandValue = (value) => Number.isInteger(value) && ALLOWED_VALUES.indexOf(value) !== -1;
  const bandIndex = (value) => ALLOWED_VALUES.indexOf(value);

  function dataError(message) {
    const error = new Error('r2 catalog validation failed');
    error.userMessage = message;
    return error;
  }

  // R2内で完結する同期ガード。app.js の guard() は使わない。
  function r2Guard(fn) {
    return function guarded(...args) {
      try {
        return fn.apply(this, args);
      } catch (error) {
        handleR2Unexpected();
        return undefined;
      }
    };
  }

  /* ---------------- データ検証 ---------------- */

  function normalizeVariable(raw, position) {
    const where = `${position + 1}番目の比較変数`;
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
      throw dataError(`${where}のデータ形式が正しくありません。`);
    }
    ['id', 'displayName', 'definition', 'range', 'lowLabel', 'midLabel', 'highLabel', 'limitation'].forEach((key) => {
      if (!isText(raw[key])) throw dataError(`${where}に必須の情報がありません。`);
    });
    if (!Array.isArray(raw.bandLabels) || raw.bandLabels.length !== ALLOWED_VALUES.length || !raw.bandLabels.every(isText)) {
      throw dataError(`${where}の段階ラベルが${ALLOWED_VALUES.length}件そろっていません。`);
    }
    if (!isText(raw.moreWord) || !isText(raw.lessWord)) {
      throw dataError(`${where}の差を表す言葉がありません。`);
    }
    return {
      id: raw.id.trim(),
      displayName: raw.displayName.trim(),
      definition: raw.definition.trim(),
      range: raw.range.trim(),
      lowLabel: raw.lowLabel.trim(),
      midLabel: raw.midLabel.trim(),
      highLabel: raw.highLabel.trim(),
      limitation: raw.limitation.trim(),
      bandLabels: raw.bandLabels.map((label) => label.trim()),
      moreWord: raw.moreWord.trim(),
      lessWord: raw.lessWord.trim()
    };
  }

  function normalizeCondition(raw, position, variableIds, eraIds) {
    const where = `${position + 1}番目の比較条件`;
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
      throw dataError(`${where}のデータ形式が正しくありません。`);
    }
    ['id', 'displayName', 'summary', 'relatedEraId', 'designRationale', 'quality'].forEach((key) => {
      if (!isText(raw[key])) throw dataError(`${where}に必須の情報がありません。`);
    });
    if (!raw.values || typeof raw.values !== 'object' || Array.isArray(raw.values)) {
      throw dataError(`${where}の象徴値がありません。`);
    }
    // 関連時代IDはR1の6時代のいずれかでなければならない（画面には表示しない）
    if (eraIds && eraIds.indexOf(raw.relatedEraId.trim()) === -1) {
      throw dataError(`${where}の関連時代が、地球の時代と対応していません。`);
    }
    const values = {};
    variableIds.forEach((variableId) => {
      const value = raw.values[variableId];
      if (value === undefined) throw dataError(`${where}に「${variableId}」の象徴値がありません。`);
      if (!isBandValue(value)) {
        throw dataError(`${where}の象徴値が0・25・50・75・100のいずれかではありません。`);
      }
      values[variableId] = value;
    });
    Object.keys(raw.values).forEach((key) => {
      if (variableIds.indexOf(key) === -1) {
        throw dataError(`${where}に、比較変数にない項目「${key}」があります。`);
      }
    });
    return {
      id: raw.id.trim(),
      displayName: raw.displayName.trim(),
      summary: raw.summary.trim(),
      relatedEraId: raw.relatedEraId.trim(),
      designRationale: raw.designRationale.trim(),
      quality: raw.quality.trim(),
      values
    };
  }

  function normalizeCatalog(raw, eraIds) {
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
      throw dataError('比較条件データの形式が正しくありません。');
    }
    if (!isText(raw.schemaVersion) || SUPPORTED_SCHEMA_VERSIONS.indexOf(raw.schemaVersion.trim()) === -1) {
      throw dataError('このモックが対応していないデータ形式です。');
    }
    ['title', 'disclaimer', 'valueNote', 'orderNote', 'interchangeNote', 'axisNote', 'diffNote', 'omissionNote'].forEach((key) => {
      if (!isText(raw[key])) throw dataError('比較条件データの注記がそろっていません。');
    });
    if (!Array.isArray(raw.variables) || raw.variables.length !== REQUIRED_VARIABLE_COUNT) {
      throw dataError(`比較変数は${REQUIRED_VARIABLE_COUNT}件必要ですが、件数が一致しません。`);
    }
    if (!Array.isArray(raw.conditions) || raw.conditions.length !== REQUIRED_CONDITION_COUNT) {
      throw dataError(`比較条件は${REQUIRED_CONDITION_COUNT}件必要ですが、件数が一致しません。`);
    }

    const variables = raw.variables.map((item, index) => normalizeVariable(item, index));
    const variableIds = variables.map((item) => item.id);
    if (new Set(variableIds).size !== variableIds.length) {
      throw dataError('比較変数のIDが重複しています。');
    }

    const conditions = raw.conditions.map((item, index) => normalizeCondition(item, index, variableIds, eraIds));
    const conditionIds = conditions.map((item) => item.id);
    if (new Set(conditionIds).size !== conditionIds.length) {
      throw dataError('比較条件のIDが重複しています。');
    }
    if (conditionIds.indexOf(DEFAULT_BASELINE_ID) === -1 || conditionIds.indexOf(DEFAULT_COMPARISON_ID) === -1) {
      throw dataError('初期表示に使う比較条件が見つかりません。');
    }

    return {
      title: raw.title.trim(),
      disclaimer: raw.disclaimer.trim(),
      // 画面に常設する注記。順序は固定。
      notes: [raw.valueNote, raw.orderNote, raw.axisNote, raw.diffNote, raw.omissionNote].map((n) => n.trim()),
      variables,
      conditions
    };
  }

  /* ---------------- 表示 ---------------- */

  const findCondition = (id) => state.conditions.filter((item) => item.id === id)[0] || null;

  function buildOptions(container, groupName, selectedId, onPick) {
    container.textContent = '';
    state.conditions.forEach((condition) => {
      const label = document.createElement('label');
      label.className = 'r2-option';
      const input = document.createElement('input');
      input.type = 'radio';
      input.name = groupName;
      input.value = condition.id;
      input.id = `${groupName}-${condition.id}`;
      // 既定値はデータ読込後に明示的に指定する（ブラウザのフォーム値復元に依存しない）
      input.checked = condition.id === selectedId;
      const text = document.createElement('span');
      text.textContent = condition.displayName;
      label.append(input, text);
      input.addEventListener('change', r2Guard(() => {
        if (input.checked) onPick(condition.id);
      }));
      container.append(label);
    });
  }

  function markChecked(container, selectedId) {
    Array.from(container.children).forEach((label) => {
      const input = label.querySelector('input');
      if (!input) return;
      const on = input.value === selectedId;
      input.checked = on;
      label.classList.toggle('r2-option-checked', on);
    });
  }

  function renderGlobe(element, condition) {
    Object.keys(GLOBE_VARS).forEach((variableId) => {
      const ratio = condition.values[variableId] / 100;
      element.style.setProperty(GLOBE_VARS[variableId], String(ratio));
    });
    const words = state.variables
      .filter((variable) => Object.prototype.hasOwnProperty.call(GLOBE_VARS, variable.id))
      .map((variable) => `${variable.displayName}は${variable.bandLabels[bandIndex(condition.values[variable.id])]}`);
    element.setAttribute('aria-label', `${condition.displayName}の象徴的な地球。${words.join('、')}。色と模様は象徴表現です。`);
  }

  function diffCell(variable, aValue, bValue) {
    const cell = document.createElement('td');
    const steps = (bValue - aValue) / BAND_STEP;
    if (steps === 0) {
      const same = document.createElement('span');
      same.className = 'r2-diff-same';
      same.textContent = '同じ';
      cell.append(same);
      return cell;
    }
    const step = document.createElement('span');
    step.className = 'r2-diff-step';
    step.textContent = `${Math.abs(steps)}段階`;
    const dir = document.createElement('span');
    dir.className = 'r2-diff-dir';
    dir.textContent = steps > 0 ? variable.moreWord : variable.lessWord;
    cell.append(step, dir);
    return cell;
  }

  function valueCell(variable, value) {
    const cell = document.createElement('td');
    const word = document.createElement('span');
    word.className = 'r2-word';
    word.textContent = variable.bandLabels[bandIndex(value)];
    const num = document.createElement('span');
    num.className = 'r2-num';
    num.textContent = `${value} / 100`;
    cell.append(word, num);
    return cell;
  }

  function renderTable(baseline, comparison) {
    r2dom.tbody.textContent = '';
    state.variables.forEach((variable) => {
      const row = document.createElement('tr');
      const head = document.createElement('th');
      head.scope = 'row';
      head.textContent = variable.displayName;
      row.append(head, valueCell(variable, baseline.values[variable.id]), valueCell(variable, comparison.values[variable.id]),
        diffCell(variable, baseline.values[variable.id], comparison.values[variable.id]));
      r2dom.tbody.append(row);
    });
  }

  function renderLimitations() {
    r2dom.limitations.textContent = '';
    state.variables.forEach((variable) => {
      const term = document.createElement('dt');
      term.textContent = variable.displayName;
      const detail = document.createElement('dd');
      detail.textContent = `${variable.definition} ${variable.limitation}`;
      r2dom.limitations.append(term, detail);
    });
  }

  function render() {
    if (!state.ready) return;
    const baseline = findCondition(state.baselineId);
    const comparison = findCondition(state.comparisonId);
    if (!baseline || !comparison) return;

    markChecked(r2dom.baselineOptions, state.baselineId);
    markChecked(r2dom.comparisonOptions, state.comparisonId);

    r2dom.aName.textContent = baseline.displayName;
    r2dom.bName.textContent = comparison.displayName;
    r2dom.aSummary.textContent = baseline.summary;
    r2dom.bSummary.textContent = comparison.summary;
    renderGlobe(r2dom.aGlobe, baseline);
    renderGlobe(r2dom.bGlobe, comparison);

    r2dom.thA.textContent = `基準A：${baseline.displayName}`;
    r2dom.thB.textContent = `比較B：${comparison.displayName}`;
    r2dom.caption.textContent = baseline.id === comparison.id
      ? `基準Aと比較Bに同じ「${baseline.displayName}」を選んでいます。差はすべて「同じ」です。`
      : `基準A「${baseline.displayName}」と比較B「${comparison.displayName}」の象徴値。差は段階の違いであり、優劣ではありません。`;
    renderTable(baseline, comparison);
  }

  /* ---------------- ビュー切替 ---------------- */

  // R1の再生を止める唯一の手段。app.js の内部には触れず、公開されているボタン状態だけを読む。
  function stopR1Playback() {
    const play = document.getElementById('play');
    if (play && !play.disabled && play.getAttribute('aria-pressed') === 'true') play.click();
  }

  function showR1() {
    state.view = 'r1';
    r2dom.panel.hidden = true;
    r2dom.r1Panel.hidden = false;
    r2dom.toR1.setAttribute('aria-pressed', 'true');
    r2dom.toR2.setAttribute('aria-pressed', 'false');
    r2dom.toR1.focus();
  }

  function showR2() {
    stopR1Playback(); // 隠す前に必ず停止する。隠してもR1のタイマーは進み続けるため。
    state.view = 'r2';
    r2dom.r1Panel.hidden = true;
    r2dom.panel.hidden = false;
    r2dom.toR1.setAttribute('aria-pressed', 'false');
    r2dom.toR2.setAttribute('aria-pressed', 'true');
    r2dom.heading.focus();
  }

  /* ---------------- エラー処理（R2だけを止める） ---------------- */

  function showR2Error(message) {
    state.ready = false;
    r2dom.loading.hidden = true;
    r2dom.body.hidden = true;
    r2dom.errorDetail.textContent = message;
    r2dom.error.hidden = false;
    if (state.view === 'r2') r2dom.error.focus();
  }

  function handleR2Unexpected() {
    if (r2UnexpectedShown) return;
    r2UnexpectedShown = true;
    showR2Error('比較の表示中に問題が発生しました。再読み込みをお試しください。R1の「地球の時代」はそのまま利用できます。');
  }

  /* ---------------- 読み込み ---------------- */

  function applyCatalog(catalog) {
    r2dom.lead.textContent = catalog.disclaimer;
    r2dom.notes.textContent = '';
    catalog.notes.forEach((note) => {
      const item = document.createElement('li');
      item.textContent = note;
      r2dom.notes.append(item);
    });

    state.variables = catalog.variables;
    state.conditions = catalog.conditions;
    state.baselineId = DEFAULT_BASELINE_ID;
    state.comparisonId = DEFAULT_COMPARISON_ID;

    buildOptions(r2dom.baselineOptions, 'r2-baseline', state.baselineId, (id) => {
      state.baselineId = id;
      render();
    });
    buildOptions(r2dom.comparisonOptions, 'r2-comparison', state.comparisonId, (id) => {
      state.comparisonId = id;
      render();
    });
    renderLimitations();

    state.ready = true;
    r2dom.loading.hidden = true;
    r2dom.error.hidden = true;
    r2dom.body.hidden = false;
    render();
  }

  async function fetchJson(url) {
    const response = await fetch(url, { cache: 'no-store' });
    if (!response.ok) throw dataError('データファイルを取得できませんでした。');
    return response.json();
  }

  async function load() {
    r2dom.error.hidden = true;
    r2dom.errorDetail.textContent = '';
    r2dom.body.hidden = true;
    r2dom.loading.hidden = false;
    state.ready = false;

    // 取得失敗・HTTPエラー・JSON構文不正はそれぞれ原因が違うため、別々の案内を出す
    let response;
    try {
      response = await fetch(R2_DATA_URL, { cache: 'no-store' });
    } catch (error) {
      showR2Error('比較条件データを取得できませんでした。ローカルHTTPサーバー経由で開いているか確認してください。R1の「地球の時代」はそのまま利用できます。');
      return;
    }
    if (!response.ok) {
      showR2Error('比較条件データのファイルを取得できませんでした。data/biosphere-scenarios.json があるか確認してください。R1の「地球の時代」はそのまま利用できます。');
      return;
    }
    let payload;
    try {
      payload = await response.json();
    } catch (error) {
      showR2Error('比較条件データのファイル形式が正しくありません。R1の「地球の時代」はそのまま利用できます。');
      return;
    }

    let eraIds = null;
    try {
      const eraPayload = await fetchJson(ERA_DATA_URL);
      if (eraPayload && Array.isArray(eraPayload.eras)) {
        eraIds = eraPayload.eras.map((era) => (era && typeof era.id === 'string' ? era.id.trim() : '')).filter((id) => id !== '');
      }
    } catch (error) {
      eraIds = null; // 時代データを読めない場合は関連時代の照合を省く（R1側で別途エラーが出る）
    }

    let catalog;
    try {
      catalog = normalizeCatalog(payload, eraIds);
    } catch (error) {
      showR2Error((error && error.userMessage ? error.userMessage : '比較条件データの内容を確認できませんでした。')
        + ' R1の「地球の時代」はそのまま利用できます。');
      return;
    }

    try {
      applyCatalog(catalog);
    } catch (error) {
      handleR2Unexpected();
    }
  }

  /* ---------------- 初期化 ---------------- */

  function init() {
    const missing = Object.keys(r2dom).filter((key) => !r2dom[key]);
    if (missing.length > 0) return; // R2の画面構造が無ければ何もしない。R1には影響させない。

    r2dom.toR1.addEventListener('click', r2Guard(showR1));
    r2dom.toR2.addEventListener('click', r2Guard(showR2));
    r2dom.retry.addEventListener('click', r2Guard(() => {
      // 戻り値のPromiseもR2内で握りつぶす。外へ漏らさない。
      load().catch(() => { handleR2Unexpected(); });
    }));

    r2dom.panel.hidden = true;
    r2dom.r1Panel.hidden = false;
    r2dom.toR1.setAttribute('aria-pressed', 'true');
    r2dom.toR2.setAttribute('aria-pressed', 'false');

    load().catch(() => { handleR2Unexpected(); });
  }

  // モジュール全体を包む。ここから例外を出さないことがR1保護の最後の砦。
  try {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', r2Guard(init));
    } else {
      r2Guard(init)();
    }
  } catch (error) {
    /* R1へ伝播させない */
  }
})();
