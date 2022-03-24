using MahApps.Metro.Controls.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static TubeEater.MediaContext;

namespace TubeEater
{
    /// <summary>
    /// HistoryWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class HistoryWindow : MahApps.Metro.Controls.MetroWindow
    {
        // NLog
        private Logger _logger = LogManager.GetCurrentClassLogger();

        // 履歴データ
        private ObservableCollection<MediaItem> _resources = new();

        // メッセージ管理
        public MessageManager MsgMgr { get; set; } = new();

        public HistoryWindow(MainWindow main)
        {
            InitializeComponent();

            try
            {
                // Icon
                using var icon = Properties.Resources.ResourceManager.GetObject("TubuEater") as System.Drawing.Icon;
                if (icon != null) Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                MsgMgr = main.MsgMgr;
                SetXamlLanguage();

                // 表示後
                ContentRendered += (_, _) =>
                {
                    var info = MsgMgr.GetMessage("i0111");
                    _logger.Info(info);
                    snackbarMessage.MessageQueue?.Enqueue(info);
                    progressBarDownload.Visibility = Visibility.Visible;
                    progressBarDownload.IsIndeterminate = true;
                    progressBarDownload.Value = 0;

                    // 履歴情報の取得
                    dataGridHistory.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
                    main.Context.MediaItems?.ToList().ForEach(p => _resources.Add(p));
                    SearchKeyword();
                };

                // 履歴の再読込み
                buttonSync.Click += (_, _) =>
                {
                    _resources.Clear();
                    main.Context.MediaItems?.ToList().ForEach(p => _resources.Add(p));
                    SearchKeyword();
                };

                // 検索ボックス
                textBoxSearch.KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Enter) SearchKeyword();
                };

                // 検索ボタン
                buttonSearch.Click += (_, _) => SearchKeyword();

                // YouTubeのURLをブラウザで開く
                menuItemOpenBrowser.Click += (_, _) =>
                {
                    if (dataGridHistory.SelectedCells.Count == 0) return;
                    var item = (MediaItem)dataGridHistory.SelectedCells[0].Item;
                    Tools.StartProcess(item.YouTubeUri?.AbsoluteUri);
                };

                // ファイルの格納場所を開く
                menuItemOpenExplorer.Click += (_, _) =>
                {
                    if (dataGridHistory.SelectedCells.Count == 0) return;
                    var item = (MediaItem)dataGridHistory.SelectedCells[0].Item;
                    var baseDir = (0 < item.Resolution) ? main.VideoPath : main.AudioPath;
                    var dirName = string.Join("_", item.Author.Split(Path.GetInvalidPathChars()));
                    var path = Path.Combine(baseDir, dirName);
                    if (!Directory.Exists(path))
                    {
                        var error = MsgMgr.GetMessage("e0101", path);
                        _logger.Error(error);
                        snackbarMessage.MessageQueue?.Enqueue(error);
                        return;
                    }
                    Tools.OpenFolder(path);
                };

                //// ダウンロード済みの再生（なぜか失敗するため保留）
                //menuItemPlay.Click += (_, _) =>
                //{
                //    if (dataGridHistory.SelectedCells == null) return;
                //    var item = (MediaItem)dataGridHistory.SelectedCells[0].Item;
                //    if (item.SavePath == null) return;
                //    Tools.StartProcess(item.SavePath);
                //};

                // 行削除
                menuItemDelete.Click += async (_, _) =>
                {
                    if (dataGridHistory.SelectedCells.Count == 0) return;
                    var item = (MediaItem)dataGridHistory.SelectedCells[0].Item;

                    var warn = MsgMgr.GetMessage("w1005", item.Title);
                    var result = await this.ShowMessageAsync(Title, warn, MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings { AffirmativeButtonText = MsgMgr.GetMessage("c0001"), NegativeButtonText = MsgMgr.GetMessage("c0002") });
                    if (result == MessageDialogResult.Negative) return;

                    // DBから削除する
                    main.Context.MediaItems?.Remove(item);
                    await main.Context.SaveChangesAsync();

                    // 表からデータを削除して、画面を更新する
                    _resources.Remove(item);
                    SearchKeyword();
                };

                // テーマ変更
                main.Designs?.SetTheme(this);
            }
            catch (Exception ex)
            {
                var error = new StringBuilder(MsgMgr.GetMessage("e1004"));
                _logger.Error(ex, error.ToString());
                error.Append(ex.Message);
                _ = this.ShowMessageAsync("ERROR", error.ToString()).Result;
            }
        }

        /// <summary>
        /// XAMLへの言語設定
        /// </summary>
        public void SetXamlLanguage()
        {
            MaterialDesignThemes.Wpf.HintAssist.SetHint(textBoxSearch, MsgMgr.GetMessage("ha016"));
            textBlockOptions.Text = MsgMgr.GetMessage("xt006");
            groupBoxKeywords.Header = MsgMgr.GetMessage("xh020");
            textBlockTitle.Text = MsgMgr.GetMessage("xt007");
            textBlockAuthor.Text = MsgMgr.GetMessage("xt008");
            groupBoxRefine.Header = MsgMgr.GetMessage("xh021");
            checkBoxVideoMp4.Content = MsgMgr.GetMessage("xc003");
            checkBoxAudioAac.Content = MsgMgr.GetMessage("xc004");
            checkBoxAudioMp3.Content = MsgMgr.GetMessage("xc005");
            //textBlockSync.Text = MsgMgr.GetMessage("xt009");
            buttonSync.ToolTip = MsgMgr.GetMessage("tt019");
            // DataGrid
            dgtColumnNumber.Header = MsgMgr.GetMessage("xh004");
            //dgtClumnCompleted.Header = MsgMgr.GetMessage("x1031");
            dgtColumnTitle.Header = MsgMgr.GetMessage("xh006");
            dgtColumnAuthor.Header = MsgMgr.GetMessage("xh007");
            dgtColumnTimeSpan.Header = MsgMgr.GetMessage("xh008");
            dgtColumnLength.Header = MsgMgr.GetMessage("xh009");
            dgtColumnCodec.Header = MsgMgr.GetMessage("xh010");
            dgtColumnResolution.Header = MsgMgr.GetMessage("xh011");
            dgtColumnFps.Header = MsgMgr.GetMessage("xh012");
            dgtColumnAudioBitrate.Header = MsgMgr.GetMessage("xh013");
            //dgtColumnDate.Header = MsgMgr.GetMessage("xh014");
            dgtColumnCreate.Header = MsgMgr.GetMessage("xh022");
            dgtColumnUpdate.Header = MsgMgr.GetMessage("xh023");
            dgtColumnTerm.Header = MsgMgr.GetMessage("xh024");

            menuItemCopy.Header = MsgMgr.GetMessage("xh015");
            menuItemOpenBrowser.Header = MsgMgr.GetMessage("xh016");
            menuItemOpenExplorer.Header = MsgMgr.GetMessage("xh017");
            //menuItemPlay.Header = MsgMgr.GetMessage("xh018");
            menuItemDelete.Header = MsgMgr.GetMessage("xh025");
        }

        /// <summary>
        /// キーワード検索
        /// </summary>
        private void SearchKeyword()
        {
            if (0 < textBoxSearch.Text.Trim().Length)
            {
                var info = MsgMgr.GetMessage("i0112", textBoxSearch.Text);
                _logger.Info(info);
                snackbarMessage.MessageQueue?.Enqueue(info);
            }
            progressBarDownload.Visibility = Visibility.Visible;
            progressBarDownload.IsIndeterminate = true;
            progressBarDownload.Value = 0;

            List<MediaItem> mediaItems;
            var keyword = textBoxSearch.Text.ToLower();
            var allCodecs = (checkBoxVideoMp4.IsChecked ?? false) && (checkBoxAudioAac.IsChecked ?? false) && (checkBoxAudioMp3.IsChecked ?? false);
            if (allCodecs)
            {
                mediaItems = toggleButtonIsDark.IsChecked ?? false
                    ? _resources.Where(p => p.Author.ToLower().Contains(keyword)).ToList()
                    : _resources.Where(p => p.Title.ToLower().Contains(keyword)).ToList();
            }
            else
            {
                var pool = new List<string>();
                if (checkBoxVideoMp4.IsChecked ?? false) pool.Add("MP4");
                if (checkBoxAudioAac.IsChecked ?? false) pool.Add("AAC");
                if (checkBoxAudioMp3.IsChecked ?? false) pool.Add("MP3");
                var codecs = string.Join("|", pool);
                mediaItems = toggleButtonIsDark.IsChecked ?? false
                    ? _resources.Where(p => codecs.Contains(p.Codec) && p.Author.ToLower().Contains(keyword)).ToList()
                    : _resources.Where(p => codecs.Contains(p.Codec) && p.Title.ToLower().Contains(keyword)).ToList();
            }
            dataGridHistory.ItemsSource = mediaItems;
            textBlockCounter.Text = $"{mediaItems.Count:#,0}";

            progressBarDownload.IsIndeterminate = false;
            progressBarDownload.Value = 100;
            progressBarDownload.Visibility = Visibility.Collapsed;
        }
    }
}
