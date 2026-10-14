using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;

namespace OctaneMCPSetup
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = false;
            Application app = new Application();
            MainWindow win = new MainWindow();
            app.Run(win);
        }
    }

    public class MainWindow : Window
    {
        // Neutral Zinc / Carbon Palette (matching extension/overlay.css)
        private static readonly Color CWindowBg = Color.FromRgb(18, 18, 20);       // #121214
        private static readonly Color CSurface = Color.FromRgb(22, 22, 24);        // #161618
        private static readonly Color CCard = Color.FromRgb(27, 27, 30);           // #1B1B1E
        private static readonly Color CCardSubtle = Color.FromRgb(23, 23, 26);     // #17171A
        private static readonly Color CCodeBg = Color.FromRgb(14, 14, 16);         // #0E0E10
        private static readonly Color CBorder = Color.FromRgb(39, 39, 43);         // #27272B
        private static readonly Color CBorderSubtle = Color.FromRgb(32, 32, 36);   // #202024
        private static readonly Color CTextPrimary = Color.FromRgb(244, 244, 246); // #F4F4F6
        private static readonly Color CTextBody = Color.FromRgb(200, 200, 206);    // #C8C8CE
        private static readonly Color CTextMuted = Color.FromRgb(138, 138, 146);   // #8A8A92
        private static readonly Color CTextDim = Color.FromRgb(107, 107, 115);     // #6B6B73
        private static readonly Color CGreen = Color.FromRgb(52, 211, 153);        // #34D399
        private static readonly Color CAmber = Color.FromRgb(251, 191, 36);        // #FBBF24
        private static readonly Color CNeutralDot = Color.FromRgb(82, 82, 91);     // #52525B

        private static readonly SolidColorBrush BWindowBg = new SolidColorBrush(CWindowBg);
        private static readonly SolidColorBrush BSurface = new SolidColorBrush(CSurface);
        private static readonly SolidColorBrush BCard = new SolidColorBrush(CCard);
        private static readonly SolidColorBrush BCardSubtle = new SolidColorBrush(CCardSubtle);
        private static readonly SolidColorBrush BCodeBg = new SolidColorBrush(CCodeBg);
        private static readonly SolidColorBrush BBorder = new SolidColorBrush(CBorder);
        private static readonly SolidColorBrush BBorderSubtle = new SolidColorBrush(CBorderSubtle);
        private static readonly SolidColorBrush BTextPrimary = new SolidColorBrush(CTextPrimary);
        private static readonly SolidColorBrush BTextBody = new SolidColorBrush(CTextBody);
        private static readonly SolidColorBrush BTextMuted = new SolidColorBrush(CTextMuted);
        private static readonly SolidColorBrush BTextDim = new SolidColorBrush(CTextDim);
        private static readonly SolidColorBrush BGreen = new SolidColorBrush(CGreen);
        private static readonly SolidColorBrush BAmber = new SolidColorBrush(CAmber);
        private static readonly SolidColorBrush BNeutralDot = new SolidColorBrush(CNeutralDot);

        private string repoDir;
        private string serverJsPath;
        private string serverJsSlash;
        private string pluginSourcePath;
        private string extensionDir;
        private string startBatPath;
        private string pluginsFolder;

        private bool isPluginInstalled = false;
        private bool isNodeInstalled = false;
        private string nodeVersionStr = "";
        private bool isNodeModulesInstalled = false;
        private bool isMcpPortListening = false;
        private bool isStudioConnected = false;
        private bool isStudioProcessRunning = false;
        private bool isZeroScriptRunning = false;

        private int currentStep = 1;
        private List<Border> navRows = new List<Border>();
        private List<TextBlock> navCheckTexts = new List<TextBlock>();
        private List<Border> navCheckBoxes = new List<Border>();
        private List<UIElement> stepPages = new List<UIElement>();
        private ScrollViewer mainScroll;

        // Titlebar & Sidebar health indicators
        private Ellipse titleStatusDot;
        private TextBlock titleStatusText;
        private Ellipse sideDotPlugin;
        private TextBlock sideTxtPlugin;
        private Ellipse sideDotNode;
        private TextBlock sideTxtNode;
        private Ellipse sideDotPort;
        private TextBlock sideTxtPort;
        private Ellipse sideDotStudio;
        private TextBlock sideTxtStudio;

        // Dynamic controls across pages
        private TextBlock lblPluginStatusDetail;
        private Button btnInstallPlugin;
        private TextBlock lblDepsStatusDetail;
        private Button btnRunNpm;
        private TextBox txtNpmOutput;

        private string selectedClient = "opencode";
        private Dictionary<string, Button> clientButtons = new Dictionary<string, Button>();
        private TextBlock lblClientFile;
        private Ellipse dotClientConfigured;
        private TextBlock lblClientConfigured;
        private TextBox txtClientCode;
        private Button btnApplyClient;

        private TextBox txtTestOutput;
        private Border shotPreviewBorder;
        private Image imgShotPreview;

        private Button btnPrev;
        private Button btnNext;
        private TextBlock lblFooterStep;
        private Border toastBorder;
        private TextBlock toastLabel;
        private DispatcherTimer toastTimer;
        private DispatcherTimer pollTimer;
        private Process bgServerProc;

        public MainWindow()
        {
            ResolvePaths();

            this.Title = "Octane Studio MCP";
            this.Width = 900;
            this.Height = 600;
            this.MinWidth = 760;
            this.MinHeight = 500;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.CanResize;
            this.Background = BWindowBg;
            this.Foreground = BTextPrimary;
            this.FontFamily = new FontFamily("Segoe UI");

            WindowChrome chrome = new WindowChrome
            {
                CaptionHeight = 38,
                ResizeBorderThickness = new Thickness(5),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            LoadStyles();
            TryLoadIcon();
            BuildLayout();

            this.Closed += OnWindowClosed;

            RefreshStatusAsync();
            pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            pollTimer.Tick += delegate { RefreshStatusAsync(); };
            pollTimer.Start();
        }

        private void LoadStyles()
        {
            string styleXaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <!-- Sleek Thin Dark ScrollBar -->
  <Style x:Key='ScrollBarThumb' TargetType='{x:Type Thumb}'>
    <Setter Property='OverridesDefaultStyle' Value='true'/>
    <Setter Property='IsTabStop' Value='false'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Thumb}'>
          <Border x:Name='thumbBd' Background='#2E2E34' CornerRadius='3' Margin='2,0,2,0'/>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='thumbBd' Property='Background' Value='#45454E'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='{x:Type ScrollBar}'>
    <Setter Property='OverridesDefaultStyle' Value='true'/>
    <Setter Property='Width' Value='8'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ScrollBar}'>
          <Grid Background='Transparent'>
            <Track x:Name='PART_Track' IsDirectionReversed='true'>
              <Track.Thumb>
                <Thumb Style='{StaticResource ScrollBarThumb}'/>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property='Orientation' Value='Horizontal'>
        <Setter Property='Width' Value='Auto'/>
        <Setter Property='Height' Value='8'/>
        <Setter Property='Template'>
          <Setter.Value>
            <ControlTemplate TargetType='{x:Type ScrollBar}'>
              <Grid Background='Transparent'>
                <Track x:Name='PART_Track' IsDirectionReversed='false'>
                  <Track.Thumb>
                    <Thumb Style='{StaticResource ScrollBarThumb}'/>
                  </Track.Thumb>
                </Track>
              </Grid>
            </ControlTemplate>
          </Setter.Value>
        </Setter>
      </Trigger>
    </Style.Triggers>
  </Style>

  <!-- Primary High-Contrast Button (#F4F4F6 surface, #161618 text) -->
  <Style x:Key='BtnPrimary' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='#F4F4F6'/>
    <Setter Property='Foreground' Value='#141416'/>
    <Setter Property='BorderBrush' Value='#F4F4F6'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='13,6,13,6'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bd' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='6' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#FFFFFF'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#DCDCE0'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='false'>
              <Setter Property='Opacity' Value='0.45'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- Secondary Surface Button (#222226 surface, #303036 border, #E4E4E8 text) -->
  <Style x:Key='BtnSecondary' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='#222226'/>
    <Setter Property='Foreground' Value='#E4E4E8'/>
    <Setter Property='BorderBrush' Value='#303036'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='12,6,12,6'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='FontWeight' Value='Medium'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bd' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='6' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#2B2B30'/>
              <Setter TargetName='bd' Property='BorderBrush' Value='#3E3E46'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#1C1C20'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='false'>
              <Setter Property='Opacity' Value='0.4'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- Subtle Inline Button (for Copy / small actions) -->
  <Style x:Key='BtnSubtle' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='#1D1D21'/>
    <Setter Property='Foreground' Value='#A0A0A8'/>
    <Setter Property='BorderBrush' Value='#2B2B31'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='9,4,9,4'/>
    <Setter Property='FontSize' Value='11'/>
    <Setter Property='FontWeight' Value='Medium'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bd' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='5' Padding='{TemplateBinding Padding}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#27272C'/>
              <Setter Property='Foreground' Value='#F4F4F6'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- Titlebar Window Control Button -->
  <Style x:Key='BtnWinCtrl' TargetType='{x:Type Button}'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Foreground' Value='#8A8A92'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='Width' Value='42'/>
    <Setter Property='Height' Value='38'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bd' Background='{TemplateBinding Background}'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#26262B'/>
              <Setter Property='Foreground' Value='#F4F4F6'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='BtnWinClose' TargetType='{x:Type Button}' BasedOn='{StaticResource BtnWinCtrl}'>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='bd' Background='Transparent'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='true'>
              <Setter TargetName='bd' Property='Background' Value='#C42B1C'/>
              <Setter Property='Foreground' Value='#FFFFFF'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>";
            ResourceDictionary dict = (ResourceDictionary)XamlReader.Parse(styleXaml);
            this.Resources.MergedDictionaries.Add(dict);
        }

        private void ResolvePaths()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            pluginsFolder = System.IO.Path.Combine(localAppData, "Roblox", "Plugins");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            if (File.Exists(System.IO.Path.Combine(baseDir, "server.js")) &&
                File.Exists(System.IO.Path.Combine(baseDir, "MCPBridge2021.lua")))
            {
                repoDir = baseDir;
            }
            else
            {
                DirectoryInfo parent = Directory.GetParent(baseDir);
                if (parent != null &&
                    File.Exists(System.IO.Path.Combine(parent.FullName, "server.js")) &&
                    File.Exists(System.IO.Path.Combine(parent.FullName, "MCPBridge2021.lua")))
                {
                    repoDir = parent.FullName;
                }
                else
                {
                    // Standalone .exe mode: extract embedded payload into %LOCALAPPDATA%\OctaneStudioMCP
                    string standaloneDir = System.IO.Path.Combine(localAppData, "OctaneStudioMCP");
                    ExtractEmbeddedPayload(standaloneDir);
                    repoDir = standaloneDir;
                }
            }

            serverJsPath = System.IO.Path.Combine(repoDir, "server.js");
            serverJsSlash = serverJsPath.Replace('\\', '/');
            pluginSourcePath = System.IO.Path.Combine(repoDir, "MCPBridge2021.lua");
            extensionDir = System.IO.Path.Combine(repoDir, "extension");
            startBatPath = System.IO.Path.Combine(repoDir, "start.bat");
        }

        private void ExtractEmbeddedPayload(string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream resStream = asm.GetManifestResourceStream("OctaneMCP.Payload.zip"))
                {
                    if (resStream == null) return;
                    using (ZipArchive archive = new ZipArchive(resStream, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string fullPath = System.IO.Path.Combine(targetDir, entry.FullName.Replace('/', '\\'));
                            if (String.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(fullPath);
                                continue;
                            }
                            string dir = System.IO.Path.GetDirectoryName(fullPath);
                            if (!String.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                            using (Stream inStream = entry.Open())
                            using (FileStream outStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                            {
                                inStream.CopyTo(outStream);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void TryLoadIcon()
        {
            try
            {
                string iconPng = System.IO.Path.Combine(extensionDir, "icon.png");
                if (File.Exists(iconPng))
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(iconPng, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    this.Icon = bmp;
                }
            }
            catch { }
        }

        private void BuildLayout()
        {
            Border outerFrame = new Border
            {
                BorderBrush = BBorder,
                BorderThickness = new Thickness(1),
                Background = BWindowBg
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) }); // Custom Titlebar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Content
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); // Bottom Bar

            // 1. Custom Dark Titlebar
            Border titleBar = BuildTitleBar();
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            // 2. Body (Left Sidebar + Right Content)
            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(224) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 1);

            Border sidebar = BuildSidebar();
            Grid.SetColumn(sidebar, 0);
            body.Children.Add(sidebar);

            Grid pageContainer = new Grid();
            Grid.SetColumn(pageContainer, 1);

            mainScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(28, 22, 28, 24)
            };

            Grid pagesGrid = new Grid();
            stepPages.Add(BuildPage1Plugin());
            stepPages.Add(BuildPage2Dependencies());
            stepPages.Add(BuildPage3Clients());
            stepPages.Add(BuildPage4BrowserExtension());
            stepPages.Add(BuildPage5Diagnostics());

            for (int i = 0; i < stepPages.Count; i++)
            {
                pagesGrid.Children.Add(stepPages[i]);
            }

            mainScroll.Content = pagesGrid;
            pageContainer.Children.Add(mainScroll);

            // Discreet Bottom-Right Toast Notification
            toastBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 66)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(13, 8, 13, 8),
                Margin = new Thickness(0, 0, 20, 14),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Visibility = Visibility.Collapsed
            };
            toastLabel = new TextBlock
            {
                Foreground = BTextPrimary,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            toastBorder.Child = toastLabel;
            pageContainer.Children.Add(toastBorder);

            body.Children.Add(pageContainer);
            root.Children.Add(body);

            // 3. Footer Bar
            Border footer = BuildFooter();
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            outerFrame.Child = root;
            this.Content = outerFrame;

            SelectStep(1);
            SelectClient("opencode");
        }

        private Border BuildTitleBar()
        {
            Border bar = new Border
            {
                Background = BSurface,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // App Identity Left
            StackPanel left = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };

            if (this.Icon != null)
            {
                Image ic = new Image
                {
                    Source = this.Icon,
                    Width = 15,
                    Height = 15,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(ic, BitmapScalingMode.HighQuality);
                left.Children.Add(ic);
            }

            left.Children.Add(new TextBlock
            {
                Text = "Octane Studio MCP",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            });

            left.Children.Add(new TextBlock
            {
                Text = "v1.5.7",
                FontSize = 11,
                Foreground = BTextDim,
                Margin = new Thickness(7, 1, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            Grid.SetColumn(left, 0);
            g.Children.Add(left);

            // Quiet Status Line
            StackPanel statusLine = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            titleStatusDot = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = BNeutralDot,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStatusText = new TextBlock
            {
                Text = "Checking environment...",
                FontSize = 11.5,
                Foreground = BTextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusLine.Children.Add(titleStatusDot);
            statusLine.Children.Add(titleStatusText);
            Grid.SetColumn(statusLine, 2);
            g.Children.Add(statusLine);

            // Window Controls
            StackPanel winCtrls = new StackPanel { Orientation = Orientation.Horizontal };

            Button btnMin = new Button { Style = (Style)FindResource("BtnWinCtrl"), Content = CreateVectorIcon("M 0,5 L 10,5", 10, 10) };
            WindowChrome.SetIsHitTestVisibleInChrome(btnMin, true);
            btnMin.Click += delegate { this.WindowState = WindowState.Minimized; };

            Button btnMax = new Button { Style = (Style)FindResource("BtnWinCtrl"), Content = CreateVectorIcon("M 0.5,0.5 L 9.5,0.5 L 9.5,9.5 L 0.5,9.5 Z", 10, 10) };
            WindowChrome.SetIsHitTestVisibleInChrome(btnMax, true);
            btnMax.Click += delegate
            {
                this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
            };

            Button btnClose = new Button { Style = (Style)FindResource("BtnWinClose"), Content = CreateVectorIcon("M 0.5,0.5 L 9.5,9.5 M 9.5,0.5 L 0.5,9.5", 10, 10) };
            WindowChrome.SetIsHitTestVisibleInChrome(btnClose, true);
            btnClose.Click += delegate { this.Close(); };

            winCtrls.Children.Add(btnMin);
            winCtrls.Children.Add(btnMax);
            winCtrls.Children.Add(btnClose);
            Grid.SetColumn(winCtrls, 3);
            g.Children.Add(winCtrls);

            bar.Child = g;
            return bar;
        }

        private UIElement CreateVectorIcon(string pathData, double w, double h)
        {
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = BTextMuted,
                StrokeThickness = 1.2,
                Width = w,
                Height = h,
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            p.SetBinding(System.Windows.Shapes.Path.StrokeProperty, new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
            return p;
        }

        private Border BuildSidebar()
        {
            Border side = new Border
            {
                Background = BSurface,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(12, 16, 12, 14)
            };

            DockPanel dock = new DockPanel { LastChildFill = false };

            TextBlock lblSetup = CreateSectionLabel("Setup steps");
            lblSetup.Margin = new Thickness(8, 0, 0, 8);
            DockPanel.SetDock(lblSetup, Dock.Top);
            dock.Children.Add(lblSetup);

            string[] stepTitles = new string[]
            {
                "Studio Plugin",
                "Dependencies",
                "AI Client Config",
                "Browser Extension",
                "Connection & Tools"
            };

            for (int i = 0; i < stepTitles.Length; i++)
            {
                int stepNum = i + 1;
                TextBlock checkTxt;
                Border checkBox;
                Border row = CreateSidebarStepRow(stepNum, stepTitles[i], out checkTxt, out checkBox);
                navRows.Add(row);
                navCheckTexts.Add(checkTxt);
                navCheckBoxes.Add(checkBox);
                DockPanel.SetDock(row, Dock.Top);
                dock.Children.Add(row);
            }

            Border sep = new Border
            {
                Height = 1,
                Background = BBorderSubtle,
                Margin = new Thickness(6, 14, 6, 14)
            };
            DockPanel.SetDock(sep, Dock.Top);
            dock.Children.Add(sep);

            TextBlock lblEnv = CreateSectionLabel("Environment");
            lblEnv.Margin = new Thickness(8, 0, 0, 8);
            DockPanel.SetDock(lblEnv, Dock.Top);
            dock.Children.Add(lblEnv);

            Border h1 = CreateHealthItem("Studio plugin", "Checking...", out sideDotPlugin, out sideTxtPlugin);
            Border h2 = CreateHealthItem("Node.js", "Checking...", out sideDotNode, out sideTxtNode);
            Border h3 = CreateHealthItem("MCP bridge :3021", "Checking...", out sideDotPort, out sideTxtPort);
            Border h4 = CreateHealthItem("2021 Studio", "Checking...", out sideDotStudio, out sideTxtStudio);

            DockPanel.SetDock(h1, Dock.Top);
            DockPanel.SetDock(h2, Dock.Top);
            DockPanel.SetDock(h3, Dock.Top);
            DockPanel.SetDock(h4, Dock.Top);
            dock.Children.Add(h1);
            dock.Children.Add(h2);
            dock.Children.Add(h3);
            dock.Children.Add(h4);

            // Bottom Quick Actions
            StackPanel bottomStack = new StackPanel();
            Button btnQuick = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Quick Setup",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 6)
            };
            btnQuick.Click += delegate { RunQuickSetup(); };
            bottomStack.Children.Add(btnQuick);

            Button btnOpenFolder = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "Open Project Folder",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnOpenFolder.Click += delegate { OpenFolder(repoDir); };
            bottomStack.Children.Add(btnOpenFolder);

            DockPanel.SetDock(bottomStack, Dock.Bottom);
            dock.Children.Add(bottomStack);

            side.Child = dock;
            return side;
        }

        private Border CreateSidebarStepRow(int idx, string title, out TextBlock badgeText, out Border badgeBox)
        {
            Border row = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 7, 8, 7),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            badgeBox = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 36)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 52)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeText = new TextBlock
            {
                Text = idx.ToString(),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBox.Child = badgeText;
            Grid.SetColumn(badgeBox, 0);
            g.Children.Add(badgeBox);

            TextBlock lbl = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = BTextBody,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 1);
            g.Children.Add(lbl);

            row.Child = g;
            row.MouseLeftButtonUp += delegate { SelectStep(idx); };
            return row;
        }

        private Border CreateHealthItem(string name, string detail, out Ellipse dot, out TextBlock detailLbl)
        {
            Border item = new Border
            {
                Background = BCardSubtle,
                BorderBrush = BBorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 6, 9, 6),
                Margin = new Thickness(0, 0, 0, 4)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = BNeutralDot,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dot, 0);
            g.Children.Add(dot);

            TextBlock nameLbl = new TextBlock
            {
                Text = name,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = BTextBody,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameLbl, 1);
            g.Children.Add(nameLbl);

            detailLbl = new TextBlock
            {
                Text = detail,
                FontSize = 10.5,
                Foreground = BTextDim,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(detailLbl, 2);
            g.Children.Add(detailLbl);

            item.Child = g;
            return item;
        }

        private void SelectStep(int stepIdx)
        {
            if (stepIdx < 1) stepIdx = 1;
            if (stepIdx > stepPages.Count) stepIdx = stepPages.Count;
            currentStep = stepIdx;

            for (int i = 0; i < stepPages.Count; i++)
            {
                stepPages[i].Visibility = (i + 1 == currentStep) ? Visibility.Visible : Visibility.Collapsed;
            }

            for (int i = 0; i < navRows.Count; i++)
            {
                bool active = (i + 1 == currentStep);
                navRows[i].Background = active
                    ? new SolidColorBrush(Color.FromRgb(34, 34, 39))
                    : Brushes.Transparent;
            }

            if (btnPrev != null) btnPrev.IsEnabled = (currentStep > 1);
            if (btnNext != null) btnNext.Content = (currentStep < stepPages.Count) ? "Next" : "Done";
            if (lblFooterStep != null) lblFooterStep.Text = String.Format("Step {0} of {1}", currentStep, stepPages.Count);

            if (mainScroll != null) mainScroll.ScrollToTop();
        }

        // ====================================================================
        // PAGE 1: STUDIO PLUGIN
        // ====================================================================
        private UIElement BuildPage1Plugin()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(CreatePageHeader(
                "Studio Plugin",
                "Install MCPBridge2021.lua into your local Roblox Studio plugins folder so Studio can communicate with the MCP server on port 3021."
            ));

            // Action Card
            Border card = CreateSurfaceCard();
            Grid cg = new Grid();
            cg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
            info.Children.Add(new TextBlock
            {
                Text = "MCPBridge2021.lua",
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary
            });
            lblPluginStatusDetail = new TextBlock
            {
                Text = pluginsFolder,
                FontSize = 11.5,
                Foreground = BTextMuted,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            info.Children.Add(lblPluginStatusDetail);
            Grid.SetColumn(info, 0);
            cg.Children.Add(info);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnOpenPlugins = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "Open Plugins Folder",
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnOpenPlugins.Click += delegate
            {
                try { Directory.CreateDirectory(pluginsFolder); } catch { }
                OpenFolder(pluginsFolder);
            };
            actions.Children.Add(btnOpenPlugins);

            btnInstallPlugin = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Install Plugin"
            };
            btnInstallPlugin.Click += delegate { InstallPlugin(); };
            actions.Children.Add(btnInstallPlugin);

            Grid.SetColumn(actions, 1);
            cg.Children.Add(actions);
            card.Child = cg;
            page.Children.Add(card);

            // Step-by-step instructions list
            page.Children.Add(CreateSectionLabel("Manual installation & verification"));

            Border stepsContainer = CreateSurfaceCard();
            stepsContainer.Padding = new Thickness(16, 6, 16, 6);
            StackPanel stepsList = new StackPanel();

            stepsList.Children.Add(CreateStepRow(
                1,
                "Open the Roblox Plugins directory",
                "Click Install Plugin above, or press Win + R and open your local plugins folder:",
                "%localappdata%\\Roblox\\Plugins",
                true
            ));
            stepsList.Children.Add(CreateRowDivider());

            stepsList.Children.Add(CreateStepRow(
                2,
                "Copy MCPBridge2021.lua into the folder",
                "Copy MCPBridge2021.lua from this repository into %localappdata%\\Roblox\\Plugins.",
                null,
                false
            ));
            stepsList.Children.Add(CreateRowDivider());

            stepsList.Children.Add(CreateStepRow(
                3,
                "Open a place in 2021 Roblox Studio (Octane / Aisaka)",
                "Launch 2021 Studio and open any place file. Open View > Output and verify this line appears:",
                "[MCP 2021] Bridge initialized. Connecting to http://127.0.0.1:3021 ...",
                false
            ));
            stepsList.Children.Add(CreateRowDivider());

            stepsList.Children.Add(CreateStepRow(
                4,
                "Confirm HttpService is enabled",
                "The plugin sets HttpService.HttpEnabled = true automatically. If your Studio build restricts plugin permissions, run this once in the Command Bar:",
                "game:GetService(\"HttpService\").HttpEnabled = true",
                true
            ));

            stepsContainer.Child = stepsList;
            page.Children.Add(stepsContainer);

            return page;
        }

        // ====================================================================
        // PAGE 2: DEPENDENCIES
        // ====================================================================
        private UIElement BuildPage2Dependencies()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(CreatePageHeader(
                "Server Dependencies",
                "The MCP server (server.js) runs on Node.js. Install the required packages in this folder before connecting an AI client."
            ));

            Border card = CreateSurfaceCard();
            Grid cg = new Grid();
            cg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
            info.Children.Add(new TextBlock
            {
                Text = "Node.js & npm packages",
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary
            });
            lblDepsStatusDetail = new TextBlock
            {
                Text = "Checking environment...",
                FontSize = 11.5,
                Foreground = BTextMuted,
                Margin = new Thickness(0, 2, 0, 0)
            };
            info.Children.Add(lblDepsStatusDetail);
            Grid.SetColumn(info, 0);
            cg.Children.Add(info);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnNodeSite = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "nodejs.org",
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnNodeSite.Click += delegate { OpenUrl("https://nodejs.org"); };
            actions.Children.Add(btnNodeSite);

            btnRunNpm = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Run npm install"
            };
            btnRunNpm.Click += delegate { RunNpmInstall(); };
            actions.Children.Add(btnRunNpm);

            Grid.SetColumn(actions, 1);
            cg.Children.Add(actions);
            card.Child = cg;
            page.Children.Add(card);

            page.Children.Add(CreateSectionLabel("Console output"));

            Border codeCard = new Border
            {
                Background = BCodeBg,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7)
            };
            StackPanel codeStack = new StackPanel();

            Border codeHeader = new Border
            {
                Background = BCardSubtle,
                BorderBrush = BBorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Padding = new Thickness(12, 6, 10, 6)
            };
            DockPanel chDock = new DockPanel();
            Button btnCopyNpm = new Button
            {
                Style = (Style)FindResource("BtnSubtle"),
                Content = "Copy command"
            };
            btnCopyNpm.Click += delegate { CopyText("npm install", "Copied 'npm install'"); };
            DockPanel.SetDock(btnCopyNpm, Dock.Right);
            chDock.Children.Add(btnCopyNpm);
            chDock.Children.Add(new TextBlock
            {
                Text = "npm install",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                Foreground = BTextMuted,
                VerticalAlignment = VerticalAlignment.Center
            });
            codeHeader.Child = chDock;
            codeStack.Children.Add(codeHeader);

            txtNpmOutput = new TextBox
            {
                Text = Directory.Exists(System.IO.Path.Combine(repoDir, "node_modules"))
                    ? "node_modules is present in " + repoDir
                    : "Click 'Run npm install' above or run 'npm install' in your terminal.",
                Background = Brushes.Transparent,
                Foreground = BTextBody,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                Padding = new Thickness(12, 10, 12, 10),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 140,
                MaxHeight = 230
            };
            codeStack.Children.Add(txtNpmOutput);
            codeCard.Child = codeStack;
            page.Children.Add(codeCard);

            return page;
        }

        // ====================================================================
        // PAGE 3: AI CLIENT CONFIGURATION
        // ====================================================================
        private UIElement BuildPage3Clients()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(CreatePageHeader(
                "AI Client Configuration",
                "Select your coding assistant to generate or automatically apply the MCP configuration pointing to server.js."
            ));

            // Segmented Selector
            WrapPanel segBar = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            string[,] items = new string[,]
            {
                { "opencode", "OpenCode" },
                { "claude-desktop", "Claude Desktop" },
                { "cursor", "Cursor" },
                { "windsurf", "Windsurf" },
                { "claude-cli", "Claude Code CLI" },
                { "codex-cli", "Codex CLI" }
            };

            for (int i = 0; i < items.GetLength(0); i++)
            {
                string id = items[i, 0];
                string label = items[i, 1];
                Button b = new Button
                {
                    Style = (Style)FindResource("BtnSecondary"),
                    Content = label,
                    Margin = new Thickness(0, 0, 6, 6)
                };
                string captured = id;
                b.Click += delegate { SelectClient(captured); };
                clientButtons[id] = b;
                segBar.Children.Add(b);
            }
            page.Children.Add(segBar);

            // Config Block
            Border cfgCard = new Border
            {
                Background = BCodeBg,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            StackPanel cfgStack = new StackPanel();

            Border cfgTop = new Border
            {
                Background = BCard,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(14, 10, 14, 10)
            };
            Grid topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftMeta = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
            dotClientConfigured = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = BNeutralDot,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            lblClientConfigured = new TextBlock
            {
                Text = "Not configured",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusRow.Children.Add(dotClientConfigured);
            statusRow.Children.Add(lblClientConfigured);
            leftMeta.Children.Add(statusRow);

            lblClientFile = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = BTextDim
            };
            leftMeta.Children.Add(lblClientFile);
            Grid.SetColumn(leftMeta, 0);
            topGrid.Children.Add(leftMeta);

            StackPanel rightBtns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnCopyCfg = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "Copy",
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnCopyCfg.Click += delegate { CopyText(txtClientCode.Text, "Copied configuration to clipboard"); };
            rightBtns.Children.Add(btnCopyCfg);

            btnApplyClient = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Apply Config"
            };
            btnApplyClient.Click += delegate { ApplySelectedClientConfig(); };
            rightBtns.Children.Add(btnApplyClient);

            Grid.SetColumn(rightBtns, 1);
            topGrid.Children.Add(rightBtns);
            cfgTop.Child = topGrid;
            cfgStack.Children.Add(cfgTop);

            txtClientCode = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = BTextBody,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Padding = new Thickness(14, 12, 14, 12),
                IsReadOnly = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 190
            };
            cfgStack.Children.Add(txtClientCode);

            cfgCard.Child = cfgStack;
            page.Children.Add(cfgCard);

            return page;
        }

        private void SelectClient(string id)
        {
            selectedClient = id;
            foreach (KeyValuePair<string, Button> kv in clientButtons)
            {
                kv.Value.Style = (Style)FindResource(kv.Key == id ? "BtnPrimary" : "BtnSecondary");
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (id == "opencode")
            {
                string p = System.IO.Path.Combine(home, ".config", "opencode", "opencode.json");
                lblClientFile.Text = p;
                btnApplyClient.Content = "Write to opencode.json";
                txtClientCode.Text =
"{\r\n" +
"  \"$schema\": \"https://opencode.ai/config.json\",\r\n" +
"  \"mcp\": {\r\n" +
"    \"Roblox_2021\": {\r\n" +
"      \"type\": \"local\",\r\n" +
"      \"command\": [\"node\", \"" + serverJsSlash + "\"],\r\n" +
"      \"enabled\": true\r\n" +
"    }\r\n" +
"  }\r\n" +
"}";
                UpdateClientBadge(p);
            }
            else if (id == "claude-desktop")
            {
                string p = System.IO.Path.Combine(appData, "Claude", "claude_desktop_config.json");
                lblClientFile.Text = p;
                btnApplyClient.Content = "Write to Claude config";
                txtClientCode.Text = GetMcpServersSnippet();
                UpdateClientBadge(p);
            }
            else if (id == "cursor")
            {
                string p = System.IO.Path.Combine(home, ".cursor", "mcp.json");
                lblClientFile.Text = p;
                btnApplyClient.Content = "Write to Cursor config";
                txtClientCode.Text = GetMcpServersSnippet();
                UpdateClientBadge(p);
            }
            else if (id == "windsurf")
            {
                string p = System.IO.Path.Combine(home, ".codeium", "windsurf", "mcp_config.json");
                lblClientFile.Text = p;
                btnApplyClient.Content = "Write to Windsurf config";
                txtClientCode.Text = GetMcpServersSnippet();
                UpdateClientBadge(p);
            }
            else if (id == "claude-cli")
            {
                lblClientFile.Text = "Terminal command";
                btnApplyClient.Content = "Run in terminal";
                txtClientCode.Text = "claude mcp add roblox-2021 -- node \"" + serverJsSlash + "\"";
                dotClientConfigured.Fill = BNeutralDot;
                lblClientConfigured.Text = "CLI command";
                lblClientConfigured.Foreground = BTextMuted;
            }
            else if (id == "codex-cli")
            {
                lblClientFile.Text = "Terminal command";
                btnApplyClient.Content = "Run in terminal";
                txtClientCode.Text = "codex mcp add roblox-2021 -- node \"" + serverJsSlash + "\"";
                dotClientConfigured.Fill = BNeutralDot;
                lblClientConfigured.Text = "CLI command";
                lblClientConfigured.Foreground = BTextMuted;
            }
        }

        private string GetMcpServersSnippet()
        {
            return
"{\r\n" +
"  \"mcpServers\": {\r\n" +
"    \"roblox-2021\": {\r\n" +
"      \"command\": \"node\",\r\n" +
"      \"args\": [\"" + serverJsSlash + "\"]\r\n" +
"    }\r\n" +
"  }\r\n" +
"}";
        }

        private void UpdateClientBadge(string path)
        {
            if (HasMcpInFile(path))
            {
                dotClientConfigured.Fill = BGreen;
                lblClientConfigured.Text = "Configured";
                lblClientConfigured.Foreground = BTextPrimary;
            }
            else
            {
                dotClientConfigured.Fill = BNeutralDot;
                lblClientConfigured.Text = "Not configured";
                lblClientConfigured.Foreground = BTextMuted;
            }
        }

        // ====================================================================
        // PAGE 4: BROWSER EXTENSION (ZEROSCRIPT)
        // ====================================================================
        private UIElement BuildPage4BrowserExtension()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(CreatePageHeader(
                "Browser Extension (ZeroScript)",
                "Use web AI chats (Claude.ai, DeepSeek, ChatGPT, Gemini, Qwen, Kimi) directly with 2021 Roblox Studio using the included unpacked browser extension and local WebSocket bridge."
            ));

            Border topCard = CreateSurfaceCard();
            Grid tg = new Grid();
            tg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
            info.Children.Add(new TextBlock
            {
                Text = "ZeroScript WebSocket Bridge (start.bat)",
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary
            });
            info.Children.Add(new TextBlock
            {
                Text = "Listens on ws://127.0.0.1:17613 and spawns server.js on port 3021.",
                FontSize = 11.5,
                Foreground = BTextMuted,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(info, 0);
            tg.Children.Add(info);

            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Button btnExtFolder = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "Open extension/",
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnExtFolder.Click += delegate { OpenFolder(extensionDir); };
            actions.Children.Add(btnExtFolder);

            Button btnRunStart = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Start Bridge (start.bat)"
            };
            btnRunStart.Click += delegate { RunStartBat(); };
            actions.Children.Add(btnRunStart);

            Grid.SetColumn(actions, 1);
            tg.Children.Add(actions);
            topCard.Child = tg;
            page.Children.Add(topCard);

            Border stepsCard = CreateSurfaceCard();
            stepsCard.Padding = new Thickness(16, 6, 16, 6);
            StackPanel list = new StackPanel();

            list.Children.Add(CreateStepRow(
                1,
                "Load the unpacked extension in Chrome or Edge",
                "Open chrome://extensions or edge://extensions, enable Developer mode (top right), click Load unpacked, and select the extension folder:",
                extensionDir,
                true
            ));
            list.Children.Add(CreateRowDivider());

            list.Children.Add(CreateStepRow(
                2,
                "Start the local bridge",
                "Click 'Start Bridge (start.bat)' above or double-click start.bat. Keep the terminal window minimized while working.",
                null,
                false
            ));
            list.Children.Add(CreateRowDivider());

            list.Children.Add(CreateStepRow(
                3,
                "Open a supported AI chat and click Start session",
                "Navigate to any supported web chat below and click 'Start session' in the ZeroScript bar above the composer.",
                null,
                false
            ));

            WrapPanel providers = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(32, 0, 0, 10) };
            string[,] links = new string[,]
            {
                { "Claude.ai", "https://claude.ai/new" },
                { "DeepSeek", "https://chat.deepseek.com/" },
                { "ChatGPT", "https://chatgpt.com/" },
                { "Gemini", "https://gemini.google.com/app" },
                { "Qwen", "https://chat.qwen.ai/" },
                { "Kimi", "https://www.kimi.ai/" }
            };
            for (int i = 0; i < links.GetLength(0); i++)
            {
                string name = links[i, 0];
                string url = links[i, 1];
                Button b = new Button
                {
                    Style = (Style)FindResource("BtnSubtle"),
                    Content = name + " ->",
                    Margin = new Thickness(0, 0, 6, 6)
                };
                string u = url;
                b.Click += delegate { OpenUrl(u); };
                providers.Children.Add(b);
            }
            list.Children.Add(providers);

            stepsCard.Child = list;
            page.Children.Add(stepsCard);

            return page;
        }

        // ====================================================================
        // PAGE 5: CONNECTION TESTER & TOOLS
        // ====================================================================
        private UIElement BuildPage5Diagnostics()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(CreatePageHeader(
                "Connection & Tools",
                "Test two-way communication with 2021 Roblox Studio on port 3021 and browse the built-in MCP tools."
            ));

            WrapPanel bar = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            Button bStart = new Button { Style = (Style)FindResource("BtnSecondary"), Content = "Start Server (:3021)", Margin = new Thickness(0, 0, 6, 6) };
            bStart.Click += delegate { StartBackgroundServer(true); };
            bar.Children.Add(bStart);

            Button bPing = new Button { Style = (Style)FindResource("BtnPrimary"), Content = "Ping Studio", Margin = new Thickness(0, 0, 6, 6) };
            bPing.Click += delegate { RunStudioTest("ping"); };
            bar.Children.Add(bPing);

            Button bTree = new Button { Style = (Style)FindResource("BtnSecondary"), Content = "Workspace Tree", Margin = new Thickness(0, 0, 6, 6) };
            bTree.Click += delegate { RunStudioTest("tree"); };
            bar.Children.Add(bTree);

            Button bLogs = new Button { Style = (Style)FindResource("BtnSecondary"), Content = "Output Log", Margin = new Thickness(0, 0, 6, 6) };
            bLogs.Click += delegate { RunStudioTest("logs"); };
            bar.Children.Add(bLogs);

            Button bShot = new Button { Style = (Style)FindResource("BtnSecondary"), Content = "Capture Screenshot", Margin = new Thickness(0, 0, 6, 6) };
            bShot.Click += delegate { RunStudioTest("screenshot"); };
            bar.Children.Add(bShot);

            page.Children.Add(bar);

            Border outCard = new Border
            {
                Background = BCodeBg,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Margin = new Thickness(0, 0, 0, 16)
            };
            StackPanel outStack = new StackPanel();
            txtTestOutput = new TextBox
            {
                Text = "Open a place in 2021 Roblox Studio, then click 'Ping Studio' to verify execution.",
                Background = Brushes.Transparent,
                Foreground = BTextBody,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                Padding = new Thickness(12, 10, 12, 10),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 95,
                MaxHeight = 190
            };
            outStack.Children.Add(txtTestOutput);

            shotPreviewBorder = new Border
            {
                BorderBrush = BBorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(10),
                Visibility = Visibility.Collapsed
            };
            imgShotPreview = new Image { MaxHeight = 220, Stretch = Stretch.Uniform };
            shotPreviewBorder.Child = imgShotPreview;
            outStack.Children.Add(shotPreviewBorder);

            outCard.Child = outStack;
            page.Children.Add(outCard);

            page.Children.Add(CreateSectionLabel("Available MCP tools"));

            Border toolsTable = CreateSurfaceCard();
            toolsTable.Padding = new Thickness(14, 4, 14, 4);
            StackPanel tList = new StackPanel();

            string[,] tools = new string[,]
            {
                { "execute_luau", "Execute Luau in Studio Edit mode with ChangeHistoryService undo waypoints", "Use execute_luau to create a part in Workspace" },
                { "screen_capture", "Capture a screenshot of the 2021 Roblox Studio window", "Take a screen_capture of my Studio window" },
                { "read_script / write_script", "Read or overwrite the .Source of any Script, LocalScript, or ModuleScript", "Read ServerScriptService scripts and review them" },
                { "multi_edit", "Apply exact string replacements or create scripts in Studio", "Edit my script using multi_edit" },
                { "script_grep", "Search all scripts in the place for a text or regex pattern", "Use script_grep to search for RemoteEvent" },
                { "inspect_instance", "Read properties, CFrames, colors, attributes, tags, and children", "Use inspect_instance on Workspace.Baseplate" },
                { "audit_scene_assets", "Scan sounds, meshes, decals, and animations for external asset IDs", "Run audit_scene_assets on Workspace" },
                { "run_playtest", "Start Play Solo (F5) or Run (F8), monitor errors, screenshot, and stop (Shift+F5)", "Run a 5-second test with run_playtest" }
            };

            for (int i = 0; i < tools.GetLength(0); i++)
            {
                if (i > 0) tList.Children.Add(CreateRowDivider());
                string name = tools[i, 0];
                string desc = tools[i, 1];
                string prompt = tools[i, 2];

                Grid row = new Grid { Margin = new Thickness(0, 7, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock nLbl = new TextBlock
                {
                    Text = name,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BTextPrimary,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nLbl, 0);
                row.Children.Add(nLbl);

                TextBlock dLbl = new TextBlock
                {
                    Text = desc,
                    FontSize = 11.5,
                    Foreground = BTextMuted,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(8, 0, 10, 0)
                };
                Grid.SetColumn(dLbl, 1);
                row.Children.Add(dLbl);

                Button btnP = new Button
                {
                    Style = (Style)FindResource("BtnSubtle"),
                    Content = "Copy prompt"
                };
                string cp = prompt;
                btnP.Click += delegate { CopyText(cp, "Copied prompt"); };
                Grid.SetColumn(btnP, 2);
                row.Children.Add(btnP);

                tList.Children.Add(row);
            }

            toolsTable.Child = tList;
            page.Children.Add(toolsTable);

            return page;
        }

        private Border BuildFooter()
        {
            Border footer = new Border
            {
                Background = BSurface,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 0, 20, 0)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            lblFooterStep = new TextBlock
            {
                Text = "Step 1 of 5",
                FontSize = 11.5,
                Foreground = BTextDim,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblFooterStep, 0);
            g.Children.Add(lblFooterStep);

            StackPanel nav = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            btnPrev = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Content = "Back",
                Width = 72,
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnPrev.Click += delegate { SelectStep(currentStep - 1); };
            nav.Children.Add(btnPrev);

            btnNext = new Button
            {
                Style = (Style)FindResource("BtnPrimary"),
                Content = "Next",
                Width = 80
            };
            btnNext.Click += delegate
            {
                if (currentStep < stepPages.Count) SelectStep(currentStep + 1);
                else ShowToast("Setup complete.");
            };
            nav.Children.Add(btnNext);

            Grid.SetColumn(nav, 2);
            g.Children.Add(nav);

            footer.Child = g;
            return footer;
        }

        // ====================================================================
        // SHARED LAYOUT HELPERS
        // ====================================================================
        private StackPanel CreatePageHeader(string title, string subtitle)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary,
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12.5,
                Foreground = BTextMuted,
                TextWrapping = TextWrapping.Wrap
            });
            return sp;
        }

        private TextBlock CreateSectionLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextDim,
                Margin = new Thickness(0, 4, 0, 8)
            };
        }

        private Border CreateSurfaceCard()
        {
            return new Border
            {
                Background = BCard,
                BorderBrush = BBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 13, 16, 13),
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private Border CreateRowDivider()
        {
            return new Border
            {
                Height = 1,
                Background = BBorderSubtle,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private Grid CreateStepRow(int num, string title, string desc, string codeSnippet, bool showCopy)
        {
            Grid g = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border numBadge = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 39)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 0, 0)
            };
            numBadge.Child = new TextBlock
            {
                Text = num.ToString(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(numBadge, 0);
            g.Children.Add(numBadge);

            StackPanel body = new StackPanel();
            body.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BTextPrimary,
                Margin = new Thickness(0, 0, 0, 2)
            });
            body.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 12,
                Foreground = BTextMuted,
                TextWrapping = TextWrapping.Wrap
            });

            if (!String.IsNullOrEmpty(codeSnippet))
            {
                Border codeBox = new Border
                {
                    Background = BCodeBg,
                    BorderBrush = BBorderSubtle,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 6, 8, 6),
                    Margin = new Thickness(0, 7, 0, 0)
                };
                DockPanel dp = new DockPanel();
                if (showCopy)
                {
                    Button btnCopy = new Button
                    {
                        Style = (Style)FindResource("BtnSubtle"),
                        Content = "Copy",
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    string captured = codeSnippet;
                    btnCopy.Click += delegate { CopyText(captured, "Copied to clipboard"); };
                    DockPanel.SetDock(btnCopy, Dock.Right);
                    dp.Children.Add(btnCopy);
                }
                dp.Children.Add(new TextBlock
                {
                    Text = codeSnippet,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11.5,
                    Foreground = BTextBody,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
                codeBox.Child = dp;
                body.Children.Add(codeBox);
            }

            Grid.SetColumn(body, 1);
            g.Children.Add(body);
            return g;
        }

        // ====================================================================
        // OPERATIONS
        // ====================================================================
        private void ShowToast(string msg)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                toastLabel.Text = msg;
                toastBorder.Visibility = Visibility.Visible;
                if (toastTimer == null)
                {
                    toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
                    toastTimer.Tick += delegate
                    {
                        toastBorder.Visibility = Visibility.Collapsed;
                        toastTimer.Stop();
                    };
                }
                toastTimer.Stop();
                toastTimer.Start();
            }));
        }

        private void CopyText(string text, string notice)
        {
            try
            {
                Clipboard.SetText(text);
                ShowToast(notice);
            }
            catch (Exception ex)
            {
                ShowToast("Clipboard error: " + ex.Message);
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex)
            {
                ShowToast("Error: " + ex.Message);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowToast("Error: " + ex.Message);
            }
        }

        private bool InstallPlugin()
        {
            try
            {
                if (!File.Exists(pluginSourcePath))
                {
                    ShowToast("MCPBridge2021.lua not found in " + repoDir);
                    return false;
                }
                Directory.CreateDirectory(pluginsFolder);
                string dest = System.IO.Path.Combine(pluginsFolder, "MCPBridge2021.lua");
                File.Copy(pluginSourcePath, dest, true);

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                foreach (string alt in new string[] { "Aisaka", "Octane", "RobloxStudio" })
                {
                    string altDir = System.IO.Path.Combine(localAppData, alt);
                    if (Directory.Exists(altDir))
                    {
                        try
                        {
                            string altP = System.IO.Path.Combine(altDir, "Plugins");
                            Directory.CreateDirectory(altP);
                            File.Copy(pluginSourcePath, System.IO.Path.Combine(altP, "MCPBridge2021.lua"), true);
                        }
                        catch { }
                    }
                }

                ShowToast("Installed MCPBridge2021.lua to Roblox Plugins");
                RefreshStatusAsync();
                return true;
            }
            catch (Exception ex)
            {
                ShowToast("Install failed: " + ex.Message);
                return false;
            }
        }

        private void RunNpmInstall()
        {
            btnRunNpm.IsEnabled = false;
            txtNpmOutput.Text = "> npm install\r\nRunning in " + repoDir + " ...\r\n";

            Task.Run(delegate
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c npm install",
                        WorkingDirectory = repoDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                    using (Process p = Process.Start(psi))
                    {
                        string o = p.StandardOutput.ReadToEnd();
                        string e = p.StandardError.ReadToEnd();
                        p.WaitForExit();
                        Dispatcher.Invoke(new Action(delegate
                        {
                            txtNpmOutput.Text = (o + "\r\n" + e).Trim() +
                                (p.ExitCode == 0 ? "\r\n\r\nDone (exit code 0)." : "\r\n\r\nExited with code " + p.ExitCode);
                            btnRunNpm.IsEnabled = true;
                            if (p.ExitCode == 0) ShowToast("Dependencies installed");
                            RefreshStatusAsync();
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        txtNpmOutput.Text += "\r\nError: " + ex.Message;
                        btnRunNpm.IsEnabled = true;
                    }));
                }
            });
        }

        private void ApplySelectedClientConfig()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (selectedClient == "claude-cli" || selectedClient == "codex-cli")
            {
                string cmd = txtClientCode.Text;
                Task.Run(delegate
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c " + cmd,
                            WorkingDirectory = repoDir,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process p = Process.Start(psi))
                        {
                            p.WaitForExit(10000);
                        }
                        ShowToast("Executed CLI registration");
                    }
                    catch (Exception ex)
                    {
                        ShowToast("CLI error: " + ex.Message);
                    }
                });
                return;
            }

            string path = "";
            bool isOpenCode = false;
            if (selectedClient == "opencode")
            {
                path = System.IO.Path.Combine(home, ".config", "opencode", "opencode.json");
                isOpenCode = true;
            }
            else if (selectedClient == "claude-desktop")
            {
                path = System.IO.Path.Combine(appData, "Claude", "claude_desktop_config.json");
            }
            else if (selectedClient == "cursor")
            {
                path = System.IO.Path.Combine(home, ".cursor", "mcp.json");
            }
            else if (selectedClient == "windsurf")
            {
                path = System.IO.Path.Combine(home, ".codeium", "windsurf", "mcp_config.json");
            }

            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                if (!File.Exists(path) || String.IsNullOrWhiteSpace(File.ReadAllText(path)))
                {
                    File.WriteAllText(path, txtClientCode.Text + "\r\n", Encoding.UTF8);
                }
                else
                {
                    if (!MergeJsonViaNode(path, isOpenCode))
                    {
                        File.Copy(path, path + ".bak", true);
                        File.WriteAllText(path, txtClientCode.Text + "\r\n", Encoding.UTF8);
                    }
                }
                SelectClient(selectedClient);
                ShowToast("Updated " + System.IO.Path.GetFileName(path));
                RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                ShowToast("Error writing config: " + ex.Message);
            }
        }

        private bool MergeJsonViaNode(string targetPath, bool isOpenCode)
        {
            try
            {
                string escapedTarget = targetPath.Replace("\\", "\\\\");
                string escapedServer = serverJsSlash.Replace("\\", "/");
                string script = isOpenCode
                    ? String.Format("const fs=require('fs');const p='{0}';let c={{}};try{{c=JSON.parse(fs.readFileSync(p,'utf8'));}}catch(e){{process.exit(2);}}if(!c.$schema)c.$schema='https://opencode.ai/config.json';if(!c.mcp)c.mcp={{}};c.mcp.Roblox_2021={{type:'local',command:['node','{1}'],enabled:true}};fs.writeFileSync(p,JSON.stringify(c,null,2)+'\\n');", escapedTarget, escapedServer)
                    : String.Format("const fs=require('fs');const p='{0}';let c={{}};try{{c=JSON.parse(fs.readFileSync(p,'utf8'));}}catch(e){{process.exit(2);}}if(!c.mcpServers)c.mcpServers={{}};c.mcpServers['roblox-2021']={{command:'node',args:['{1}']}};fs.writeFileSync(p,JSON.stringify(c,null,2)+'\\n');", escapedTarget, escapedServer);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "-e \"" + script + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RunStartBat()
        {
            try
            {
                if (!File.Exists(startBatPath))
                {
                    ShowToast("start.bat not found");
                    return;
                }
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c start \"ZeroScript Bridge\" cmd.exe /k \"" + startBatPath + "\"",
                    WorkingDirectory = repoDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                ShowToast("Started start.bat bridge");
            }
            catch (Exception ex)
            {
                ShowToast("Error: " + ex.Message);
            }
        }

        private void StartBackgroundServer(bool notify)
        {
            if (IsPortOpen(3021))
            {
                if (notify) ShowToast("Port 3021 is already active");
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "\"" + serverJsPath + "\" --ui",
                    WorkingDirectory = repoDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                bgServerProc = Process.Start(psi);
                if (notify) ShowToast("Started server.js on port 3021");
                RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                if (notify) ShowToast("Failed to start server.js: " + ex.Message);
            }
        }

        private void RunQuickSetup()
        {
            InstallPlugin();
            StartBackgroundServer(false);
            if (!Directory.Exists(System.IO.Path.Combine(repoDir, "node_modules")))
            {
                SelectStep(2);
                RunNpmInstall();
            }
            else
            {
                SelectStep(3);
                ShowToast("Plugin installed and server active on :3021");
            }
        }

        private void RunStudioTest(string mode)
        {
            StartBackgroundServer(false);
            txtTestOutput.Text = "Dispatching request to http://127.0.0.1:3021/api/call ...";
            shotPreviewBorder.Visibility = Visibility.Collapsed;

            string body = "{\"tool\":\"get_studio_state\",\"args\":{}}";
            if (mode == "ping")
            {
                body = "{\"tool\":\"execute_luau\",\"args\":{\"code\":\"print('[MCP] Connection verified') return 'Connected to 2021 Studio | Place: ' .. tostring(game.Name) .. ' | Workspace children: ' .. tostring(#workspace:GetChildren())\"}}";
            }
            else if (mode == "tree")
            {
                body = "{\"tool\":\"get_tree\",\"args\":{\"root\":\"game.Workspace\",\"maxDepth\":2}}";
            }
            else if (mode == "logs")
            {
                body = "{\"tool\":\"get_output_log\",\"args\":{\"limit\":25}}";
            }
            else if (mode == "screenshot")
            {
                body = "{\"tool\":\"screen_capture\",\"args\":{}}";
            }

            Task.Run(delegate
            {
                try
                {
                    Thread.Sleep(200);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3021/api/call");
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Timeout = 15000;

                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    req.ContentLength = bytes.Length;
                    using (Stream s = req.GetRequestStream())
                    {
                        s.Write(bytes, 0, bytes.Length);
                    }

                    string respStr = "";
                    try
                    {
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            respStr = sr.ReadToEnd();
                        }
                    }
                    catch (WebException wex)
                    {
                        if (wex.Response != null)
                        {
                            using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                            {
                                respStr = sr.ReadToEnd();
                            }
                        }
                        else throw;
                    }

                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (mode == "screenshot" && respStr.Contains("imageBase64"))
                        {
                            Match m = Regex.Match(respStr, "\"imageBase64\"\\s*:\\s*\"([^\"]+)\"");
                            if (m.Success)
                            {
                                byte[] raw = Convert.FromBase64String(m.Groups[1].Value);
                                BitmapImage bmp = new BitmapImage();
                                using (MemoryStream ms = new MemoryStream(raw))
                                {
                                    bmp.BeginInit();
                                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                                    bmp.StreamSource = ms;
                                    bmp.EndInit();
                                }
                                imgShotPreview.Source = bmp;
                                shotPreviewBorder.Visibility = Visibility.Visible;
                                txtTestOutput.Text = "Captured screenshot from 2021 Roblox Studio.";
                                return;
                            }
                        }

                        txtTestOutput.Text = respStr
                            .Replace("\\r\\n", "\r\n")
                            .Replace("\\n", "\r\n")
                            .Replace("\\t", "\t")
                            .Replace("\\\"", "\"");
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        txtTestOutput.Text = "Error: " + ex.Message +
                            "\r\nEnsure MCPBridge2021.lua is installed and a place is open in 2021 Roblox Studio.";
                    }));
                }
            });
        }

        // ====================================================================
        // STATUS POLLING
        // ====================================================================
        private void RefreshStatusAsync()
        {
            Task.Run(delegate
            {
                string destPlugin = System.IO.Path.Combine(pluginsFolder, "MCPBridge2021.lua");
                bool plugOk = File.Exists(destPlugin);
                bool nmOk = Directory.Exists(System.IO.Path.Combine(repoDir, "node_modules"));

                if (!isNodeInstalled)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "node",
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        using (Process p = Process.Start(psi))
                        {
                            nodeVersionStr = p.StandardOutput.ReadToEnd().Trim();
                            p.WaitForExit(2000);
                            isNodeInstalled = (p.ExitCode == 0 && !String.IsNullOrEmpty(nodeVersionStr));
                        }
                    }
                    catch { isNodeInstalled = false; }
                }

                bool p3021 = IsPortOpen(3021);
                bool p17613 = IsPortOpen(17613);
                bool studioProc = IsStudioRunning();
                bool studioAlive = false;

                if (p3021)
                {
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3021/api/status");
                        req.Method = "GET";
                        req.Timeout = 1000;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            string j = sr.ReadToEnd();
                            if (j.Contains("\"studioConnected\":true")) studioAlive = true;
                        }
                    }
                    catch { }
                }

                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                bool anyClient =
                    HasMcpInFile(System.IO.Path.Combine(home, ".config", "opencode", "opencode.json")) ||
                    HasMcpInFile(System.IO.Path.Combine(appData, "Claude", "claude_desktop_config.json")) ||
                    HasMcpInFile(System.IO.Path.Combine(home, ".cursor", "mcp.json")) ||
                    HasMcpInFile(System.IO.Path.Combine(home, ".codeium", "windsurf", "mcp_config.json"));

                isPluginInstalled = plugOk;
                isNodeModulesInstalled = nmOk;
                isMcpPortListening = p3021;
                isStudioConnected = studioAlive;
                isStudioProcessRunning = studioProc;
                isZeroScriptRunning = p17613;

                Dispatcher.Invoke(new Action(delegate
                {
                    // Titlebar summary
                    if (isStudioConnected)
                    {
                        titleStatusDot.Fill = BGreen;
                        titleStatusText.Text = "Connected · Roblox Studio ready";
                    }
                    else if (isMcpPortListening)
                    {
                        titleStatusDot.Fill = BAmber;
                        titleStatusText.Text = studioProc ? "Bridge ready · open a place in Studio" : "Bridge ready · waiting for Studio";
                    }
                    else
                    {
                        titleStatusDot.Fill = BNeutralDot;
                        titleStatusText.Text = isPluginInstalled ? "Plugin installed · standby" : "Setup required";
                    }

                    // Sidebar health rows
                    sideDotPlugin.Fill = isPluginInstalled ? BGreen : BAmber;
                    sideTxtPlugin.Text = isPluginInstalled ? "installed" : "missing";

                    sideDotNode.Fill = (isNodeInstalled && isNodeModulesInstalled) ? BGreen : BAmber;
                    sideTxtNode.Text = isNodeInstalled ? (isNodeModulesInstalled ? nodeVersionStr : "no deps") : "missing";

                    sideDotPort.Fill = isMcpPortListening ? BGreen : BNeutralDot;
                    sideTxtPort.Text = isMcpPortListening ? "online" : "standby";

                    sideDotStudio.Fill = isStudioConnected ? BGreen : (isStudioProcessRunning ? BAmber : BNeutralDot);
                    sideTxtStudio.Text = isStudioConnected ? "connected" : (isStudioProcessRunning ? "no place" : "offline");

                    // Page 1 & 2 labels
                    if (lblPluginStatusDetail != null)
                    {
                        lblPluginStatusDetail.Text = isPluginInstalled
                            ? "Installed in " + pluginsFolder
                            : "Target: " + pluginsFolder;
                    }
                    if (btnInstallPlugin != null)
                    {
                        btnInstallPlugin.Content = isPluginInstalled ? "Reinstall Plugin" : "Install Plugin";
                    }
                    if (lblDepsStatusDetail != null)
                    {
                        lblDepsStatusDetail.Text = (isNodeInstalled ? ("Node " + nodeVersionStr + " · ") : "Node.js not found · ") +
                            (isNodeModulesInstalled ? "node_modules installed" : "node_modules missing");
                    }

                    // Step completion checkmarks
                    UpdateStepCheck(0, isPluginInstalled);
                    UpdateStepCheck(1, isNodeInstalled && isNodeModulesInstalled);
                    UpdateStepCheck(2, anyClient);
                    UpdateStepCheck(3, isZeroScriptRunning);
                    UpdateStepCheck(4, isStudioConnected);
                }));
            });
        }

        private void UpdateStepCheck(int idx, bool done)
        {
            if (idx < 0 || idx >= navCheckTexts.Count) return;
            navCheckTexts[idx].Text = done ? "✓" : (idx + 1).ToString();
            navCheckTexts[idx].Foreground = done ? BGreen : BTextMuted;
            navCheckBoxes[idx].BorderBrush = done
                ? new SolidColorBrush(Color.FromArgb(90, 52, 211, 153))
                : new SolidColorBrush(Color.FromRgb(46, 46, 52));
        }

        private bool HasMcpInFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                string s = File.ReadAllText(path);
                return s.Contains("Roblox_2021") || s.Contains("roblox-2021") || s.IndexOf("server.js", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private bool IsPortOpen(int port)
        {
            try
            {
                IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
                foreach (IPEndPoint ep in props.GetActiveTcpListeners())
                {
                    if (ep.Port == port) return true;
                }
            }
            catch { }
            return false;
        }

        private bool IsStudioRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    string n = p.ProcessName;
                    if (n.IndexOf("AisakaStudio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("RobloxStudio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Octane", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            try
            {
                if (bgServerProc != null && !bgServerProc.HasExited)
                {
                    bgServerProc.Kill();
                }
            }
            catch { }
        }
    }
}
