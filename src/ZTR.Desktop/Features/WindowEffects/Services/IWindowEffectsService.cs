using System.Windows;

namespace ZTR.Desktop.Features.WindowEffects.Services;

public interface IWindowEffectsService
{
    void ApplyMica(Window window);
    void ApplyDarkTitleBar(Window window, bool dark);
    void ApplyRoundedCorners(Window window);
    void RemoveMica(Window window);
}
