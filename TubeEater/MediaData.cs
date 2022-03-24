using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VideoLibrary;

namespace TubeEater
{
    /// <summary>
    /// ダウンロード予約用
    /// </summary>
    public class MediaData
    {
        public string Id { get; set; } = string.Empty;          // YouTube Video-ID + Codec
        public string Title { get; set; } = string.Empty;       // タイトル
        public string Author { get; set; } = string.Empty;      // 著者
        public TimeSpan TimeSpan { get; set; }                  // 再生時間
        public long ContentLength { get; set; }                 // データサイズ(ファイルサイズ)
        public string Codec { get; set; } = string.Empty;       // コーデック(MP4,AAC,MP3)
        public int Resolution { get; set; }                     // 解像度
        public int Fps { get; set; }                            // FPS
        public int AudioBitrate { get; set; }                   // ビットレート
        public Uri? YouTubeUri { get; set; }                    // YouTube URL
        public DateTime Created { get; set; }                   // 作成日

        [JsonIgnore]
        public bool IsRunngin { get; set; }                     // ダウンロードを開始した
        public bool IsCompleted { get; set; }                   // 完了した？
        public bool HasError { get; set; }
        public bool IsCreate { get; set; }                      // ファイルを作成(MP4,AAC)する？
        public bool IsConvert { get; set; }                     // コンバート(MP3)する？
        public string? SavePath { get; set; }                   // 保存先のパス

        // YouTubeVideo の複数情報(失敗時は次でリトライ)
        [JsonIgnore]
        public List<YouTubeVideo>? Items { get; set; } = null;
    }
}
