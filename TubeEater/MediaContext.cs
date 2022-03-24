using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace TubeEater
{
    /// <summary>
    /// EntiryFrameworkCorey制御 [NotMapped]
    /// </summary>
    public class MediaContext : DbContext
    {
        /// <summary>
        /// アプリケーション設定をJSONで格納する
        /// </summary>
        public class SettingsItem
        {
            [Key]
            public int Id { get; set; }                             // ID
            public string? JsonText { get; set; }                   // JSONデータ
        }

        /// <summary>
        /// ダウンロード履歴用
        /// </summary>
        public class MediaItem
        {
            [Key]
            public string Id { get; set; } = string.Empty;                  // TubeEater-ID(YouTube-ID + Codec)
            [Required]
            public string Title { get; set; } = string.Empty;               // タイトル
            [Required]
            public string Author { get; set; } = string.Empty;              // 著書
            public TimeSpan TimeSpan { get; set; }                          // 再生時間
            public long ContentLength { get; set; }                         // データサイズ(ファイルサイズ)
            [Required]
            public string Codec { get; set; } = string.Empty;               // コーデック
            public int Resolution { get; set; }                             // 解像度
            public int Fps { get; set; }                                    // FPS
            public int AudioBitrate { get; set; }                           // ビットレート
            public Uri? YouTubeUri { get; set; }                            // YouTube URL
            public string? SavePath { get; set; }                           // 保存先のパス
            public DateTime Created { get; set; }                           // 作成日
            public DateTime Updated { get; set; }                           // 更新日
            public TimeSpan Span  => Updated - Created;                     // 更新期間
            public string? Memo { get; set; }                               // メモ(予備)
        }

        public string DataSource { get; set; } = string.Empty;          // SQLiteデータベースのパス

        public DbSet<SettingsItem>? SettingsItems { get; set; }             // アプリケーション設定
        public DbSet<MediaItem>? MediaItems { get; set; }                   // ダウンロード履歴

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Data Source={DataSource}");
    }
}
