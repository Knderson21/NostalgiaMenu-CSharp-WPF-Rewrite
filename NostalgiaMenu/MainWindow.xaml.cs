using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using NostalgiaMenu.Models;
using NostalgiaMenu.Parsers;

namespace NostalgiaMenu
{
    public partial class MainWindow : Window
    {
        private const string IniFileName    = "games.ini";
        private const int    CountdownTotal = 60;

        private GameEntry       _defaultGame;
        private DispatcherTimer _timer;
        private int             _secondsRemaining;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        // ──────────────────────────────────────────────────────────────
        // Startup
        // ──────────────────────────────────────────────────────────────

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IniFileName);

            if (!File.Exists(iniPath))
            {
                CreateTemplateIni(iniPath);
                MessageBox.Show(
                    string.Format("No games.ini found.\nA template has been created at:\n\n{0}\n\nEdit it to add your games, then restart.", iniPath),
                    "NostalgiaMenu — First Run",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }

            var games = LoadGames(iniPath);

            if (games.Count == 0)
            {
                MessageBox.Show(
                    "games.ini contains no valid game entries.\nCheck that each section has a 'launcher=' key.",
                    "NostalgiaMenu — No Games",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            // Default game tile goes first
            _defaultGame = games.FirstOrDefault(g => g.IsDefault);
            var orderedGames = games.Where(g => !g.IsDefault).ToList();
            if (_defaultGame != null)
                orderedGames.Insert(0, _defaultGame);

            foreach (var game in orderedGames)
                GameItemsControl.Items.Add(BuildTile(game));

            if (_defaultGame != null)
            {
                DefaultGameLabel.Text = _defaultGame.DisplayName;
                StartCountdown();
            }
        }

        // ──────────────────────────────────────────────────────────────
        // INI Loading
        // ──────────────────────────────────────────────────────────────

        private static List<GameEntry> LoadGames(string iniPath)
        {
            var ini   = IniParser.Parse(iniPath);
            var games = new List<GameEntry>();

            foreach (var section in ini)
            {
                var kv = section.Value;
                if (!kv.ContainsKey("launcher") || string.IsNullOrEmpty(kv["launcher"]))
                    continue;

                bool isDefault = section.Key.Equals("DEFAULT GAME", StringComparison.OrdinalIgnoreCase);
                string display = kv.ContainsKey("name") ? kv["name"] : section.Key;

                games.Add(new GameEntry
                {
                    SectionName  = section.Key,
                    DisplayName  = display,
                    LauncherPath = kv["launcher"],
                    ImagePath    = kv.ContainsKey("image") ? kv["image"] : null,
                    Color        = kv.ContainsKey("color") ? kv["color"] : null,
                    IsDefault    = isDefault
                });
            }

            return games;
        }

        // ──────────────────────────────────────────────────────────────
        // Tile Building
        // ──────────────────────────────────────────────────────────────

        private Border BuildTile(GameEntry entry)
        {
            // Accent color: gold for default or explicit gold, blue otherwise
            bool useGold = entry.IsDefault ||
                           (entry.Color != null &&
                            entry.Color.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0);

            var accentColor = useGold
                ? Color.FromRgb(0xFF, 0xD7, 0x00)
                : Color.FromRgb(0x00, 0xBF, 0xFF);

            var glowEffect = new DropShadowEffect
            {
                Color       = accentColor,
                BlurRadius  = 0,
                ShadowDepth = 0,
                Opacity     = 0.0
            };

            var tile = new Border
            {
                Width           = 220,
                Height          = 220,
                Margin          = new Thickness(12),
                BorderBrush     = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(2),
                CornerRadius    = new CornerRadius(10),
                Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                Effect          = glowEffect,
                Cursor          = Cursors.Hand,
                Tag             = entry,
                ClipToBounds    = true
            };

            var innerGrid = new Grid();
            tile.Child = innerGrid;

            // Layer 1: cover art or gradient fallback
            if (!string.IsNullOrEmpty(entry.ImagePath) && File.Exists(entry.ImagePath))
            {
                try
                {
                    var img = new Image
                    {
                        Source  = new BitmapImage(new Uri(entry.ImagePath, UriKind.Absolute)),
                        Stretch = Stretch.UniformToFill
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    innerGrid.Children.Add(img);
                }
                catch
                {
                    innerGrid.Children.Add(BuildFallbackBackground(entry.DisplayName, accentColor));
                }
            }
            else
            {
                innerGrid.Children.Add(BuildFallbackBackground(entry.DisplayName, accentColor));
            }

            // Layer 2: semi-transparent name strip at bottom
            var nameStrip = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Background        = new SolidColorBrush(Color.FromArgb(0xCC, 0x00, 0x00, 0x00)),
                Padding           = new Thickness(8, 5, 8, 5)
            };
            nameStrip.Child = new TextBlock
            {
                Text          = entry.DisplayName,
                Foreground    = new SolidColorBrush(Colors.White),
                FontSize      = 14,
                FontWeight    = FontWeights.SemiBold,
                TextWrapping  = TextWrapping.NoWrap,
                TextTrimming  = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center
            };
            innerGrid.Children.Add(nameStrip);

            // Default game indicator dot in top-right corner
            if (entry.IsDefault)
            {
                var dot = new Ellipse
                {
                    Width               = 14,
                    Height              = 14,
                    Fill                = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment   = VerticalAlignment.Top,
                    Margin              = new Thickness(0, 10, 10, 0),
                    Effect              = new DropShadowEffect
                    {
                        Color       = Color.FromRgb(0xFF, 0xD7, 0x00),
                        BlurRadius  = 8,
                        ShadowDepth = 0,
                        Opacity     = 0.9
                    }
                };
                innerGrid.Children.Add(dot);
            }

            // Events
            tile.MouseEnter += (s, e) => AnimateTileHover(tile, glowEffect, true);
            tile.MouseLeave += (s, e) => AnimateTileHover(tile, glowEffect, false);
            tile.MouseDown  += (s, e) => AnimateTilePress(tile);
            tile.MouseUp    += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    AnimateTileHover(tile, glowEffect, false);
                    LaunchGame(entry);
                }
            };
            tile.TouchDown += (s, e) =>
            {
                AnimateTilePress(tile);
                e.Handled = true;
            };
            tile.TouchUp += (s, e) =>
            {
                AnimateTileHover(tile, glowEffect, false);
                LaunchGame(entry);
                e.Handled = true;
            };

            return tile;
        }

