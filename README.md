# TubeEator

YouTube がダウンロードを禁止していることを確認せずに開発してしまいました。
このため、TubeEater のダウンロード機能は使わずに、
「マテリアルデザイン、カラー変更、非同期、EntityFramework、多言語対応、クリップボード監視、インストーラ作成」など、
アプリ開発の参考にお役立てください。
そのうち、ダウンロード機能をビューワ機能に変更したいと考えています。

TubeEater は YouTube ダウンローダーと呼ばれているアプリケーションに属します。
TubeEater を起動し、YouTube の URL を指定することで、
YouTube のコンテンツをダウンロードすることができます。

YouTube に公開されたコンテンツには著作権があります。
著作者の許可なく SNS などのサイトへアップロードしたり、
記憶媒体に保存して第三者への譲渡は行わないでください。
あくまでも、個人的に楽しむこととし、それ以外の利用は絶対に行わないでください。

# 特徴

- TubeEater の操作は、自動ダウンロードボタンをクリックするだけです。
- マテリアルデザインを使った画面で、誰もが使えるようにシンプルな操作にしました。
- テーマ・色も好みに合わせてカスタマイズできるようになっています。
- 多言語対応で、Languages 配下に JSON ファイルを追加するだけです。

# 動作条件

TubeEater は Windows 10 および Windows 11 の .NET 6 上で動作するアプリケーションです。

このため、TubeEater を実行するには、.NET Desktop Runtime ランタイムが必要になります。
お使いの Windows 環境に .NET 6 Desktop Runtime が必要になります。

必要に応じて .NET 6 の最新版をダウンロードし、Windows 環境にインストールしてください。

- [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/ja-jp/download/dotnet/6.0)


# インストール

[Release](https://github.com/sabakunotabito/TubeEater/releases) から最新版のインストーラをダウンロードし、インストールしてください。


# 使い方

デスクトップの TubeEater ショートカットをクリックすることで起動します。

TubeEater 起動後、画面上部に自動ダウンロードボタン(下記の丸い黄緑ボタン)が表示されています。
このボタンをクリックすると、アイコンがチェックマークに変更されます。
これで TubeEater の操作は完了です。

後は、ブラウザから URL をコピーするだけで、YouTube のコンテンツが自動で取り込まれます。

![TubeEater起動画面](https://github.com/sabakunotabito/tubeeater/blob/images/TubeEater01.png)


# 参考

TubeEater の開発過程はブログに掲載されています。

- [【C#】YouTube からビデオとオーディオをダウンロードするアプリを作ってみる - その1](https://sabakunotabito.hatenablog.com/entry/2022/02/28/012442)


# 謝辞

TubeEater は、以下のライブラリを利用しています。各ライブラリを公開して頂いた方々に深く感謝申し上げます。

- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro)
- [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [VideoLibrary](https://github.com/omansak/libvideo)
- [NAudio](https://github.com/naudio/NAudio)
- [TagLibSharp](https://github.com/mono/taglib-sharp)
- [NLog](https://nlog-project.org/)
