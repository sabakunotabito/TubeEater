# TubeEater

> **⚠️ 重要なお知らせ：このプロジェクトは開発を中止しており、ソースコードは教育・研究目的でのみ公開されています ⚠️**
>
> このアプリケーションは、YouTubeの利用規約を十分に確認しないまま開発を進めてしまった、いわば**「失敗の記録」**です。
>
> 現在、YouTubeの仕様変更により、このアプリケーションの核となるライブラリ (`VideoLibrary`) が機能しなくなっており、**このコードをビルドしてもコンテンツをダウンロードすることはできません。**
>
> このリポジトリは、開発中止に至った経緯と、その過程で得られた技術的な知見を、将来のプロジェクトへの教訓として残すために公開されています。

---

## 概要 (Overview)

**TubeEater** は、かつてYouTubeのコンテンツをダウンロードするために開発された、WPFベースのデスクトップアプリケーションのソースコードです。

このプロジェクトの価値は、ダウンロード機能にはありません。
当初はMVVMパターンとPrismフレームワークの導入を目指しましたが、道半ばで断念した経緯があります。
その試行錯誤の結果として、WPFアプリケーション開発における、以下のような**技術的な実装例のサンプル**として、このソースコードを活用していただければ幸いです。

*   **モダンなUI設計:** Material Design (MaterialDesignInXamlToolkit) と MahApps.Metro
*   **非同期処理:** クリップボード監視と連携したバックグラウンド処理
*   **データ永続化:** Entity Framework Core (SQLite) を用いた履歴管理
*   **多言語対応:** JSONファイルによる動的な言語切り替え
*   **メディア操作:** NAudio, TagLib# を利用した音声フォーマット変換とタグ編集
*   **インストーラ作成:** Visual Studio Installer Projects の活用例

## 注意事項 (Disclaimer)

YouTubeに公開されたコンテンツには著作権があります。このリポジトリのコードは、あくまで技術的な学習目的でのみ参照してください。コンテンツのダウンロード、再配布、譲渡は、各国の法律およびYouTubeの利用規約に抵触する可能性があります。

## 開発環境 (Development Environment)

このアプリケーションが開発された当時の環境は以下の通りです。

*   **OS:** Windows 10 / 11
*   **IDE:** Visual Studio 2022
*   **フレームワーク:** .NET 6

## 開発の記録 (Development Journey)

このアプリケーションが、どのような経緯で生まれ、そして幻となったのか。その開発過程は、ブログに詳しく記されています。

![TubeEater Screenshot](https://github.com/sabakunotabito/tubeeater/blob/images/TubeEater01.png)

*   **[砂漠の旅人 - TubeEater開発譚](https://sabakunotabito.hatenablog.com/archive/category/TubeEater)**

## 謝辞 (Acknowledgements)

TubeEaterは、以下の素晴らしいライブラリの力によって成り立っていました。各ライブラリの作者様、そしてコントリビューターの方々に、心から感謝申し上げます。

*   [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
*   [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
*   [VideoLibrary](https://github.com/omansak/libvideo) (libvideo)
*   [NAudio](https://github.com/naudio/NAudio)
*   [TagLibSharp](https://github.com/mono/taglib-sharp)
*   [NLog](https://nlog-project.org/)
