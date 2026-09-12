# このフォルダのJSONを編集しないこと

## 何が置かれているか

`earth-eras.json` は派生物である。正本はリポジトリの `site/data/earth-eras.json` だけである。

## 禁止

- このフォルダのJSONを直接編集しない。
- Unity側から `site/data/` へ書き戻さない。更新の向きは `site/data/` から本フォルダへの一方向のみである。

## 更新のしかた

`site/data/earth-eras.json` を直したうえで、Unityエディタのメニューから次を実行する。

1. `Tools/Civilization to Space/正本から交換データを同期`
2. `Tools/Civilization to Space/交換データのコピーを照合`

## 同一性の証明

gitのblobハッシュ一致をもって証明する。次の2つが同じSHAであればよい。

```text
git rev-parse HEAD:site/data/earth-eras.json
git rev-parse HEAD:unity/CivilizationToSpace/Assets/StreamingAssets/earth-eras.json
```

ディスク上のファイルハッシュは正としない。`core.autocrlf` が有効な環境では、正本は作業ツリー上でCRLFになる一方、
このコピーは `unity/.gitattributes` の `eol=lf` によりLFで取り出される。blobは正規化後の内容であり、環境差に左右されない。

照合メニューも同じ理由でLFへ正規化した内容どうしを比べる。

関連：[データ交換形式](../../../../docs/design/DATA_INTERCHANGE_R1.md)
