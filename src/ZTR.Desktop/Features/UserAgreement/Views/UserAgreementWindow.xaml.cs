using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Desktop.Features.UserAgreement.Services;

namespace ZTR.Desktop.Features.UserAgreement.Views;

public partial class UserAgreementWindow : Window
{
    private readonly IUserAgreementService _userAgreementService;

    private readonly DispatcherTimer _countdownTimer;

    private int _remainingSeconds = 120;

    private bool _hasScrolledToBottom;

    private double _lastVerticalOffset;

    private const double MaxScrollDelta = 28;

    private string AgreementVersion => _userAgreementService.CurrentAgreementVersion;

    private readonly DispatcherTimer _shakeTimer;

    private readonly List<(Window Window, double OriginalLeft, double OriginalTop, List<TranslateTransform> ContentTransforms)> _trollWindows = [];

    private readonly List<(UIElement Element, TranslateTransform Transform)> _shakeElements = [];

    private bool _isCountdownPaused;

    private int _shakeRemainingMs;

    private double _originalLeft;

    private double _originalTop;

    private static readonly Random _random = new();

    public UserAgreementWindow() : this(null)
    {
    }

    public UserAgreementWindow(IUserAgreementService? agreementService)
    {
        InitializeComponent();

        _userAgreementService = agreementService ?? ResolveAgreementService();

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += CountdownTimer_Tick;

        _shakeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _shakeTimer.Tick += ShakeTimer_Tick;

        Loaded += UserAgreementWindow_Loaded;
        Activated += UserAgreementWindow_Activated;
        Deactivated += UserAgreementWindow_Deactivated;
        Closed += UserAgreementWindow_Closed;

        ForceLog.Write("[USER-AGREEMENT] UserAgreementWindow 已初始化");
    }

    private static IUserAgreementService ResolveAgreementService()
    {
        try
        {
            var service = (IUserAgreementService?)App.Services.GetService(typeof(IUserAgreementService));
            if (service != null)
            {
                ForceLog.Write("[USER-AGREEMENT] 从 DI 容器解析 IUserAgreementService 成功");
                return service;
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[USER-AGREEMENT] DI 容器解析失败，使用新实例: {ex.Message}");
        }

        ForceLog.Write("[USER-AGREEMENT] 创建新的 UserAgreementService 实例");
        var newService = new UserAgreementService();
        newService.Load();
        return newService;
    }

    private void UserAgreementWindow_Closed(object? sender, EventArgs e)
    {
        _countdownTimer.Stop();
        _countdownTimer.Tick -= CountdownTimer_Tick;

        _shakeTimer.Stop();
        _shakeTimer.Tick -= ShakeTimer_Tick;

        Loaded -= UserAgreementWindow_Loaded;
        Activated -= UserAgreementWindow_Activated;
        Deactivated -= UserAgreementWindow_Deactivated;
        Closed -= UserAgreementWindow_Closed;

        foreach (var (w, _, _, _) in _trollWindows)
        {
            try { w.Close(); } catch { }
        }
        _trollWindows.Clear();

        _shakeElements.Clear();

        ForceLog.Write("[USER-AGREEMENT] UserAgreementWindow 已关闭并清理资源");
    }

    private void UserAgreementWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCountdownDisplay();
        _countdownTimer.Start();
        AgreeButton.IsEnabled = false;

        TitleBar.MouseLeftButtonDown += (_, _) => DragMove();

        var scrollViewer = FindScrollViewer(AgreementContent);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += AgreementScrollViewer_ScrollChanged;
            _lastVerticalOffset = scrollViewer.VerticalOffset;
        }

