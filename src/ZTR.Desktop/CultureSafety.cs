using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace ZTR.Desktop;

/// <summary>
/// 解决 MaterialDesign 在中文 Windows 上的 en-us 文化崩溃问题。
/// 三层防护：线程文化钉死 + FE/FCE LanguageProperty OverrideMetadata + Dispatcher Hooks 兜底。
/// </summary>
public static class CultureSafety
{
    /// <summary>
    /// 通过 LCID=1033 获取安全文化，钉死线程默认文化，覆盖 WPF LanguageProperty 默认值。
    /// 返回安全文化对象供 DispatcherHooks 使用。
    /// </summary>
    public static CultureInfo ApplySafeCulture()
    {
        var safeCulture = GetSafeCulture();

        try
        {
            CultureInfo.DefaultThreadCurrentCulture = safeCulture;
            CultureInfo.DefaultThreadCurrentUICulture = safeCulture;
            Thread.CurrentThread.CurrentCulture = safeCulture;
            Thread.CurrentThread.CurrentUICulture = safeCulture;
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CULT] 钉死默认线程文化失败: {ex.Message}");
        }

        var emptyXml = System.Windows.Markup.XmlLanguage.Empty;
        try
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                forType: typeof(FrameworkElement),
                typeMetadata: new FrameworkPropertyMetadata(emptyXml));
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CULT] FE.LanguageProperty.OverrideMetadata(Empty) 失败: {ex.Message}");
        }

        try
        {
            FrameworkContentElement.LanguageProperty.OverrideMetadata(
                forType: typeof(FrameworkContentElement),
                typeMetadata: new FrameworkPropertyMetadata(emptyXml));
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CULT] FCE.LanguageProperty.OverrideMetadata(Empty) 失败: {ex.Message}");
        }

        ForceLog.Write("[CULT] 安全文化已应用：线程 + FE/FCE LanguageProperty 默认值已钉死");
        return safeCulture;
    }

    /// <summary>
    /// 挂 Dispatcher.Hooks.OperationStarted，每个操作前重设 CurrentCulture。
    /// 封死 MaterialDesign/WPF 模板延迟执行的 DataBindEngine 毒任务。
    /// </summary>
    public static void HookDispatcher(Dispatcher dispatcher, CultureInfo safeCulture)
    {
        dispatcher.Hooks.OperationStarted += (_, _) =>
        {
            try
            {
                CultureInfo.CurrentCulture = safeCulture;
                CultureInfo.CurrentUICulture = safeCulture;
            }
            catch
            {
            }
        };

        ForceLog.Write("[CULT] DispatcherHooks.OperationStarted 已挂，每个操作执行前都会重设线程文化");
    }

    /// <summary>
    /// 遍历 Application.Current.Windows 兜底覆盖，对已存在的元素显式设置 Language。
    /// </summary>
    public static void ApplyToExistingWindows()
    {
        try
        {
            var windows = Application.Current?.Windows;
            if (windows == null) return;

            int affected = 0;
            foreach (Window? w in windows)
            {
                if (w == null) continue;
                try
                {
                    ApplyLanguageRecursive(w, ref affected);
                }
                catch (Exception ex)
                {
                    ForceLog.Write($"[CULT] ApplyLanguage 处理窗口 '{w.Title ?? w.Name ?? "(null)"}' 出错: {ex.Message}");
                }
            }

            if (affected > 0)
                ForceLog.Write($"[CULT] ApplyToExistingWindows 已覆盖 {affected} 个元素 (XmlLanguage.Empty)");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CULT] ApplyToExistingWindows 整体失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 通过 LCID=1033 (GetCultureInfo(1033)) 获取安全文化。
    /// LCID 整数查表跳过字符串名解析，在任何 NLS/ICU 实现上都一定存在。
    /// </summary>
    private static CultureInfo GetSafeCulture()
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(1033);
            ForceLog.Write("[CULT] 通过 LCID=1033 拿到 en-US 文化 (LCID 查表法)");
            return culture;
        }
        catch (Exception lcidEx)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo("en-US");
                ForceLog.Write($"[CULT] LCID=1033 失败，退回字符串法拿到 en-US: {lcidEx.Message}");
                return culture;
            }
            catch (Exception nameEx)
            {
                ForceLog.Write($"[CULT] LCID+字符串法全部失败，终极 fallback 到 InvariantCulture. " +
                               $"LCID 失败: {lcidEx.Message} | 字符串失败: {nameEx.Message}");
                return CultureInfo.InvariantCulture;
            }
        }
    }

    private static void ApplyLanguageRecursive(DependencyObject root, ref int affected)
    {
        if (root == null) return;

        var emptyXml = System.Windows.Markup.XmlLanguage.Empty;

        if (root is FrameworkElement fe)
        {
            try
            {
                if (!fe.Language.IetfLanguageTag.Equals(emptyXml.IetfLanguageTag, StringComparison.Ordinal))
                {
                    fe.SetValue(FrameworkElement.LanguageProperty, emptyXml);
                    affected++;
                }
            }
            catch
            {
            }
        }
        else if (root is FrameworkContentElement fce)
        {
            try
            {
                if (!fce.Language.IetfLanguageTag.Equals(emptyXml.IetfLanguageTag, StringComparison.Ordinal))
                {
                    fce.SetValue(FrameworkContentElement.LanguageProperty, emptyXml);
                    affected++;
                }
            }
            catch
            {
            }
        }

        try
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is DependencyObject dobj)
                    ApplyLanguageRecursive(dobj, ref affected);
            }
        }
        catch
        {
        }
    }
}