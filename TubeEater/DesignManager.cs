using ControlzEx.Theming;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TubeEater
{
    /// <summary>
    /// MahappsとMaterialDesignを管理する
    /// </summary>
    public class DesignManager
    {
        // 初期表示色(App.xamlと合わせること)
        public const string DefaultMahAppsTheme = @"Purple";
        public const string DefaultPrimaryColor = @"DeepPurple";
        public const string DefaultSecondaryColor = @"Lime";

        /// <summary>
        /// 配色管理
        /// </summary>
        public class ColorItem
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
            public Color Color { get; set; }
        }

        // 設定情報
        public SettingsData Configs { get; set; }

        // Windowsモードと同期する
        public bool IsInheritDark { get; set; }

        /// <summary>
        /// MahAppsテーマ色
        /// </summary>
        public List<ColorItem> MahAppsItems { get; } = new List<ColorItem>
        {
            new ColorItem() { Name = "Red", Value = 0 },
            new ColorItem() { Name = "Green", Value = 1 },
            new ColorItem() { Name = "Blue", Value = 2 },
            new ColorItem() { Name = "Purple", Value = 3 },
            new ColorItem() { Name = "Orange", Value = 4 },
            new ColorItem() { Name = "Lime", Value = 5 },
            new ColorItem() { Name = "Emerald", Value = 6 },
            new ColorItem() { Name = "Teal", Value = 7 },
            new ColorItem() { Name = "Cyan", Value = 8 },
            new ColorItem() { Name = "Cobalt", Value = 9 },
            new ColorItem() { Name = "Indigo", Value = 10 },
            new ColorItem() { Name = "Violet", Value = 11 },
            new ColorItem() { Name = "Pink", Value = 12 },
            new ColorItem() { Name = "Magenta", Value = 13 },
            new ColorItem() { Name = "Crimson", Value = 14 },
            new ColorItem() { Name = "Amber", Value = 15 },
            new ColorItem() { Name = "Yellow", Value = 16 },
            new ColorItem() { Name = "Brown", Value = 17 },
            new ColorItem() { Name = "Olive", Value = 18 },
            new ColorItem() { Name = "Steel", Value = 19 },
            new ColorItem() { Name = "Mauve", Value = 20 },
            new ColorItem() { Name = "Taupe", Value = 21 },
            new ColorItem() { Name = "Sienna", Value = 22 }
        };

        // MaterialDesign Xaml Toolkit の配色
        public List<ColorItem> MdxToolItems { get; } = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings"></param>
        public DesignManager(SettingsData settings) => Configs = settings;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="window"></param>
        public void Initialize(Window window)
        {
            // MaterialDesign
            var paletteHelper = new PaletteHelper();
            var mdxTheme = paletteHelper.GetTheme();
            if (mdxTheme.Background.Equals(ColorConverter.ConvertFromString("#000000"))) IsInheritDark = true;
            if (Configs.IsWindowsMode)
            {
                Configs.IsDark = IsInheritDark;     // Windowsモードに同期する
            }
            else
            {
                mdxTheme.SetBaseTheme(Configs.IsDark ? new MaterialDesignDarkTheme() : new MaterialDesignLightTheme());
                paletteHelper.SetTheme(mdxTheme);
            }

            // MahApps のテーマを変更する（ライト＆ダーク）
            var mahTheme = Configs.IsDark ? "Dark" : "Light";
            ThemeManager.Current.ChangeTheme(window, $"{mahTheme}.{Configs.MahThemeColor}");

            // MaterialDesign Xaml Toolkitの全配色を取得する
            MdxToolItems.AddRange(new SwatchesProvider().Swatches.Select(p => new ColorItem()
            {
                Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p.Name),
                Value = MdxToolItems.Count,
                Color = p.PrimaryHues[5].Color,
            }));

            // プライマリ・セカンダリの色設定
            SetPrimaryColor(MdxToolItems.FirstOrDefault(p => p.Name.Equals(Configs.PrimaryColor)));
            SetSecondaryColor(MdxToolItems.FirstOrDefault(p => p.Name.Equals(Configs.SecondaryColor)));
        }

        /// <summary>
        /// テーマ色の設定
        /// </summary>
        /// <param name="window"></param>
        public void SetTheme(Window window) => SetTheme(new Window[] { window });

        /// <summary>
        /// テーマ色の設定
        /// </summary>
        /// <param name="windows"></param>
        public void SetTheme(IEnumerable<Window?> windows)
        {
            // MahApps
            var mahTheme = Configs.IsDark ? "Dark" : "Light";
            foreach (var window in windows)
            {
                if (window == null) continue;
                ThemeManager.Current.ChangeTheme(window, $"{mahTheme}.{Configs.MahThemeColor}");
            }

            // MaterialDesign Xaml Toolkit
            var paletteHelper = new PaletteHelper();
            var mdxTheme = paletteHelper.GetTheme();
            mdxTheme.SetBaseTheme(Configs.IsDark ? new MaterialDesignDarkTheme() : new MaterialDesignLightTheme());
            paletteHelper.SetTheme(mdxTheme);
        }

        /// <summary>
        /// プライマリ＆セカンダリの色設定
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public void SetColors(ColorItem? primary, ColorItem? secondary)
        {
            if (primary == null || secondary == null) return;
            var paletteHelper = new PaletteHelper();
            var mdxTheme = paletteHelper.GetTheme();
            mdxTheme.SetPrimaryColor(primary.Color);
            mdxTheme.SetSecondaryColor(secondary.Color);
            if (Configs.IsColorAdjustment) mdxTheme.AdjustColors();
            paletteHelper.SetTheme(mdxTheme);
            Configs.PrimaryColor = primary.Name;
            Configs.SecondaryColor = secondary.Name;
        }

        /// <summary>
        /// プライマリ色の設定
        /// </summary>
        /// <param name="item"></param>
        public void SetPrimaryColor(ColorItem? item)
        {
            if (item == null) return;
            var paletteHelper = new PaletteHelper();
            var mdxTheme = paletteHelper.GetTheme();
            mdxTheme.SetPrimaryColor(item.Color);
            if (Configs.IsColorAdjustment) mdxTheme.AdjustColors();
            paletteHelper.SetTheme(mdxTheme);
            Configs.PrimaryColor = item.Name;
        }

        /// <summary>
        /// セカンダリ色の設定
        /// </summary>
        /// <param name="item"></param>
        public void SetSecondaryColor(ColorItem? item)
        {
            if (item == null) return;
            var paletteHelper = new PaletteHelper();
            var mdxTheme = paletteHelper.GetTheme();
            mdxTheme.SetSecondaryColor(item.Color);
            if (Configs.IsColorAdjustment) mdxTheme.AdjustColors();
            paletteHelper.SetTheme(mdxTheme);
            Configs.SecondaryColor = item.Name;
        }
    }
}