        ForceLog.Write("[USER-AGREEMENT] 窗口已加载，倒计时已开始");
    }

    private void UserAgreementWindow_Activated(object? sender, EventArgs e)
    {
        if (_isCountdownPaused && _remainingSeconds > 0)
        {
            _countdownTimer.Start();
            _isCountdownPaused = false;
            UpdateCountdownDisplay();
            ForceLog.Write("[USER-AGREEMENT] 窗口激活，倒计时恢复");
        }
    }

    private void UserAgreementWindow_Deactivated(object? sender, EventArgs e)
    {
        if (_countdownTimer.IsEnabled && _remainingSeconds > 0)
        {
            _countdownTimer.Stop();
            _isCountdownPaused = true;
            CountdownText.Text = "[WARN] 请保持窗口焦点，倒计时已暂停";
            ForceLog.Write("[USER-AGREEMENT] 窗口失焦，倒计时暂停");
        }
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateCountdownDisplay();

        if (_remainingSeconds <= 0)
        {
            _countdownTimer.Stop();
            CheckCanAgree();
            ForceLog.Write("[USER-AGREEMENT] 倒计时结束，检查同意条件");
        }
    }

    private void AgreementScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.ScrollViewer sv) return;

        var delta = sv.VerticalOffset - _lastVerticalOffset;
        if (delta > MaxScrollDelta)
        {
            var clamped = _lastVerticalOffset + MaxScrollDelta;
            sv.ScrollToVerticalOffset(clamped);
            _lastVerticalOffset = clamped;
        }
        else if (delta < -MaxScrollDelta)
        {
            var clamped = _lastVerticalOffset - MaxScrollDelta;
            sv.ScrollToVerticalOffset(clamped);
            _lastVerticalOffset = clamped;
        }
        else
        {
            _lastVerticalOffset = sv.VerticalOffset;
        }

        if (sv.VerticalOffset >= sv.ScrollableHeight - 1)
        {
            _hasScrolledToBottom = true;
            CheckCanAgree();
            ForceLog.Write("[USER-AGREEMENT] 用户已滚动至协议底部");
        }
    }

    private void UpdateCountdownDisplay()
    {
        var minutes = _remainingSeconds / 60;
        var seconds = _remainingSeconds % 60;
        CountdownText.Text = $"请仔细阅读协议（{minutes:D2}:{seconds:D2}）";

        if (!_hasScrolledToBottom)
        {
            ScrollHintText.Text = "请滚动至协议底部";
            ScrollHintText.Visibility = Visibility.Visible;
        }
        else
        {
            ScrollHintText.Visibility = Visibility.Collapsed;
        }
    }

    private void CheckCanAgree()
    {
        if (_remainingSeconds <= 0 && _hasScrolledToBottom)
        {
            AgreeButton.IsEnabled = true;
            CountdownText.Text = "已阅读完毕，请选择是否同意";
            ForceLog.Write("[USER-AGREEMENT] 同意条件已满足，同意按钮已启用");
        }
    }

    private static System.Windows.Controls.ScrollViewer? FindScrollViewer(System.Windows.DependencyObject parent)
    {
        if (parent is System.Windows.Controls.ScrollViewer sv)
            return sv;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void CollectShakeElements(System.Windows.DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is UIElement element
                and not System.Windows.Controls.Panel
                and not System.Windows.Controls.Decorator
                and not System.Windows.Controls.ContentPresenter
                and not System.Windows.Controls.ScrollContentPresenter)
            {
                if (element.RenderTransform is not TranslateTransform)
                {
                    element.RenderTransform = new TranslateTransform();
                }
                var transform = (TranslateTransform)element.RenderTransform;
                _shakeElements.Add((element, transform));
            }

            CollectShakeElements(child);
        }
    }

    private void ResetShakeElements()
    {
        foreach (var (_, transform) in _shakeElements)
        {
            transform.X = 0;
            transform.Y = 0;
        }
        _shakeElements.Clear();
    }

    private void AgreeButton_Click(object sender, RoutedEventArgs e)
    {
        _userAgreementService.SetAgreed(AgreementVersion);
        ForceLog.Write($"[USER-AGREEMENT] 用户已同意协议 v{AgreementVersion}");
        DialogResult = true;
        Close();
    }

    private void DisagreeButton_Click(object sender, RoutedEventArgs e)
    {
        _originalLeft = Left;
        _originalTop = Top;
        _shakeRemainingMs = 5000;

        DisagreeButton.IsEnabled = false;
        AgreeButton.IsEnabled = false;

        CollectShakeElements(ContentRoot);

        _shakeTimer.Start();

        ForceLog.Write("[USER-AGREEMENT] 用户点击不同意，启动恶作剧动画");

        for (int i = 0; i < 40; i++)
        {
            var (troll, transforms) = CreateTrollWindow();
            _trollWindows.Add((troll, troll.Left, troll.Top, transforms));
            troll.Show();
        }

        ForceLog.Write("[USER-AGREEMENT] 已创建 40 个恶作剧弹窗");
    }

    private (Window Window, List<TranslateTransform> ContentTransforms) CreateTrollWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        const int w = 320;
        const int h = 160;

        var window = new Window
        {
            Title = "[WARN] 错误",
            Width = w,
            Height = h,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = _random.Next(0, (int)(screenWidth - w)),
            Top = _random.Next(0, (int)(screenHeight - h)),
            Topmost = true,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = false,
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x00, 0x00)),
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.Red,
        };

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(20),
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };

        var transforms = new List<TranslateTransform>();

        var iconTransform = new TranslateTransform();
        var icon = new System.Windows.Controls.TextBlock
        {
            Text = "[ERR]",
            FontSize = 36,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
            RenderTransform = iconTransform
        };
        transforms.Add(iconTransform);

        var textPanel = new System.Windows.Controls.StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleTransform = new TranslateTransform();
        var title = new System.Windows.Controls.TextBlock
        {
            Text = "没同意用户协议用你妈呢傻逼玩意???",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.OrangeRed,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            RenderTransform = titleTransform
        };
        transforms.Add(titleTransform);

        var subTransform = new TranslateTransform();
        var sub = new System.Windows.Controls.TextBlock
        {
            Text = "爱用就用不用给老子爬",
            FontSize = 11,
            Foreground = Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap,
            RenderTransform = subTransform
        };
        transforms.Add(subTransform);

        textPanel.Children.Add(title);
        textPanel.Children.Add(sub);
        panel.Children.Add(icon);
        panel.Children.Add(textPanel);
        window.Content = panel;

        return (window, transforms);
    }

    private void ShakeTimer_Tick(object? sender, EventArgs e)
    {
        _shakeRemainingMs -= 50;

        Left = _originalLeft + _random.Next(-50, 51);
        Top = _originalTop + _random.Next(-50, 51);

        ContentTranslate.X = _random.Next(-10, 11);
        ContentTranslate.Y = _random.Next(-10, 11);

        foreach (var (_, transform) in _shakeElements)
        {
            transform.X = _random.Next(-4, 5);
            transform.Y = _random.Next(-4, 5);
        }

        foreach (var (w, origLeft, origTop, contentTransforms) in _trollWindows)
        {
            try
            {
                w.Left = origLeft + _random.Next(-30, 31);
                w.Top = origTop + _random.Next(-30, 31);

                foreach (var t in contentTransforms)
                {
                    t.X = _random.Next(-3, 4);
                    t.Y = _random.Next(-3, 4);
                }
            }
            catch { }
        }

        if (_shakeRemainingMs <= 0)
        {
            _shakeTimer.Stop();

            Left = _originalLeft;
            Top = _originalTop;

            ContentTranslate.X = 0;
            ContentTranslate.Y = 0;

            ResetShakeElements();

            foreach (var (w, _, _, _) in _trollWindows)
            {
                try { w.Close(); } catch { }
            }
            _trollWindows.Clear();

            ForceLog.Write("[USER-AGREEMENT] 恶作剧动画结束，关闭所有弹窗并退出应用");

            DialogResult = false;
            Application.Current.Shutdown();
        }
    }
}