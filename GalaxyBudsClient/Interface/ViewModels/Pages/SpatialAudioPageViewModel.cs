using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentIcons.Common;
using GalaxyBudsClient.Generated.I18N;
using GalaxyBudsClient.Interface.Pages;
using GalaxyBudsClient.Model;
using GalaxyBudsClient.Platform.SpatialAudio;
using ReactiveUI.SourceGenerators;

namespace GalaxyBudsClient.Interface.ViewModels.Pages;

public partial class SpatialAudioPageViewModel : MainPageViewModelBase, IDisposable
{
    private readonly OscSpatialBroadcaster _oscBroadcaster = new();
    private readonly OpenTrackBroadcaster _openTrackBroadcaster = new();
    private readonly BinauralDemoAudioPlayer _demoPlayer = new();

    [Reactive] private bool _isTrackingEnabled;
    [Reactive] private double _yaw;
    [Reactive] private double _pitch;
    [Reactive] private double _roll;
    [Reactive] private bool _isOscBroadcasting;
    [Reactive] private bool _isOpenTrackBroadcasting;
    [Reactive] private bool _isDemoPlaying;
    [Reactive] private string _statusText = Strings.SpatialTrackingInactive;

    public SpatialAudioPageViewModel()
    {
        SpatialAudioService.Instance.OrientationUpdated += OnOrientationUpdated;
        SpatialAudioService.Instance.PropertyChanged += OnServicePropertyChanged;
        _demoPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        PropertyChanged += OnSelfPropertyChanged;

        IsTrackingEnabled = SpatialAudioService.Instance.IsActive;
        UpdateStatusText();
    }

    private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SpatialAudioService.IsActive))
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsTrackingEnabled = SpatialAudioService.Instance.IsActive;
                UpdateStatusText();
            });
        }
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsTrackingEnabled):
                if (IsTrackingEnabled && !SpatialAudioService.Instance.IsActive)
                {
                    SpatialAudioService.Instance.Start();
                }
                else if (!IsTrackingEnabled && SpatialAudioService.Instance.IsActive)
                {
                    SpatialAudioService.Instance.Stop();
                }
                UpdateStatusText();
                break;

            case nameof(IsOscBroadcasting):
                _oscBroadcaster.IsEnabled = IsOscBroadcasting;
                break;

            case nameof(IsOpenTrackBroadcasting):
                _openTrackBroadcaster.IsEnabled = IsOpenTrackBroadcasting;
                break;
        }
    }

    private void OnOrientationUpdated(object? sender, SpatialOrientationEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Yaw = Math.Round(e.Yaw, 1);
            Pitch = Math.Round(e.Pitch, 1);
            Roll = Math.Round(e.Roll, 1);
        }, DispatcherPriority.Render);
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsDemoPlaying = isPlaying;
        });
    }

    public void Recenter()
    {
        SpatialAudioService.Instance.Recenter();
    }

    public async void ToggleDemo()
    {
        if (IsDemoPlaying)
        {
            _demoPlayer.Stop();
        }
        else
        {
            if (!SpatialAudioService.Instance.IsActive)
            {
                IsTrackingEnabled = true;
            }
            await _demoPlayer.StartAsync();
        }
    }

    private void UpdateStatusText()
    {
        StatusText = IsTrackingEnabled ? Strings.SpatialTrackingActive : Strings.SpatialTrackingInactive;
    }

    public override void OnNavigatedFrom()
    {
        base.OnNavigatedFrom();
        if (IsDemoPlaying)
        {
            _demoPlayer.Stop();
        }
    }

    public override Control CreateView() => new SpatialAudioPage { DataContext = this };

    public override string TitleKey => Keys.PageSpatialAudio;
    public override Symbol IconKey => Symbol.SoundWaveCircle;
    public override bool ShowsInFooter => false;

    public void Dispose()
    {
        SpatialAudioService.Instance.OrientationUpdated -= OnOrientationUpdated;
        SpatialAudioService.Instance.PropertyChanged -= OnServicePropertyChanged;
        _demoPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        _demoPlayer.Dispose();
        _oscBroadcaster.Dispose();
        _openTrackBroadcaster.Dispose();
        GC.SuppressFinalize(this);
    }
}
