using MahApps.Metro.Controls.Dialogs;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualBasic;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TagLib.Gif;
using VideoLibrary;

namespace TubeEater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private const string GitHubUri = @"https://github.com/sabakunotabito/tubeeater";
        private const string TabitoBlogUri = @"https://sabakunotabito.hatenablog.com/entry/2022/02/28/012442";

        // NLog
        private Logger _logger = LogManager.GetCurrentClassLogger();

        // データベースエラー
        private bool _hasDatabaseError = false;

        // 自動クリップボードコピー
        private bool _isAutoCopy = false;

        // キー入力から受け付けたURL一覧
        private HashSet<string> _inputUris = new HashSet<string>();

        // ダウンロード予約したURL一覧
        private HashSet<string> _reservedUris = new HashSet<string>();

        // メディア情報
        private readonly ObservableCollection<MediaData> _resources = new();

        // 監視
        private readonly DispatcherTimer _moniter = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(97) };

        // 予約処理中の判定フラグ
        private bool _isBookingLock = false;

        // URL予約受付キュー
        private readonly ConcurrentQueue<string> _inputQueue = new();

        // ダウンロードキュー
        private readonly ConcurrentQueue<MediaData> _downloadQueue = new();

        // キューから取り出したアイテム
        private MediaData? _currentData = null;

        // ダウンロードロック
        private bool _isDownloadLock = false;

        // ビデオ格納先
        public string VideoPath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)}\\{App.AsmName}";

        // オーディオ格納先
        public string AudioPath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}\\{App.AsmName}";

        // ダウンロード履歴
        private HistoryWindow? _historyWindow = null;

        // メッセージ管理
        public MessageManager MsgMgr { get; set; } = new();

        // アプリケーション設定
        public SettingsData Configs { get; set; } = new()
        {
            IsWindowsMode = true,
            MahThemeColor = DesignManager.DefaultMahAppsTheme,
            PrimaryColor = DesignManager.DefaultPrimaryColor,
            SecondaryColor = DesignManager.DefaultSecondaryColor,
            IsOverWrite = false
        };

        // Mahapps & MaterialDesign
        public DesignManager Designs { get; set; } = new(new SettingsData());

        // Entity Framework(SQLite)
        public MediaContext Context { get; } = new();

        public MainWindow()
        {
            _logger.Debug(MethodBase.GetCurrentMethod()?.Name);

            InitializeComponent();

            try
            {
                // 終了
                Closing += async (_, e) => await ClosingApplication(e);

                // 開始
                Loaded += async (_, e) => await StartingApplication();

                // 画面表示後
                ContentRendered += (_, _) => _moniter.Start();

                // 監視
                _moniter.Tick += async (_, _) => await Monitoring();

                // GitHubボタン
                buttonJumpGitHub.Click += (_, _) => Tools.StartProcess(GitHubUri);

                // GitHubボタン
                buttonJumpTabitoBlog.Click += (_, _) => Tools.StartProcess(TabitoBlogUri);

                // テキストボックス
                textBoxUrl.KeyDown += async (s, e) =>
                {
                    if (e.Key == Key.Enter) await BookYoutubeAsync(((TextBox)s).Text.Trim());
                };

                // Downloadボタン
                buttonDownload.Click += async (_, _) => await BookYoutubeAsync(textBoxUrl.Text.Trim());

                // 自動ダウンロード選択スイッチ
                toggleButtonAutoDownload.Click += (s, _) => gridManual.IsEnabled = !(((ToggleButton)s).IsChecked ?? false);

                // ビデオ(MP4)選択スイッチ
                checkBoxVideoMp4.Click += (s, _) =>
                {
                    var tb = (ToggleButton)s;
                    var isZero = (tb.IsChecked ?? false) || (checkBoxAudioAac.IsChecked ?? false) || (checkBoxAudioMp3.IsChecked ?? false);
                    if (!isZero) tb.IsChecked = true;
                };

                // オーディオ(AAC)選択スイッチ
                checkBoxAudioAac.Click += (s, _) =>
                {
                    var isZero = (checkBoxVideoMp4.IsChecked ?? false) || (((ToggleButton)s).IsChecked ?? false) || (checkBoxAudioMp3.IsChecked ?? false);
                    if (!isZero) checkBoxVideoMp4.IsChecked = true;
                };

                // オーディオ(MP3)選択スイッチ
                checkBoxAudioMp3.Click += (s, _) =>
                {
                    var isZero = (checkBoxVideoMp4.IsChecked ?? false) || (((ToggleButton)s).IsChecked ?? false) || (checkBoxAudioAac.IsChecked ?? false);
                    if (!isZero) checkBoxVideoMp4.IsChecked = true;
                };

                // 保存先を開く
                buttonOpen.Click += (_, _) =>
                {
                    if (checkBoxVideoMp4.IsChecked ?? false)
                    {
                        if (Directory.Exists(VideoPath)) Tools.StartProcess(VideoPath);
                    }
                    if ((checkBoxAudioAac.IsChecked ?? false) || (checkBoxAudioMp3.IsChecked ?? false))
                    {
                        if (Directory.Exists(AudioPath)) Tools.StartProcess(AudioPath);
                    }
                };

                // 履歴の表示
                buttonHistory.Click += (_, _) =>
                {
                    if (_hasDatabaseError) return;
                    _logger.Info("Open download history.");
                    drawerHostMain.IsLeftDrawerOpen = false;
                    if (_historyWindow == null || !_historyWindow.IsLoaded) _historyWindow = new HistoryWindow(this);
                    _historyWindow.Show();
                    _historyWindow.Activate();
                };

                // ライト or ダーク
                toggleButtonIsDark.Click += (_, _) => SetTheme();

                // Windows モードに同期する
                checkBoxIsWindowsMode.Click += (s, _) =>
                {
                    var value = ((CheckBox)s).IsChecked ?? false;
                    Configs.IsWindowsMode = value;
                    if (value) Configs.IsDark = Designs.IsInheritDark;
                    toggleButtonIsDark.IsEnabled = !value;
                };

                // テーマ
                comboBoxTheme.SelectionChanged += (_, _) => SetTheme();

                // プライマリ
                comboBoxPrimary.SelectionChanged += (s, _) => Designs.SetPrimaryColor((DesignManager.ColorItem)((ComboBox)s).SelectedItem);

                // セカンダリ
                comboBoxSecondary.SelectionChanged += (s, _) => Designs.SetSecondaryColor((DesignManager.ColorItem)((ComboBox)s).SelectedItem);

                // 色調整
                checkBoxIsColorAdjustment.Click += (s, _) =>
                {
                    Configs.IsColorAdjustment = ((CheckBox)s).IsChecked ?? false;
                    Designs.SetColors((DesignManager.ColorItem)comboBoxPrimary.SelectedItem, (DesignManager.ColorItem)comboBoxSecondary.SelectedItem);
                };

                // 言語
                comboBoxLanguage.SelectionChanged += (s, _) =>
                {
                    var lang = ((MessageManager.LangData)((ComboBox)s).SelectedItem);
                    MsgMgr.CurrentLang = lang.Name;
                    Configs.SelectedLanguage = lang.Name;
                    Configs.SelectedLocale = lang.Locale;
                    SetXamlLanguage();
                    if (_historyWindow != null && _historyWindow.IsLoaded) _historyWindow.SetXamlLanguage();
                };

                // ブラウザで開く
                menuItemOpenBrowser.Click += (_, _) =>
                {
                    if (dataGridReserve.SelectedCells.Count == 0) return;
                    var item = (MediaData)dataGridReserve.SelectedCells[0].Item;
                    Tools.StartProcess(item.YouTubeUri?.AbsoluteUri);
                };

                // 格納場所を開く
                menuItemOpenExplorer.Click += (_, _) =>
                {
                    if (dataGridReserve.SelectedCells.Count == 0) return;
                    var item = (MediaData)dataGridReserve.SelectedCells[0].Item;
                    if (item.SavePath == null) return;
                    var path = new FileInfo(item.SavePath).DirectoryName;
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
                //    if (dataGridReserve.SelectedCells == null) return;
                //    var item = (MediaData)dataGridReserve.SelectedCells[0].Item;
                //    if (!item.IsCompleted || item.SaveFile == null) return;
                //    ProcessStart(item.SaveFile.FullName);
                //};

                // 予約をキャンセルする
                menuItemDelete.Click += (_, _) => CancelReservation();
            }
            catch (Exception ex)
            {
                var errmsg = new StringBuilder(MsgMgr.GetMessage("e0001"));
                _logger.Error(ex, errmsg.ToString());
                errmsg.Append(ex.Message);
                _ = this.ShowMessageAsync("ERROR", errmsg.ToString()).Result;
            }

        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task ClosingApplication(CancelEventArgs e)
        {
            var isExit = false;     // 終了判定
            e.Cancel = true;        // 終了中断
            try
            {
                // 監視の終了
                _moniter.Stop();

                // ダウンロード中
                var remains = _resources.Where(p => !p.IsCompleted && !p.HasError).ToList();
                if (0 < remains.Count)
                {
                    var endmsg = new StringBuilder();
                    endmsg.AppendLine(MsgMgr.GetMessage("w1001"));
                    endmsg.Append(MsgMgr.GetMessage("w1002"));
                    var result = await this.ShowMessageAsync(Title, endmsg.ToString(), MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings { AffirmativeButtonText = MsgMgr.GetMessage("c0001"), NegativeButtonText = MsgMgr.GetMessage("c0002") });
                    if (result == MessageDialogResult.Negative)
                    {
                        _moniter.Start();
                        return;
                    }
                    _logger.Info("Force termination.");

                    // 予約待ちのダウンロードURLを次回に備えて保存する
                    Configs.RemainedData = new List<MediaData>();
                    foreach (var remain in remains)
                    {
                        _logger.Info($"Remain:'{remain.Title}', '{remain.Author}'.");
                        remain.Items = null;
                        Configs.RemainedData.Add(remain);
                    }
                }

                // チェックボックスの値を保存する
                Configs.IsVideoMp4 = checkBoxVideoMp4.IsChecked;
                Configs.IsAudioAac = checkBoxAudioAac.IsChecked;
                Configs.IsAudioMp3 = checkBoxAudioMp3.IsChecked;

                // 設定情報の読込み
                _logger.Info("Read the setting information from the database.");
                PutSettingsItem(1, Configs);                // 全般設定
                PutSettingsItem(2, MsgMgr.Languages);       // 言語設定
                await Context.SaveChangesAsync();           // DB保存

                isExit = true;
                e.Cancel = false;                           // 終了させる
            }
            catch (Exception ex)
            {
                _logger.Error(ex, MsgMgr.GetMessage("e0001"));
                e.Cancel = false;                           // 終了させる
            }
            if (isExit) Application.Current.Shutdown();     // アプリケーション終了
        }

        /// <summary>
        /// 設定情報の書き込み
        /// </summary>
        /// <param name="id"></param>
        /// <param name="obj"></param>
        private void PutSettingsItem(int id, object obj)
        {
            var conf = Context.SettingsItems?.FirstOrDefault(p => p.Id == id);
            if (conf == null)
            {
                Context.SettingsItems?.Add(new MediaContext.SettingsItem() { Id = id, JsonText = JsonConvert.SerializeObject(obj) });
            }
            else
            {
                conf.JsonText = JsonConvert.SerializeObject(obj);
            }
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        /// <returns></returns>
        private async Task StartingApplication()
        {
            // Icon
            using var icon = Properties.Resources.ResourceManager.GetObject("TubuEater") as System.Drawing.Icon;
            if (icon != null) Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // プロダクト情報を表示する
            var assembly = Assembly.GetExecutingAssembly().GetName();
            textBlockProduct.Text = $"{assembly.Name} Ver.{assembly.Version}";

            // データベース格納フォルダを作成できるかを確認する
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? @".";
            var path = Path.Combine(basePath, "data");
            try
            {
                var dir = new DirectoryInfo(path);
                if (!dir.Exists) dir.Create();
                Context.DataSource = Path.Combine(path, $"{assembly.Name}.db");
            }
            catch(Exception ex)
            {
                var error = new StringBuilder();
                error.AppendLine(MsgMgr.GetMessage("e0201"));
                error.AppendLine(MsgMgr.GetMessage("e0202"));
                error.Append($"{path}");
                _logger.Error(ex, error.ToString());
                await this.ShowMessageAsync("ERROR", error.ToString());
                Application.Current.Shutdown();
            }

            try
            {
                // Database 作成
                if (!Context.Database.GetService<IRelationalDatabaseCreator>().Exists()) Context.Database.EnsureCreated();

                // 全般設定
                var json = Context.SettingsItems?.Where(p => p.Id == 1).Select(p => p.JsonText).FirstOrDefault();
                if (json != null)
                {
                    var instance = JsonConvert.DeserializeObject<SettingsData>(json);
                    if (instance != null) Configs = instance;
                }

                // 言語設定
                json = Context.SettingsItems?.Where(p => p.Id == 2).Select(p => p.JsonText).FirstOrDefault();
                if (json != null)
                {
                    var instance = JsonConvert.DeserializeObject<List<MessageManager.LangData>>(json);
                    if (instance != null) MsgMgr.Languages.AddRange(instance);
                }

                // ダウンロード履歴をダミーで取得(DBエラー検出用)
                var dmy = Context.MediaItems?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _hasDatabaseError = true;
                var error = MsgMgr.GetMessage("e0203");
                _logger.Error(ex, error);
                await this.ShowMessageAsync("ERROR", error);
                buttonHistory.IsEnabled = false;        // 履歴不可で続行
            }

            // 言語設定
            await SetMultiLanguages(basePath);

            // カラー管理
            Designs = new DesignManager(Configs);
            Designs.Initialize(this);

            // MaterialDesign Xaml Toolkit の色設定
            comboBoxPrimary.ItemsSource = Designs.MdxToolItems;
            comboBoxSecondary.ItemsSource = Designs.MdxToolItems;

            // 各コントロール初期設定
            checkBoxIsWindowsMode.IsChecked = Configs.IsWindowsMode;
            toggleButtonIsDark.IsEnabled = !Configs.IsWindowsMode;
            toggleButtonIsDark.IsChecked = Configs.IsDark;
            comboBoxTheme.ItemsSource = Designs.MahAppsItems;
            comboBoxTheme.SelectedIndex = Designs.MahAppsItems.Where(p => p.Name.Equals(Configs.MahThemeColor, StringComparison.Ordinal)).Select(p => p.Value).First();
            comboBoxPrimary.SelectedIndex = Designs.MdxToolItems.Where(p => p.Name.ToLower().Equals(Configs.PrimaryColor.ToLower(), StringComparison.Ordinal)).Select(p => p.Value).First();
            comboBoxSecondary.SelectedIndex = Designs.MdxToolItems.Where(p => p.Name.ToLower().Equals(Configs.SecondaryColor.ToLower(), StringComparison.Ordinal)).Select(p => p.Value).First();
            checkBoxIsColorAdjustment.IsChecked = Configs.IsColorAdjustment;
            if (Configs.IsVideoMp4 != null) checkBoxVideoMp4.IsChecked = Configs.IsVideoMp4;
            if (Configs.IsAudioAac != null) checkBoxAudioAac.IsChecked = Configs.IsAudioAac;
            if (Configs.IsAudioMp3 != null) checkBoxAudioMp3.IsChecked = Configs.IsAudioMp3;

            // 予約残の復元
            await RestoreRemainedData();

            // データ表示
            dataGridReserve.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            dataGridReserve.ItemsSource = _resources;
        }

        /// <summary>
        /// 前回の予約残を復元する
        /// </summary>
        /// <returns></returns>
        private async Task RestoreRemainedData()
        {
            if (Configs.RemainedData.Count == 0) return;

            var result = await this.ShowMessageAsync(Title, MsgMgr.GetMessage("i0101"), MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = MsgMgr.GetMessage("c0001"), NegativeButtonText = MsgMgr.GetMessage("c0002"), DefaultText = MsgMgr.GetMessage("c0001") });
            if (result != MessageDialogResult.Negative)
            {
                // データを復元する
                _logger.Info("Resume download.");
                IsEnabled = false;
                foreach (var data in Configs.RemainedData)
                {
                    var videos = await YouTube.Default.GetAllVideosAsync(data.YouTubeUri?.AbsoluteUri);  // YouTubeから情報取得
                    (data.Items, _, _) = data.Codec.Equals("MP4") ? GetVideoInfo(videos, data.Codec) : GetAudioInfo(videos, data.Codec);
                    _downloadQueue.Enqueue(data);       // 予約キューに格納
                    _resources.Add(data);               // メディアデータに格納
                }
                IsEnabled = true;
            }
            Configs.RemainedData.Clear();
        }

        /// <summary>
        /// マルチ言語の設定
        /// </summary>
        /// <param name="basePath"></param>
        private async Task SetMultiLanguages(string basePath)
        {
            try
            {
                // 言語ファイルが格納されていれば、読み込んで設定する
                var dir = new DirectoryInfo(Path.Combine(basePath, "Languages"));
                //if (!dir.Exists) return;                                // 意図的に削除した？
                if (dir.Exists)
                {
                    foreach (var file in dir.GetFiles("*.json"))
                    {
                        try
                        {
                            var json = System.IO.File.ReadAllText(file.FullName);
                            var data = JsonConvert.DeserializeObject<MessageManager.LangData>(json);
                            if (data == null) continue;
                            data.Update = file.LastWriteTime;
                            var lang = MsgMgr.Languages.Where(p => p.Name == data.Name).FirstOrDefault();
                            switch (lang)
                            {
                                case null:
                                    MsgMgr.Languages.Add(data);             // 新規追加
                                    break;
                                default:
                                    if (lang.Update < file.LastWriteTime)   // 更新日が新しい
                                    {
                                        MsgMgr.Languages.Remove(lang);      // 現行を削除する
                                        MsgMgr.Languages.Add(data);         // 最新を追加する
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = MsgMgr.GetMessage("e1001");
                            _logger.Error(ex, error);
                            await this.ShowMessageAsync("ERROR", error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var error = MsgMgr.GetMessage("e1001");
                _logger.Error(ex, error);
                await this.ShowMessageAsync("ERROR", error);
            }

            // デフォルト言語がなければ追加する
            if (!MsgMgr.Languages.Where(p => p.Name.Equals(MessageManager.DefaultLanguage)).Any()) MsgMgr.Languages.Add(MsgMgr.Default);

            // ComboBox に言語を設定する
            comboBoxLanguage.ItemsSource = MsgMgr.Languages;

            // 前回設定した言語がある
            if (0 < Configs.SelectedLanguage.Length)
            {
                var idx = 0;
                for (int i = 0; i < MsgMgr.Languages.Count; i++)
                {
                    if (MsgMgr.Languages[i].Name.Equals(MsgMgr.CurrentLang))
                    {
                        idx = i;
                        break;
                    }
                }
                if (MsgMgr.Default.Messages.Count != MsgMgr.Languages[idx].Messages.Count)
                {
                    foreach (var kv in MsgMgr.Default.Messages)
                    {
                        var key = kv.Key;
                        if (MsgMgr.Languages[idx].Messages.ContainsKey(key)) continue;
                        MsgMgr.Languages[idx].Messages.Add(key, kv.Value);
                    }
                }

                var lang = MsgMgr.Languages.FirstOrDefault(p => p.Name.Equals(Configs.SelectedLanguage));
                if (lang != null)
                {
                    comboBoxLanguage.SelectedItem = lang;
                    return;
                }
            }

            // 現在のロケールを反映する
            var items = new List<string>() { CultureInfo.CurrentCulture.Name.ToLower() };
            items.AddRange(CultureInfo.CurrentCulture.Name.Split("-").Select(p => p.ToLower()));
            var currentLang = MsgMgr.Languages.FirstOrDefault(p => items.Contains(p.Locale.ToLower()));
            switch (currentLang)
            {
                case null:
                    comboBoxLanguage.SelectedIndex = 0;
                    break;
                default:
                    comboBoxLanguage.SelectedItem = currentLang;
                    Configs.SelectedLanguage = currentLang.Name;
                    break;
            }
        }

        /// <summary>
        /// Xamlへの言語設定
        /// </summary>
        private void SetXamlLanguage()
        {
            if (comboBoxLanguage.SelectedItem != null) MsgMgr.CurrentLang = ((MessageManager.LangData)comboBoxLanguage.SelectedItem).Name;
            textBoxHistory.Text = MsgMgr.GetMessage("xt001");
            groupBoxColor.Header = MsgMgr.GetMessage("xh001");
            textBlockLight.Text = MsgMgr.GetMessage("xt002");
            textBlockDark.Text = MsgMgr.GetMessage("xt003");
            checkBoxIsWindowsMode.Content = MsgMgr.GetMessage("xc001");
            checkBoxIsWindowsMode.ToolTip = MsgMgr.GetMessage("tt011");
            comboBoxTheme.ToolTip = MsgMgr.GetMessage("tt012");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(comboBoxTheme, MsgMgr.GetMessage("ha011"));
            comboBoxPrimary.ToolTip = MsgMgr.GetMessage("tt013");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(comboBoxPrimary, MsgMgr.GetMessage("ha012"));
            comboBoxSecondary.ToolTip = MsgMgr.GetMessage("tt014");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(comboBoxSecondary, MsgMgr.GetMessage("ha013"));
            checkBoxIsColorAdjustment.Content = MsgMgr.GetMessage("xc002");
            checkBoxIsColorAdjustment.ToolTip = MsgMgr.GetMessage("tt015");
            groupBoxLanguage.Header = MsgMgr.GetMessage("xh002");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(comboBoxLanguage, MsgMgr.GetMessage("ha014"));
            comboBoxLanguage.ToolTip= MsgMgr.GetMessage("tt016");
            textBlockOption.Text = MsgMgr.GetMessage("xt004");
            groupBoxSaveFile.Header = MsgMgr.GetMessage("xh003");
            checkBoxVideoMp4.Content = MsgMgr.GetMessage("xc003");
            checkBoxAudioAac.Content = MsgMgr.GetMessage("xc004");
            checkBoxAudioMp3.Content = MsgMgr.GetMessage("xc005");
            textBlockOpen.Text = MsgMgr.GetMessage("xt005");
            MaterialDesignThemes.Wpf.HintAssist.SetHint(textBoxUrl, MsgMgr.GetMessage("ha015"));
            buttonDownload.ToolTip = MsgMgr.GetMessage("tt017");
            toggleButtonAutoDownload.ToolTip = MsgMgr.GetMessage("tt018");
            dgtColumnNumber.Header = MsgMgr.GetMessage("xh004");
            dgtClumnCompleted.Header = MsgMgr.GetMessage("xh005");
            dgtColumnTitle.Header = MsgMgr.GetMessage("xh006");
            dgtColumnAuthor.Header = MsgMgr.GetMessage("xh007");
            dgtColumnTimeSpan.Header = MsgMgr.GetMessage("xh008");
            dgtColumnLength.Header = MsgMgr.GetMessage("xh009");
            dgtColumnCodec.Header = MsgMgr.GetMessage("xh010");
            dgtColumnResolution.Header = MsgMgr.GetMessage("xh011");
            dgtColumnFps.Header = MsgMgr.GetMessage("xh012");
            dgtColumnAudioBitrate.Header = MsgMgr.GetMessage("xh013");
            dgtColumnDate.Header = MsgMgr.GetMessage("xh014");
            menuItemCopy.Header = MsgMgr.GetMessage("xh015");
            menuItemOpenBrowser.Header = MsgMgr.GetMessage("xh016");
            menuItemOpenExplorer.Header = MsgMgr.GetMessage("xh017");
            //menuItemPlay.Header = MsgMgr.GetMessage("xh018");
            menuItemDelete.Header = MsgMgr.GetMessage("xh019");
            buttonJumpGitHub.ToolTip = MsgMgr.GetMessage("tt001");
            buttonJumpTabitoBlog.ToolTip = MsgMgr.GetMessage("tt002");
        }

        /// <summary>
        /// タイマー監視(入力とダウンロード)
        /// </summary>
        private async Task Monitoring()
        {
            try
            {
                // クリップボード監視
                if (_isAutoCopy || (toggleButtonAutoDownload.IsChecked ?? false))
                {
                    var clipboard = System.Windows.Forms.Clipboard.GetDataObject();
                    if (clipboard != null && clipboard.GetDataPresent(DataFormats.Text))
                    {
                        var textData = (string)clipboard.GetData(DataFormats.Text);
                        if (string.IsNullOrEmpty(textData) || !textData.StartsWith("https://")) return;
                        textBoxUrl.Text = textData.Trim();
                    }
                }

                // 自動ダウンロード
                if (toggleButtonAutoDownload.IsChecked ?? false) await BookYoutubeAsync(textBoxUrl.Text);

                // 予約キューから情報を取り出してダウンロードを開始する
                if (!_isDownloadLock && !_downloadQueue.IsEmpty && _downloadQueue.TryDequeue(out _currentData))
                {
                    if (_currentData == null || _currentData.Items == null || _currentData.SavePath == null) return;
                    _isDownloadLock = true;
                    _currentData.IsRunngin = true;
                    var target = new FileInfo(_currentData.SavePath);
                    _logger.Debug($"Queue count: {_downloadQueue.Count}");

                    // 既に保存されている場合、条件によって上書きしない
                    if (!Configs.IsOverWrite && target.Exists)
                    {
                        var data = Context.MediaItems?.FirstOrDefault(p => p.Id.Equals(_currentData.Id));
                        if (data != null)
                        {
                            _currentData.ContentLength = target.Length;         // 現在のバイト数を設定する
                            var info2 = MsgMgr.GetMessage("w1003", _currentData.Title, _currentData.Codec);
                            _logger.Info(info2);
                            snackbarMessage.MessageQueue?.Enqueue(info2);
                            await SaveDatabase(_currentData, false);            // データベースに保存する
                            PostProcess(_currentData);                          // 後処理
                            _isDownloadLock = false;
                            return;
                        }

                        // ID違いで同名のファイルの場合、IDを付加する
                        _currentData.SavePath = string.Concat(_currentData.SavePath.AsSpan(0, _currentData.SavePath.Length - 4), $"_{_currentData.Id}");
                    }

                    // ダウンロード準備（プログレスバー、メッセージ）
                    progressBarDownload.Visibility = Visibility.Visible;
                    progressBarDownload.IsIndeterminate = true;
                    progressBarDownload.Value = 0;
                    var info = MsgMgr.GetMessage("i0103", _currentData.Title, _currentData.Codec);
                    _logger.Info(info);
                    snackbarMessage.MessageQueue?.Enqueue(info);

                    // ダウンロード処理
                    var isSuccess = false;
                    var tryCnt = 0;
                    foreach (var item in _currentData.Items)
                    {
                        _currentData.Resolution = item.Resolution;          // 解像度
                        _currentData.Fps = item.Fps;                        // FPS
                        _currentData.AudioBitrate = item.AudioBitrate;      // ビットレート
                        _logger.Info($"Downloading... '{_currentData.Title}', {_currentData.Author}, {_currentData.Codec}, {_currentData.Resolution}, {_currentData.Fps}, {_currentData.AudioBitrate}");

                        // MP3 かつ AAC が存在するとき
                        if (_currentData.IsConvert && target.Exists)
                        {
                            await ConvertAacToMp3(_currentData, target);
                            isSuccess = true;
                            break;
                        }

                        // ダウンロード開始
                        var backupExist = target.Exists;
                        await item.GetBytesAsync().ContinueWith(t =>
                        {
                            if (_currentData.IsCreate)
                            {
                                _logger.Info($"Save File: {target.FullName}");
                                var dir = target.Directory;
                                if (dir != null && !dir.Exists) dir.Create();
                                if (t.IsCompletedSuccessfully)
                                {
                                    var bs = t.Result;                                              // ダウンロードしたバイト情報
                                    System.IO.File.WriteAllBytesAsync(target.FullName, bs);
                                    _currentData.ContentLength = bs.Length;

                                    isSuccess = true;
                                }
                                else if (tryCnt < _currentData.Items.Count)
                                {
                                    if (t.Exception != null) _logger.Error(t.Exception, "Download failed.");
                                    tryCnt++;
                                    return;
                                }
                                else
                                {
                                    if (t.Exception != null) throw t.Exception;
                                    throw new Exception("ダウンロードに失敗しました。");
                                }
                            }

                            // MP3専用
                            if (_currentData.IsConvert)
                            {
                                ConvertAacToMp3(_currentData, target).Wait();
                                isSuccess = true;

                                // MP3だけの時は AACを削除する
                                if (!backupExist) System.IO.File.Delete(_currentData.SavePath);
                            }
                        });
                        if (isSuccess) break;   // 成功判定
                    }

                    // 正常終了
                    if (isSuccess)
                    {
                        // データベースに保存する
                        await SaveDatabase(_currentData, true);

                        // 後処理
                        var info2 = string.Format(MsgMgr.GetMessage("i0104"), _currentData.Title, _currentData.Codec, (DateTime.Now - _currentData.Created).TotalMinutes);
                        _logger.Info(info2);
                        snackbarMessage.MessageQueue?.Enqueue(info2);
                    }
                    PostProcess(_currentData);
                    _isDownloadLock = false;            // 次タスクを実行可能
                }
            }
            catch (Exception ex)
            {
                // 自動ダウンロード(クリップボード監視)の停止
                _moniter.Stop();
                toggleButtonAutoDownload.IsChecked = false;
                gridManual.IsEnabled = true;

                var error = new StringBuilder();
                error.AppendLine(MsgMgr.GetMessage("e1002"));
                _logger.Error(ex, error.ToString());
                error.Append(ex.Message);
                await this.ShowMessageAsync("ERROR", error.ToString());

                // エラーフラグ立て後処理
                if (_currentData != null)
                {
                    _currentData.HasError = true;
                    PostProcess(_currentData);
                }

                // ロックをクリアする
                if (_isDownloadLock) _isDownloadLock = false;

                _moniter.Start();
            }
        }

        /// <summary>
        /// MP3コンバート
        /// </summary>
        /// <param name="data"></param>
        /// <param name="target"></param>
        private async Task ConvertAacToMp3(MediaData data, FileInfo target)
        {
            // AACからMP3へ変換する
            var mp3Path = string.Concat(target.FullName.AsSpan(0, target.FullName.Length - 3), "mp3");
            _logger.Info($"Convert AAC to MP3: {mp3Path}");

            // AAC -> MP3 & Edit Title.
            await Task.Run(() =>
            {
                // from AAC to MP3
                try
                {
                    var reader = new MediaFoundationReader(target.FullName);
                    var mediaType = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, reader.WaveFormat, reader.WaveFormat.BitsPerSample);
                    using var encoder = new MediaFoundationEncoder(mediaType);
                    encoder.Encode(mp3Path, reader);
                }
                catch (Exception ex)
                {
                    var error = MsgMgr.GetMessage("e1006");
                    _logger.Error($"{error} {ex.Message}");
                    if (System.IO.File.Exists(mp3Path))
                    {
                        _logger.Warn($"Delete file: {mp3Path}");
                        System.IO.File.Delete(mp3Path);
                    }
                    throw;
                }

                // MP3変換後に情報を付加する
                using var tfile = TagLib.File.Create(mp3Path);          // MP3読込み
                tfile.Tag.Title = data.Title;                           // タイトル追加
                tfile.Save();                                           // MP3へ再保存する
                data.AudioBitrate = tfile.Properties.AudioBitrate;      // ビットレート 
                data.ContentLength = new FileInfo(mp3Path).Length;      // ファイルサイズ
            });
        }

        /// <summary>
        /// データ保存
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isWrite"></param>
        /// <returns></returns>
        private async Task SaveDatabase(MediaData item, bool isWrite)
        {
            if (_hasDatabaseError) return;
            try
            {
                var data = Context.MediaItems?.FirstOrDefault(p => p.Id == item.Id);
                if (data != null)
                {
                    if (isWrite)
                    {
                        data.ContentLength = item.ContentLength;            // ファイルサイズ
                        data.Resolution = item.Resolution;                  // 解像度
                        data.Fps = item.Fps;                                // FPS
                        data.AudioBitrate = item.AudioBitrate;              // ビットレート
                        data.SavePath = item.SavePath;                      // 保存したパス
                        data.Updated = item.Created;                        // 日付更新
                    }
                    else
                    {
                        data.Updated = item.Created;                        // 日付更新
                    }
                }
                else
                {
                    // 履歴データ追加
                    Context.MediaItems?.Add(new MediaContext.MediaItem()
                    {
                        Id = item.Id,                                       // プライマリキー
                        Title = item.Title,                                 // タイトル
                        Author = item.Author,                               // 著者
                        TimeSpan = item.TimeSpan,                           // 再生時間
                        ContentLength = item.ContentLength,                 // ファイルサイズ
                        Codec = item.Codec,                                 // コーデック
                        Resolution = item.Resolution,                       // 解像度
                        Fps = item.Fps,                                     // FPS
                        AudioBitrate = item.AudioBitrate,                   // ビットレート
                        YouTubeUri = item.YouTubeUri,                       // URL
                        SavePath = item.SavePath,                           // 保存したパス
                        Created = item.Created,                             // 作成日付
                        Updated = item.Created,                             // 更新日図家
                    });
                }
                _logger.Info($"Save Database: {item.Id}, '{item.Title}', '{item.Author}', {item.Resolution}, {item.Fps}, {item.AudioBitrate}, {item.YouTubeUri}");
                await Context.SaveChangesAsync();                           // DB保存
            }
            catch (Exception ex)
            {
                _hasDatabaseError = true;
                var error = MsgMgr.GetMessage("e0203");
                _logger.Error(ex, error);
                _ = this.ShowMessageAsync("ERROR", error).Result;
                buttonHistory.IsEnabled = false;                            // 履歴不可で続行
            }
        }

        /// <summary>
        /// ダウンロード完了後の後始末
        /// </summary>
        /// <param name="item"></param>
        private void PostProcess(MediaData item)
        {
            if (!item.HasError)
            {
                item.IsCompleted = true;                        // 完了フラグ
                dataGridReserve.Items.Refresh();                // 表示情報の更新
                textBlockCounter.Text = $"{_resources.Count(p => p.IsCompleted):#,0}/{_resources.Count:#,0}";
            }

            // プログレスバー完了処理
            progressBarDownload.IsIndeterminate = false;
            progressBarDownload.Value = 100;

            // ダウンロード予約キューが空になったら、プログレスバーを隠す
            if (_downloadQueue.IsEmpty) progressBarDownload.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// YouTubeからのダウンロード予約
        /// </summary>
        /// <param name="inputUrl"></param>
        /// <returns></returns>
        private async Task BookYoutubeAsync(string? inputUrl)
        {
            var syncObject = new object();
            if (!_moniter.IsEnabled) _moniter.Start();
            if (inputUrl == null || !inputUrl.StartsWith("https://")) return;   // URLではない

            // URLの受け付け
            lock (syncObject)
            {
                var url = inputUrl.Trim();
                if (!_inputUris.Contains(url))
                {
                    _inputUris.Add(url);
                    _inputQueue.Enqueue(url);
                }

                // URLがキューにない
                if (_inputQueue.IsEmpty)
                {
                    _inputUris.Clear();
                    return;
                }

                // 予約処理中の判定
                if (_isBookingLock) return;
            }

            try
            {
                _isBookingLock = true;

                // URL受付キューからURLを取り出す
                string? textUrl;
                _inputQueue.TryDequeue(out textUrl);
                if (textUrl == null) return;

                // ビデオIDの取得
                string? videoId;                            // YouTubeのビデオID
                string link;                                // YouTubeのURL
                var dic = new Dictionary<string, bool>();   // Key:Codec, Val;MP4=True

                // YouTube の URLとして正しいかを判定する
                var uri = new Uri(textUrl);
                switch (uri.Authority)
                {
                    case "www.youtube.com":
                        var nv = HttpUtility.ParseQueryString(uri.Query);
                        videoId = nv["v"];
                        if (videoId == null) return;
                        link = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?v={videoId}";
                        break;
                    case "youtu.be":
                        videoId = uri.AbsolutePath[1..];
                        link = uri.AbsoluteUri;
                        break;
                    default: return;
                }

                // ビデオ予約
                if (checkBoxVideoMp4.IsChecked ?? false)
                {
                    var mediaId = $"{videoId}.mp4";
                    if (!_reservedUris.Contains(mediaId))
                    {
                        _reservedUris.Add(mediaId);
                        dic.Add("mp4", true);
                    }
                }

                // オーディオ(AAC)予約
                if (checkBoxAudioAac.IsChecked ?? false)
                {
                    var mediaId = $"{videoId}.aac";
                    if (!_reservedUris.Contains(mediaId))
                    {
                        _reservedUris.Add(mediaId);
                        dic.Add("aac", false);
                    }
                }

                // オーディオ(MP3)予約
                if (checkBoxAudioMp3.IsChecked ?? false)
                {
                    var mediaId = $"{videoId}.mp3";
                    if (!_reservedUris.Contains(mediaId))
                    {
                        _reservedUris.Add(mediaId);
                        dic.Add("mp3", false);
                    }
                }

                // 予約済みかを確認する
                if (dic.Count == 0) return;
                var info = MsgMgr.GetMessage("i0102", videoId);
                _logger.Info(info);
                snackbarMessage.MessageQueue?.Enqueue(info);

                // YouTubeから情報を取得し、ダウンロードを予約する
                var videos = await YouTube.Default.GetAllVideosAsync(link);
                foreach (var kv in dic)
                {
                    var codec = kv.Key;
                    var mediaId = $"{videoId}.{codec}";
                    (List<YouTubeVideo>? medias, string? path, bool isCreate) = kv.Value ? GetVideoInfo(videos, codec) : GetAudioInfo(videos, codec);
                    if (medias == null) continue;
                    _logger.Debug($"Path: {path}");

                    // ダウンロード情報を格納する
                    var media = medias[0];
                    var timeSpan = media.Info.LengthSeconds == null ? TimeSpan.Zero : TimeSpan.FromSeconds((double)media.Info.LengthSeconds);
                    var isMp3 = codec.Equals("mp3");
                    var length = 0L;                                // media.ContentLength を設定してはいけない(固まる場合あり)
                    var data = new MediaData()
                    {
                        Id = mediaId,                               // プライマリキー
                        Title = media.Title,                        // タイトル
                        Author = media.Info.Author,                 // 著者
                        TimeSpan = timeSpan,                        // 再生時間
                        ContentLength = length,                     // ファイルサイズ
                        Codec = codec.ToUpper(),                    // コーデック
                        YouTubeUri = new Uri(link),                 // YouTubeのURL
                        Created = DateTime.Now,                     // 作成日付
                        IsCreate = isCreate,                        // 新規作成
                        IsConvert = isMp3,                          // MP3用
                        Items = medias,                             // YouTubeデータ(VideoLibrary)
                        SavePath = path,                            // 保存するファイルのパス
                    };
                    _logger.Info($"Enqueue: '{data.Title}', {data.Author}, {data.Codec}, {data.TimeSpan}, {data.Resolution}, {data.Fps}, {data.AudioBitrate}, {link}");
                    _downloadQueue.Enqueue(data);                   // 予約キューに格納
                    _resources.Add(data);                           // メディアデータに格納
                }
                textBlockCounter.Text = $"{_resources.Count(p => p.IsCompleted)}/{_resources.Count}";
                dataGridReserve.Items.Refresh();                    // 表の再描画
            }
            catch (VideoLibrary.Exceptions.UnavailableStreamException ex)
            {
                var error = MsgMgr.GetMessage("e1005");
                _logger.Error(ex, error);
                snackbarMessage.MessageQueue?.Enqueue(error);
            }
            catch (Exception ex)
            {
                // 自動ダウンロード(クリップボード監視)の停止
                _moniter.Stop();
                toggleButtonAutoDownload.IsChecked = false;
                gridManual.IsEnabled = true;

                var error = new StringBuilder();
                error.AppendLine(MsgMgr.GetMessage("e1003"));
                _logger.Error(ex, error.ToString());
                error.Append(ex.Message);
                await this.ShowMessageAsync("ERROR", error.ToString());
                _moniter.Start();
            }
            finally
            {
                _isBookingLock = false;
            }
        }

        /// <summary>
        /// ビデオ情報を取得する
        /// </summary>
        /// <param name="videoInfos"></param>
        /// <returns></returns>
        private (List<YouTubeVideo>?, string?, bool) GetVideoInfo(IEnumerable<YouTubeVideo> videoInfos, string codec)
        {
            // ビデオ(最も解像度が高いものを選択する)
            var medias = videoInfos.Where(i => i.Format == VideoFormat.Mp4  && 0 < i.AudioBitrate).OrderByDescending(p => p.Resolution).ToList();
            if (medias == null) return (null, null, false);
            var media = medias[0];
            var basePath = Path.Combine(VideoPath, string.Join("_", media.Info.Author.Split(Path.GetInvalidFileNameChars())));
            var filePath = Path.Combine(basePath, 0 < media.FileExtension.Length ? media.FullName : $"{media.FullName}.{codec.ToLower()}");
            _logger.Debug($"Max={medias.Count}, {codec}: {media.Title}, {media.Info.Author}, {media.Resolution}, {media.Fps}, {media.AudioBitrate}");
            return (medias, filePath, true);
        }

        /// <summary>
        /// オーディオ情報を取得する
        /// </summary>
        /// <returns></returns>
        private (List<YouTubeVideo>?, string?, bool) GetAudioInfo(IEnumerable<YouTubeVideo> videoInfos, string codec)
        {
            // オーディオ(最もビットレートが高いものを選択する)
            var medias = videoInfos.Where(i => i.AudioFormat == AudioFormat.Aac && i.Resolution < 0).OrderByDescending(p => p.AudioBitrate).ToList();
            if (medias == null) return (null, null, false);
            var media = medias[0];
            var basePath = Path.Combine(AudioPath, string.Join("_", media.Info.Author.Split(Path.GetInvalidFileNameChars())));
            var filePath = Path.Combine(basePath, 0 < media.FileExtension.Length ? media.FullName : $"{media.FullName}.aac");
            var aacFlag = checkBoxAudioAac.IsChecked ?? false;
            var mp3Flag = checkBoxAudioMp3.IsChecked ?? false;
            var isCreate = codec.ToLower().Equals("aac") || aacFlag || (!aacFlag && mp3Flag);
            _logger.Debug($"Max={medias.Count}, {codec}: {media.Title}, {media.Info.Author}, {media.Resolution}, {media.Fps}, {media.AudioBitrate}");
            return (medias, filePath, isCreate);
        }

        /// <summary>
        /// テーマ変更
        /// </summary>
        private void SetTheme()
        {
            Designs.Configs.IsDark = toggleButtonIsDark.IsChecked ?? false;
            Designs.Configs.MahThemeColor = ((DesignManager.ColorItem)comboBoxTheme.SelectedItem).Name;
            _logger.Info($"Setting Theme. IsDark: {Designs.Configs.IsDark}, Theme: {Designs.Configs.MahThemeColor}");
            Designs.SetTheme(new Window?[] { this, _historyWindow });
        }

        /// <summary>
        /// 予約をキャンセルする
        /// </summary>
        private void CancelReservation()
        {
            if (dataGridReserve.SelectedCells.Count == 0) return;
            var item = (MediaData)dataGridReserve.SelectedCells[0].Item;
            if (item.IsRunngin)
            {
                var warn = MsgMgr.GetMessage("w1004", item.Title);
                _logger.Warn(warn);
                snackbarMessage.MessageQueue?.Enqueue(warn);
                return;
            }
            _moniter.Stop();

            // 一旦キューをクリアして、対象の行を削除し、またキューに戻す
            var syncObject = new object();
            lock (syncObject)
            {
                _downloadQueue.Clear();
                _resources.Remove(item);
                _resources.Where(p => !p.IsRunngin).ToList().ForEach(p => _downloadQueue.Enqueue(p));
            }
            _logger.Info($"Removed: '{item.Title}' - {item.Author}");
            dataGridReserve.Items.Refresh();
            _moniter.Start();
        }

    }
}
