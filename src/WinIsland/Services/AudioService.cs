using NAudio.CoreAudioApi;

namespace WinIsland.Services;

public sealed class AudioService : IDisposable
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;

    private MMDevice GetDevice()
    {
        _enumerator ??= new MMDeviceEnumerator();
        var currentDefault = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        if (_device is null || !string.Equals(_device.ID, currentDefault.ID, StringComparison.OrdinalIgnoreCase))
        {
            _device?.Dispose();
            _device = currentDefault;
        }
        else
        {
            currentDefault.Dispose();
        }

        return _device;
    }

    public int? GetVolumePercent()
    {
        try { return PercentMath.ClampPercent(GetDevice().AudioEndpointVolume.MasterVolumeLevelScalar * 100); }
        catch { return null; }
    }

    public bool? IsMuted()
    {
        try { return GetDevice().AudioEndpointVolume.Mute; }
        catch { return null; }
    }

    public void SetVolumePercent(double percent)
    {
        GetDevice().AudioEndpointVolume.MasterVolumeLevelScalar = PercentMath.ToScalar(percent);
    }

    public void ToggleMute()
    {
        var endpoint = GetDevice().AudioEndpointVolume;
        endpoint.Mute = !endpoint.Mute;
    }

    public void Dispose()
    {
        _device?.Dispose();
        _enumerator?.Dispose();
        _device = null;
        _enumerator = null;
    }
}