        private static Grid BuildFallbackBackground(string displayName, Color accentColor)
        {
            var g = new Grid();

            g.Children.Add(new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(0x18, 0x18, 0x28),
                    Color.FromRgb(0x0A, 0x0A, 0x0A),
                    new Point(0, 0), new Point(1, 1))
            });

            g.Children.Add(new TextBlock
            {
                Text                = displayName,
                Foreground          = new SolidColorBrush(Colors.White),
                FontSize            = 20,
                FontWeight          = FontWeights.Bold,
                FontFamily          = new FontFamily("Segoe UI"),
                TextWrapping        = TextWrapping.Wrap,
                TextAlignment       = TextAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(12, 0, 12, 24),
                Effect              = new DropShadowEffect
                {
                    Color       = accentColor,
                    BlurRadius  = 10,
                    ShadowDepth = 0,
                    Opacity     = 0.7
                }
            });

            return g;
        }

        // ──────────────────────────────────────────────────────────────
        // Animations
        // ──────────────────────────────────────────────────────────────

        private static void AnimateTileHover(Border tile, DropShadowEffect glow, bool entering)
        {
            EnsureScaleTransform(tile);
            var st = (ScaleTransform)tile.RenderTransform;

            double scale   = entering ? 1.06 : 1.0;
            double blur    = entering ? 26.0 : 0.0;
            double opacity = entering ? 0.9  : 0.0;
            var    dur     = new Duration(TimeSpan.FromMilliseconds(160));
            var    ease    = new CubicEase { EasingMode = EasingMode.EaseOut };

            st.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(scale, dur) { EasingFunction = ease });
            st.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(scale, dur) { EasingFunction = ease });
            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                new DoubleAnimation(blur, dur));
            glow.BeginAnimation(DropShadowEffect.OpacityProperty,
                new DoubleAnimation(opacity, dur));
        }

        private static void AnimateTilePress(Border tile)
        {
            EnsureScaleTransform(tile);
            var st  = (ScaleTransform)tile.RenderTransform;
            var dur = new Duration(TimeSpan.FromMilliseconds(80));

            st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.94, dur));
            st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.94, dur));
        }

        private static void EnsureScaleTransform(Border tile)
        {
            if (!(tile.RenderTransform is ScaleTransform))
            {
                tile.RenderTransformOrigin = new Point(0.5, 0.5);
                tile.RenderTransform = new ScaleTransform(1.0, 1.0);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Countdown Timer
        // ──────────────────────────────────────────────────────────────

        private void StartCountdown()
        {
            _secondsRemaining = CountdownTotal;
            CountdownRingControl.TotalSeconds     = CountdownTotal;
            CountdownRingControl.RemainingSeconds = CountdownTotal;
            SecondsText.Text = CountdownTotal.ToString();
            CountdownPanel.Visibility = Visibility.Visible;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _secondsRemaining--;
            SecondsText.Text = _secondsRemaining.ToString();
            CountdownRingControl.RemainingSeconds = _secondsRemaining;

            if (_secondsRemaining <= 0)
            {
                _timer.Stop();
                LaunchGame(_defaultGame);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Game Launch
        // ──────────────────────────────────────────────────────────────

        private void LaunchGame(GameEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.LauncherPath))
                return;

            _timer?.Stop();

            try
            {
                string workDir = Path.GetDirectoryName(entry.LauncherPath)
                              ?? AppDomain.CurrentDomain.BaseDirectory;

                Process.Start(new ProcessStartInfo
                {
                    FileName         = entry.LauncherPath,
                    WorkingDirectory = workDir,
                    UseShellExecute  = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Failed to launch \"{0}\":\n\n{1}", entry.DisplayName, ex.Message),
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Application.Current.Shutdown();
        }

        // ──────────────────────────────────────────────────────────────
        // Input
        // ──────────────────────────────────────────────────────────────

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _timer?.Stop();
                Application.Current.Shutdown();
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Template INI Generator
        // ──────────────────────────────────────────────────────────────

        private static void CreateTemplateIni(string path)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("; NostalgiaMenu - games.ini");
            sb.AppendLine("; ─────────────────────────────────────────────────");
            sb.AppendLine("; Each [Section] defines one game tile.");
            sb.AppendLine(";");
            sb.AppendLine("; launcher = path to start.bat  (required)");
            sb.AppendLine("; image    = path to cover art  (optional, PNG/JPG)");
            sb.AppendLine("; color    = gold | blue         (optional tile accent)");
            sb.AppendLine("; name     = Display Name        (optional label override)");
            sb.AppendLine(";");
            sb.AppendLine("; [DEFAULT GAME] auto-launches after 60 seconds.");
            sb.AppendLine("; ─────────────────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine("[DEFAULT GAME]");
            sb.AppendLine("name     = Nostalgia");
            sb.AppendLine("launcher = C:\\Games\\Nostalgia\\start.bat");
            sb.AppendLine("image    = C:\\Games\\Nostalgia\\cover.png");
            sb.AppendLine("color    = gold");
            sb.AppendLine();
            sb.AppendLine("[BeatStream]");
            sb.AppendLine("launcher = C:\\Games\\BeatStream\\start.bat");
            sb.AppendLine("image    = C:\\Games\\BeatStream\\cover.png");
            sb.AppendLine("color    = blue");

            File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        }
    }
}
