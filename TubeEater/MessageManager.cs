using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TubeEater
{
    /// <summary>
    /// メッセージ管理
    /// </summary>
    public class MessageManager
    {
        // 標準言語
        public static string DefaultLanguage { get; } = CultureInfo.CurrentCulture.Name.Equals("ja-JP") ? "Japanese" : "English";

        // NLog
        private Logger _logger = LogManager.GetCurrentClassLogger();

        // 各国の言語データ
        public class LangData
        {
            public string Name { get; set; } = String.Empty;                        // 言語
            public string Locale { get; set; } = CultureInfo.CurrentCulture.Name;   // ロケール(ja-JP)
            public DateTime Update { get; set; }                                    // DB格納日時
            public Dictionary<string, string> Messages { get; set; } = new();       // メッセージ集
        };

        // 現在の言語
        public string CurrentLang { get; set; } = DefaultLanguage;

        // 言語データ
        public List<LangData> Languages { get; set; } = new();

        // TubeEater標準の言語データ
        public LangData Default = CultureInfo.CurrentCulture.Name.Equals("ja-JP") ?
            new()
            {
                Name = DefaultLanguage,
                Locale = CultureInfo.CurrentCulture.Name,
                Update = DateTime.Now,
                Messages = new Dictionary<string, string>()
                {
                    // システム共通
                    ["c0001"] = "はい",
                    ["c0002"] = "いいえ",

                    // エラー
                    ["e0001"] = "予期せぬエラーが発生しました。",
                    // IO
                    ["e0101"] = "フォルダが見つかりません。PATH:{0}",
                    // データベース
                    ["e0201"] = "データベースを格納するフォルダが作成できません。",
                    ["e0202"] = "以下のフォルダを作成し、アクセス権を追加してください。",
                    ["e0203"] = "データベースのアクセス時に予期せぬエラーが発生しました。",
                    // アプリケーション
                    ["e1001"] = "言語の設定時に予期せぬエラーが発生しました。",
                    ["e1002"] = "YouTube のダウンロード処理に失敗しました。",
                    ["e1003"] = "YouTube の解析に失敗しました。ダウンロード予約はキャンセルされます。",
                    ["e1004"] = "履歴表示に問題が発生しました。",
                    ["e1005"] = "ライブ配信はダウンロードできません。",

                    // 警告
                    ["w1001"] = "まだダウンロード中です。本当に終了しますか？",
                    ["w1002"] = "予約中の URL は次回の起動後に再開されます。",
                    ["w1003"] = "'{0}' - {1} は既に保存されています。",
                    ["w1004"] = "'{0}' は開始または完了したため、予約を削除できません。",
                    ["w1005"] = "'{0}' をデータベースから削除しますか？",

                    // 情報
                    ["i0101"] = "前回の続きからダウンロードを開始しますか？",
                    ["i0102"] = "VideoId={0}: YouTube から情報を取得しています。",
                    ["i0103"] = "'{0}' - {1}, ダウンロード中です。",
                    ["i0104"] = "'{0}' - {1}, ダウンロードを完了しました ({2:#,0.0}m)。",
                    ["i0111"] = "ダウンロード履歴をデータベースから取得しています。",
                    ["i0112"] = "キーワード「{0}」を検索しています。",

                    // Text
                    ["xt001"] = "ダウンロード履歴",
                    ["xt002"] = "ライト",
                    ["xt003"] = "ダーク",
                    ["xt004"] = "オプション",
                    ["xt005"] = "保存先を開く",
                    ["xt006"] = "オプション",
                    ["xt007"] = "タイトル",
                    ["xt008"] = "著書",
                    ["xt009"] = "情報を更新する",

                    // Content
                    ["xc001"] = "Windows と同期",
                    ["xc002"] = "カラー調整",
                    ["xc003"] = "ビデオ (MP4)",
                    ["xc004"] = "オーディオ (AAC)",
                    ["xc005"] = "オーディオ (MP3)",

                    // Header
                    ["xh001"] = "カラー",
                    ["xh002"] = "言語",
                    ["xh003"] = "保存ファイル",
                    ["xh004"] = "No.",
                    ["xh005"] = "完了",
                    ["xh006"] = "タイトル",
                    ["xh007"] = "著者",
                    ["xh008"] = "時間",
                    ["xh009"] = "サイズ",
                    ["xh010"] = "コーデック",
                    ["xh011"] = "解像度",
                    ["xh012"] = "FPS",
                    ["xh013"] = "Kbits",
                    ["xh014"] = "日付",
                    ["xh015"] = "コピー",
                    ["xh016"] = "ブラウザ表示",
                    ["xh017"] = "保存先を開く",
                    ["xh018"] = "再生",
                    ["xh019"] = "予約を削除する",
                    ["xh020"] = "キーワード",
                    ["xh021"] = "絞り込み",
                    ["xh022"] = "作成日",
                    ["xh023"] = "更新日",
                    ["xh024"] = "期間",
                    ["xh025"] = "データ削除",

                    // ToolTip
                    ["tt001"] = "GitHub サイトを開く",
                    ["tt002"] = "たびとブログを開く",
                    ["tt011"] = "Windows と同じモードに設定します",
                    ["tt012"] = "ウィンドウ(テーマ)の色を変更します",
                    ["tt013"] = "メイン(プライマリ)の色を変更します",
                    ["tt014"] = "アクセント(セカンダリ)の色を変更します",
                    ["tt015"] = "必要に応じて色を調整します",
                    ["tt016"] = "言語を選択します",
                    ["tt017"] = "ダウンロード開始",
                    ["tt018"] = "URLをコピーすると、自動的にダウンロードを開始します",
                    ["tt019"] = "データベースの情報を更新します",

                    // HintAssist
                    ["ha011"] = "ウィンドウ",
                    ["ha012"] = "メイン",
                    ["ha013"] = "アクセント",
                    ["ha014"] = "言語",
                    ["ha015"] = "YouTube の URL を入力または貼り付けてください",
                    ["ha016"] = "検索キーワードを入力してください",
                }
            } :
            new()
            {
                Name = DefaultLanguage,
                Locale = "en-US",
                Update = DateTime.Now,
                Messages = new Dictionary<string, string>()
                {
                    // Common
                    ["c0001"] = "Ok",
                    ["c0002"] = "Cancel",

                    // Error
                    ["e0001"] = "An unexpected error has occurred.",
                    // IO
                    ["e0101"] = "The folder can't be found. PATH:{0}",
                    // Database
                    ["e0201"] = "The folder to store the database can't be created.",
                    ["e0202"] = "Create the following folder and add the access right.",
                    ["e0203"] = "An unexpected error occurred whie accessing the database.",
                    // Application
                    ["e1001"] = "An unexpected error occurred while setting the language.",
                    ["e1002"] = "YouTube download process failed.",
                    ["e1003"] = "YouTube analysis failed. The download reservation will be cancelled.",
                    ["e1004"] = "There was a problem with the History display.",
                    ["e1005"] = "This is a live stream so it's not available.",

                    // Warning
                    ["w1001"] = "It is still being downloaded. Do you really finish?",
                    ["w1002"] = "the reserved URL will be resumed after the newxt launch.",
                    ["w1003"] = "'{0}' - {1} has already been saved.",
                    ["w1004"] = "'{0}' The reservation cannot be deleted because it has started or completed.",
                    ["w1005"] = "'{0}' Do you want to delete it from the database?",

                    // Information
                    ["i0101"] = "Do you want to start downloading from the continuation of the last time?",
                    ["i0102"] = "VideoId={0}: It's getting information from YouTube.",
                    ["i0103"] = "'{0}' - {1}, Downloading it.",
                    ["i0104"] = "'{0}' - {1}, Download completed ({2:#,0.0}m).",
                    ["i0111"] = "The download history is obtained from the database.",
                    ["i0112"] = "Searching for keyword '{0}'.",

                    // Text
                    ["xt001"] = "Download history",
                    ["xt002"] = "Light",
                    ["xt003"] = "Dark",
                    ["xt004"] = "Options",
                    ["xt005"] = "Open in Explorer",
                    ["xt006"] = "Optiopns",
                    ["xt007"] = "Title",
                    ["xt008"] = "Author",
                    ["xt009"] = "Update data",

                    // Content
                    ["xc001"] = "Sync. Windows",
                    ["xc002"] = "Color adjustment",
                    ["xc003"] = "Vide (MP4)",
                    ["xc004"] = "Audio (AAC)",
                    ["xc005"] = "Audio (MP3)",

                    // Header
                    ["xh001"] = "Colors",
                    ["xh002"] = "Languages",
                    ["xh003"] = "Save file",
                    ["xh004"] = "No.",
                    ["xh005"] = "Completion",
                    ["xh006"] = "Title",
                    ["xh007"] = "Author",
                    ["xh008"] = "Time",
                    ["xh009"] = "Length",
                    ["xh010"] = "Codec",
                    ["xh011"] = "Resolution",
                    ["xh012"] = "FPS",
                    ["xh013"] = "Kbits",
                    ["xh014"] = "Date",
                    ["xh015"] = "Copy",
                    ["xh016"] = "Browser display",
                    ["xh017"] = "Open in Explorer",
                    ["xh018"] = "Play",
                    ["xh019"] = "Delete reservation",
                    ["xh020"] = "Keyword",
                    ["xh021"] = "Conditions",
                    ["xh022"] = "Create",
                    ["xh023"] = "Update",
                    ["xh024"] = "Term",
                    ["xh025"] = "Delete",

                    // ToolTip
                    ["tt001"] = "Open the GitHub site",
                    ["tt002"] = "Open the Tabito&s blog site",
                    ["tt011"] = "Set to the same mode as Windows",
                    ["tt012"] = "Change the color of the window",
                    ["tt013"] = "Change the color of the primery",
                    ["tt014"] = "Change the color of the secondary",
                    ["tt015"] = "Adjust the color as needed",
                    ["tt016"] = "Select a language",
                    ["tt017"] = "Download start",
                    ["tt018"] = "Copy the URL and it will start downloading automatically",
                    ["tt019"] = "Update database information",

                    // HintAssist
                    ["ha011"] = "Window",
                    ["ha012"] = "Primery",
                    ["ha013"] = "Secondary",
                    ["ha014"] = "Language",
                    ["ha015"] = "Enter or paste the YouTube URL",
                    ["ha016"] = "Please enter a search keyword",
                }
            };

        /// <summary>
        /// メッセージ出力
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetMessage(string key)
        {
            var lang = Languages.FirstOrDefault(p => p.Name.Equals(CurrentLang));
            switch (lang)
            {
                case null:
                    _logger.Error($"Not found Language '{CurrentLang}'.");
                    return Default.Messages[key];
                default:
                    if (lang.Messages.ContainsKey(key)) return lang.Messages[key];
                    _logger.Error($"Not found Language-Key = '{key}'.");
                    return Default.Messages[key];
            }
        }

        /// <summary>
        /// メッセージ出力(フォーマット書式)
        /// </summary>
        /// <param name="key"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public string GetMessage(string key, params object?[] args)
        {
            var lang = Languages.FirstOrDefault(p => p.Name.Equals(CurrentLang));
            switch (lang)
            {
                case null:
                    _logger.Error($"Not found Language '{CurrentLang}'.");
                    return string.Format(Default.Messages[key], args);
                default:
                    if (lang.Messages.ContainsKey(key)) return string.Format(lang.Messages[key], args);
                    _logger.Error($"Not found Language-Key = '{key}'.");
                    return string.Format(Default.Messages[key], args);
            }
        }
    }
}
