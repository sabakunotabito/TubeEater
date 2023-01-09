using System;
using System.Collections.Generic;
using System.Security.RightsManagement;

namespace TubeEater
{
    /// <summary>
    /// アプリケーション設定
    /// </summary>
    public class SettingsData
    {
        public bool IsWindowsMode { get; set; }                         // Windowsモード
        public bool IsDark { get; set; }                                // ライト or ダーク
        public string MahThemeColor { get; set; } = String.Empty;       // テーマ色
        public string PrimaryColor { get; set; } = String.Empty;        // プライマリ色
        public string SecondaryColor { get; set; } = String.Empty;      // セカンダリ色
        public bool IsColorAdjustment { get; set; }                     // 色調整
        public string SelectedLanguage { get; set; } = String.Empty;    // 言語(Japanese)
        public string SelectedLocale { get; set; } = String.Empty;      // ロケール(ja-JP)
        public bool IsOverWrite { get; set; }                           // ファイル上書き(設定項目は削除)
        public bool? IsVideoMp4 { get; set; } = null;
        public bool? IsAudioAac { get; set; } = null;
        public bool? IsAudioMp3 { get; set; } = null;
        public List<MediaData> RemainedData { get; set; } = new();      // 予約だけでダウンロードしていない
    }
}
