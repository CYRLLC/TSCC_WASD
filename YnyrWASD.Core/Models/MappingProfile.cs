using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YnyrWASD.Core.Models;

public class MappingProfile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private string _name = "自動偵測 → DualShock 4";
    private string? _description = "自動使用 NS2 Pro 或 Xbox 手把，輸出虛擬 DualShock 4。PS 圖示取決於遊戲支援。";
    private int _pollingRateHz = 125;
    private double _deadZone = 0.08;
    private InputDeviceType _inputType = InputDeviceType.Auto;
    private bool _hidePhysicalControllers = true;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get => _name; set => Set(ref _name, value); }
    public string? Description { get => _description; set => Set(ref _description, value); }

    public InputDeviceType InputType { get => _inputType; set => Set(ref _inputType, value); }
    public OutputControllerType OutputType { get; set; } = OutputControllerType.DualShock4;

    /// <summary>映射期間以 HidHide 隱藏實體手把，讓遊戲只看到虛擬 DS4。</summary>
    public bool HidePhysicalControllers { get => _hidePhysicalControllers; set => Set(ref _hidePhysicalControllers, value); }

    /// <summary>前景程式名稱 (不含路徑) 符合時自動套用。</summary>
    public string? MatchProcessName { get; set; }

    /// <summary>輪詢頻率 (Hz)。</summary>
    public int PollingRateHz { get => _pollingRateHz; set => Set(ref _pollingRateHz, value); }

    /// <summary>類比搖桿死區 (0~1)。</summary>
    public double DeadZone { get => _deadZone; set => Set(ref _deadZone, value); }

    /// <summary>若為 true，沒有驅動或裝置時不要自動啟動。</summary>
    public bool RequireDrivers { get; set; } = true;

    /// <summary>保留給後續進階設定 (曲線、巨集等)。</summary>
    public Dictionary<string, string> Advanced { get; set; } = new();

    [JsonIgnore]
    public int PollingIntervalMs => Math.Max(2, (int)Math.Round(1000.0 / Math.Max(1, PollingRateHz)));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name) || Name.Length > 100)
            throw new ArgumentException("設定檔需要 ID 與名稱（名稱最多 100 字）。");
        if (InputType is not (InputDeviceType.Auto or InputDeviceType.XInput or InputDeviceType.Switch2ProUsb) || OutputType != OutputControllerType.DualShock4)
            throw new ArgumentException("目前支援自動偵測／XInput／NS2 Pro USB → DualShock4。");
        if (PollingRateHz < 30 || PollingRateHz > 500)
            throw new ArgumentException("輪詢頻率必須介於 30–500 Hz。");
        if (!double.IsFinite(DeadZone) || DeadZone < 0 || DeadZone > 0.95)
            throw new ArgumentException("死區必須介於 0–0.95。");
    }

    public MappingProfile Snapshot()
    {
        Validate();
        return new MappingProfile
        {
            Id = Id, Name = Name, Description = Description,
            InputType = InputType, OutputType = OutputType, HidePhysicalControllers = HidePhysicalControllers,
            PollingRateHz = PollingRateHz, DeadZone = DeadZone,
            MatchProcessName = MatchProcessName, RequireDrivers = RequireDrivers,
            Advanced = Advanced is null ? new() : new(Advanced)
        };
    }
}
