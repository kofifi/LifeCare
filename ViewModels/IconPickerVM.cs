namespace LifeCare.ViewModels;

public class IconPickerVM
{
    public string ContainerId { get; set; } = "iconPicker";

    public string ForName { get; set; } = "Icon";
    public string ForId { get; set; } = "Icon";

    public string? Selected { get; set; }

    public string Label { get; set; } = "Ikona";

    public bool StartCollapsed { get; set; } = true;
}