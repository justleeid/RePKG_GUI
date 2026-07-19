using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RePKG.WpfGui.ViewModels;

/// <summary>
/// 解包进度项
/// </summary>
public partial class ExtractProgressItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _status = "等待中";

    [ObservableProperty]
    private string _statusIcon = "○";

    [ObservableProperty]
    private string _statusColor = "#9E9E9E";
}

/// <summary>
/// 解包进度对话框 ViewModel
/// </summary>
public partial class ExtractProgressViewModel : ObservableObject
{
    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// 请求取消解包事件
    /// </summary>
    public event Action? CancelRequested;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private string _currentItemTitle = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private string _elapsedTime = "00:00";

    [ObservableProperty]
    private bool _isExtracting = true;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private ObservableCollection<ExtractProgressItemViewModel> _items = [];

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(int total, int completed, int failed, string currentItem)
    {
        TotalCount = total;
        CompletedCount = completed;
        FailedCount = failed;
        CurrentItemTitle = currentItem;
        ProgressPercent = total > 0 ? (double)completed / total * 100 : 0;
        ProgressText = $"{ProgressPercent:F0}% ({completed}/{total})";
    }

    /// <summary>
    /// 更新耗时
    /// </summary>
    public void UpdateElapsed(System.TimeSpan elapsed)
    {
        ElapsedTime = elapsed.ToString(@"mm\:ss");
    }

    /// <summary>
    /// 标记完成
    /// </summary>
    public void MarkCompleted(int success, int failed, System.TimeSpan elapsed)
    {
        IsExtracting = false;
        IsCompleted = true;
        UpdateElapsed(elapsed);
        ResultSummary = $"完成！成功: {success}, 失败: {failed}, 耗时: {ElapsedTime}";
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelRequested?.Invoke();
        IsExtracting = false;
        ResultSummary = "正在取消...";
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}
