using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfMessageBox = System.Windows.MessageBox;
using OpenCvSharp;
using Windows.Media.Control;
using WinIsland.Services;
using Forms = System.Windows.Forms;

namespace WinIsland;

public partial class MainWindow : Window
{
    private readonly AudioService _audio = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _cameraTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Forms.NotifyIcon? _trayIcon;
    private GlobalSystemMediaTransportControlsSessionManager? _mediaManager;
    private GlobalSystemMediaTransportControlsSession? _spotifySession;
    private VideoCapture? _camera;
    private byte[]? _lastCameraFrame;
    private bool _isExpanded;
    private bool _updatingControls;
    private bool _refreshingStatus;
    private DateTime _lastSpotifyRefresh = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        _statusTimer.Tick += StatusTimer_Tick;
        _cameraTimer.Tick += CameraTimer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        InitializeTrayIcon();

        try
        {
            _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        }
        catch
        {
            SpotifyStateText.Text = "  MEDIA SESSION UNAVAILABLE";
        }

        _statusTimer.Start();
        await RefreshStatusAsync();
    }

    private void PositionWindow()
    {
        Left = Math.Max(0, (SystemParameters.PrimaryScreenWidth - Width) / 2);
        Top = 6;
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Text = "WinIsland — Windows control center",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem("Open WinIsland");
        openItem.Click += (_, _) => Dispatcher.Invoke(ShowAndExpand);
        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
        menu.Items.Add(openItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowAndExpand);
    }

    private void ShowAndExpand()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        ExpandIsland();
    }

    private async void StatusTimer_Tick(object? sender, EventArgs e) => await RefreshStatusAsync();

    private async Task RefreshStatusAsync()
    {
        if (_refreshingStatus)
            return;

        _refreshingStatus = true;
        try
        {
            RefreshBattery();
            RefreshAudio();

            if (DateTime.UtcNow - _lastSpotifyRefresh >= TimeSpan.FromSeconds(3))
                await RefreshSpotifyAsync();

            var brightness = await Task.Run(BrightnessService.ReadPercent);
            _updatingControls = true;
            if (brightness.HasValue)
            {
                BrightnessSlider.IsEnabled = true;
                BrightnessSlider.Value = brightness.Value;
                BrightnessValueText.Text = $"{brightness.Value}%";
                BrightnessHintText.Text = "Internal display";
            }
            else
            {
                BrightnessSlider.IsEnabled = false;
                BrightnessValueText.Text = "N/A";
                BrightnessHintText.Text = "Not exposed by display";
            }
            _updatingControls = false;
        }
        catch
        {
            _updatingControls = false;
        }
        finally
        {
            _refreshingStatus = false;
        }
    }

    private void RefreshBattery()
    {
        var battery = BatteryService.Read();
        var value = battery.Percent.HasValue ? $"{battery.Percent.Value}%" : "--%";
        CompactBatteryText.Text = value;
        BatteryPercentText.Text = value;
        BatterySourceText.Text = battery.IsCharging ? $"{battery.PowerSource} · Charging" : battery.PowerSource;
        BatteryDot.Fill = battery.Percent is <= 15 ?
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 112)) :
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 243, 107));
    }

    private void RefreshAudio()
    {
        var volume = _audio.GetVolumePercent();
        var muted = _audio.IsMuted();
        _updatingControls = true;
        if (volume.HasValue)
        {
            VolumeSlider.IsEnabled = true;
            VolumeSlider.Value = volume.Value;
            VolumeValueText.Text = $"{volume.Value}%";
            MuteButton.IsEnabled = true;
            MuteButton.Content = muted == true ? "UNMUTE" : "MUTE";
        }
        else
        {
            VolumeSlider.IsEnabled = false;
            VolumeValueText.Text = "N/A";
            MuteButton.IsEnabled = false;
        }
        _updatingControls = false;
    }

    private async Task RefreshSpotifyAsync()
    {
        _lastSpotifyRefresh = DateTime.UtcNow;
        try
        {
            _mediaManager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var sessions = _mediaManager.GetSessions();
            _spotifySession = sessions.FirstOrDefault(session =>
                session.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase));

            if (_spotifySession is null)
            {
                SpotifyStateText.Text = "  WAITING FOR SPOTIFY";
                TrackTitleText.Text = "Open Spotify to start listening";
                TrackArtistText.Text = "The app must be playing or have an active media session";
                CompactStatusText.Text = "Your Windows, at a glance";
                PlayPauseButton.IsEnabled = false;
                return;
            }

            var properties = await _spotifySession.TryGetMediaPropertiesAsync();
            var playback = _spotifySession.GetPlaybackInfo();
            var isPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            TrackTitleText.Text = string.IsNullOrWhiteSpace(properties.Title) ? "Spotify" : properties.Title;
            TrackArtistText.Text = string.IsNullOrWhiteSpace(properties.Artist) ? "Now connected" : properties.Artist;
            SpotifyStateText.Text = isPlaying ? "  PLAYING" : "  PAUSED";
            PlayPauseButton.Content = isPlaying ? "PAUSE" : "PLAY";
            PlayPauseButton.IsEnabled = true;
            CompactStatusText.Text = string.IsNullOrWhiteSpace(properties.Title)
                ? "Spotify connected"
                : properties.Title;
        }
        catch
        {
            _spotifySession = null;
            SpotifyStateText.Text = "  SPOTIFY UNAVAILABLE";
            TrackTitleText.Text = "Couldn't read the current session";
            TrackArtistText.Text = "Open Spotify and try again";
            PlayPauseButton.IsEnabled = false;
        }
    }

    private void CompactBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isExpanded)
            ExpandIsland();
    }

    private void ExpandIsland()
    {
        _isExpanded = true;
        Width = 850;
        Height = 680;
        ExpandedPanel.Visibility = Visibility.Visible;
        CompactBar.ToolTip = "WinIsland";
        PositionWindow();
    }

    private void CollapseIsland()
    {
        StopCamera();
        _isExpanded = false;
        ExpandedPanel.Visibility = Visibility.Collapsed;
        Width = 370;
        Height = 82;
        CompactBar.ToolTip = "Click to open WinIsland controls";
        PositionWindow();
    }

    private void CollapseButton_Click(object sender, RoutedEventArgs e) => CollapseIsland();

    private void ExitButton_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isExpanded)
        {
            CollapseIsland();
            e.Handled = true;
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingControls || !IsLoaded)
            return;
        try
        {
            _audio.SetVolumePercent(e.NewValue);
            VolumeValueText.Text = $"{PercentMath.ClampPercent(e.NewValue)}%";
        }
        catch
        {
            VolumeValueText.Text = "Unavailable";
        }
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _audio.ToggleMute();
            RefreshAudio();
        }
        catch
        {
            MuteButton.Content = "UNAVAILABLE";
        }
    }

    private async void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingControls || !IsLoaded || !BrightnessSlider.IsEnabled)
            return;

        var requested = PercentMath.ClampPercent(e.NewValue);
        BrightnessValueText.Text = $"{requested}%";
        var success = await Task.Run(() => BrightnessService.TrySetPercent(requested));
        if (!success)
            BrightnessHintText.Text = "Brightness control unavailable";
    }

    private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfSaveFileDialog
        {
            Title = "Save desktop screenshot",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            FileName = $"WinIsland-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        Exception? failure = null;
        Hide();
        await Task.Delay(180);
        try
        {
            ScreenCaptureService.SaveAllDisplays(dialog.FileName);
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            ShowAndExpand();
        }

        if (failure is null)
            WpfMessageBox.Show(this, $"Screenshot saved to:\n{dialog.FileName}",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            WpfMessageBox.Show(this, $"Screenshot failed: {failure.Message}",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_spotifySession is null)
            return;
        try
        {
            await _spotifySession.TryTogglePlayPauseAsync();
            await RefreshSpotifyAsync();
        }
        catch { SpotifyStateText.Text = "  CONTROL UNAVAILABLE"; }
    }

    private async void PreviousTrackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_spotifySession is null) return;
        try { await _spotifySession.TrySkipPreviousAsync(); await RefreshSpotifyAsync(); }
        catch { SpotifyStateText.Text = "  CONTROL UNAVAILABLE"; }
    }

    private async void NextTrackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_spotifySession is null) return;
        try { await _spotifySession.TrySkipNextAsync(); await RefreshSpotifyAsync(); }
        catch { SpotifyStateText.Text = "  CONTROL UNAVAILABLE"; }
    }

    private void OpenSpotifyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
        }
        catch
        {
            WpfMessageBox.Show(this, "The Spotify app could not be opened. Install or update Spotify, then try again.",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CameraToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_camera is not null)
            StopCamera();
        else
            StartCamera();
    }

    private void StartCamera()
    {
        try
        {
            _camera = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (!_camera.IsOpened())
            {
                _camera.Dispose();
                _camera = new VideoCapture(0, VideoCaptureAPIs.MSMF);
            }

            if (!_camera.IsOpened())
            {
                StopCamera();
                CameraStatusText.Text = "No camera could be opened";
                return;
            }

            _camera.Set(VideoCaptureProperties.FrameWidth, 640);
            _camera.Set(VideoCaptureProperties.FrameHeight, 360);
            CameraPanel.Visibility = Visibility.Visible;
            CameraToggleButton.Content = "ON";
            CameraStatusText.Text = "Camera is on · local preview";
            _cameraTimer.Start();
        }
        catch (Exception ex)
        {
            StopCamera();
            CameraStatusText.Text = "Camera unavailable";
            WpfMessageBox.Show(this, $"Camera could not be started: {ex.Message}",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CameraTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (_camera is null)
                return;

            using var frame = new Mat();
            if (!_camera.Read(frame) || frame.Empty())
                return;

            if (!Cv2.ImEncode(".jpg", frame, out var encoded))
                return;

            _lastCameraFrame = encoded;
            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(encoded);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            CameraPreview.Source = bitmap;
        }
        catch
        {
            // A missed camera frame is harmless; keep the preview alive.
        }
    }

    private void CameraCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCameraFrame is null)
        {
            WpfMessageBox.Show(this, "Wait for the camera preview to appear, then capture.",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new WpfSaveFileDialog
        {
            Title = "Save camera photo",
            Filter = "JPEG image (*.jpg)|*.jpg",
            DefaultExt = ".jpg",
            AddExtension = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            FileName = $"WinIsland-Camera-{DateTime.Now:yyyyMMdd-HHmmss}.jpg"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _lastCameraFrame);
            WpfMessageBox.Show(this, $"Photo saved to:\n{dialog.FileName}",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(this, $"Photo could not be saved: {ex.Message}",
                "WinIsland", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CameraStopButton_Click(object sender, RoutedEventArgs e) => StopCamera();

    private void StopCamera()
    {
        _cameraTimer.Stop();
        try { _camera?.Release(); } catch { }
        try { _camera?.Dispose(); } catch { }
        _camera = null;
        _lastCameraFrame = null;
        CameraPreview.Source = null;
        CameraPanel.Visibility = Visibility.Collapsed;
        CameraToggleButton.Content = "OPEN";
        CameraStatusText.Text = "Camera is off";
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _statusTimer.Stop();
        StopCamera();
        _audio.Dispose();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
