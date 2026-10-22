using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicaComic2PDF.Core;
using MicaComic2PDF.Core.Abstractions;
using MicaComic2PDF.Core.Models;
using MicaComic2PDF.Formats;
using Microsoft.UI.Xaml;

namespace MicaComic2PDF.App.ViewModels;

/// <summary>
/// WinUI3 主窗口 ViewModel。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IOutputFormatRegistry _registry;
    private readonly ComicPipeline _pipeline;
    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcher;

    public MainViewModel()
    {
        _registry = FormatRegistryFactory.Create();
        _pipeline = new ComicPipeline(_registry);

        // 输出格式列表
        AvailableFormats = new ObservableCollection<FormatItem>(
            _registry.GetAll().Select(f => new FormatItem(f.FormatId, f.DisplayName)));
        SelectedFormat = AvailableFormats.FirstOrDefault(f => f.Id == "pdf");

        // 开页选项
        SplitOptions = new ObservableCollection<SplitOption>
        {
            new(SplitMode.Right, "日式翻页（右开）"),
            new(SplitMode.Left, "标准翻页（左开）"),
        };
        SelectedSplit = SplitOptions.First();

        // 监听 Logs 变化，自动更新 LogText
        Logs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(LogText));
        Folders.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasFolders));
            OnPropertyChanged(nameof(HasSelection));
        };
    }

    /// <summary>后台线程回 UI 用的 DispatcherQueue。</summary>
    public void SetDispatcher(Microsoft.UI.Dispatching.DispatcherQueue dq) => _dispatcher = dq;

    // ========== 设置绑定 ==========
    [ObservableProperty] private bool autoSplit = false;
    public ObservableCollection<SplitOption> SplitOptions { get; }
    [ObservableProperty] private SplitOption? selectedSplit;
    [ObservableProperty] private int maxEdge = 2048;
    [ObservableProperty] private int quality = 85;
    [ObservableProperty] private string outputDirectory = "";
    public ObservableCollection<FormatItem> AvailableFormats { get; }
    [ObservableProperty] private FormatItem? selectedFormat;

    // ========== 任务 & 日志 ==========
    public ObservableCollection<TaskItem> Folders { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();
    public string LogText => string.Join('\n', Logs);

    // ========== 进度 & 状态 ==========
    [ObservableProperty] private double progressValue;
    [ObservableProperty] private string statusMessage = "把漫画文件夹拖进来";
    [ObservableProperty] private bool isConverting;

    // ========== UI 状态 ==========
    public bool HasFolders => Folders.Count > 0;
    public bool HasSelection => _selectedFolders is { Count: > 0 };

    private IList<TaskItem> _selectedFolders = new List<TaskItem>();
    public void UpdateSelection(IList<TaskItem> selected)
    {
        _selectedFolders = selected;
        OnPropertyChanged(nameof(HasSelection));
    }

    // ========== 命令 ==========

    [RelayCommand]
    private void AddFolder(string folderPath)
    {
        folderPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(folderPath))
        {
            AddLog($"不是文件夹：{folderPath}");
            return;
        }
        if (Folders.Any(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase))) return;

        var count = Core.Processing.FolderScanner.CollectImages(folderPath).Count;
        Folders.Add(new TaskItem(folderPath, count));
        StatusMessage = $"已添加 {Folders.Count} 个文件夹";
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        foreach (var item in _selectedFolders.ToList()) Folders.Remove(item);
        StatusMessage = Folders.Count > 0
            ? $"已添加 {Folders.Count} 个文件夹"
            : "把漫画文件夹拖进来";
    }

    [RelayCommand]
    private void ClearAll()
    {
        Folders.Clear();
        StatusMessage = "把漫画文件夹拖进来";
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task StartConvertAsync()
    {
        if (Folders.Count == 0)
        {
            StatusMessage = "先添加漫画文件夹";
            return;
        }

        var splitMode = SelectedSplit?.Mode ?? SplitMode.Right;
        var options = new ConvertOptions
        {
            SplitMode = AutoSplit ? splitMode : SplitMode.None,
            MaxEdge = MaxEdge,
            Quality = Quality,
            OutputDirectory = OutputDirectory,
            FormatId = SelectedFormat?.Id ?? "pdf"
        };

        IsConverting = true;
        int ok = 0;
        int total = Folders.Count;

        for (int i = 0; i < Folders.Count; i++)
        {
            var folder = Folders[i];
            folder.Status = "转换中...";
            ProgressValue = 0;
            StatusMessage = $"[{i + 1}/{total}] {folder.FolderName}";

            try
            {
                var progress = new Progress<ConvertProgress>(p => ProgressValue = (double)p.Current / p.Total);
                var result = await _pipeline.ConvertAsync(
                    folder.FolderPath, options,
                    progress: progress,
                    log: AddLog);

                folder.Status = result.Success ? "完成" : "失败";
                if (result.Success) ok++;
            }
            catch (Exception ex)
            {
                folder.Status = "异常";
                AddLog($"[异常] {folder.FolderName}: {ex.Message}");
            }
        }

        ProgressValue = 1;
        StatusMessage = $"全部完成：成功 {ok}/{total}";
        AddLog($"\n=== 全部完成：成功 {ok}/{total} ===");
        IsConverting = false;
    }

    private void AddLog(string msg)
    {
        void DoAdd()
        {
            Logs.Add(msg);
            if (Logs.Count > 500) Logs.RemoveAt(0);
        }

        if (_dispatcher != null && !_dispatcher.HasThreadAccess)
            _dispatcher.TryEnqueue(DoAdd);
        else
            DoAdd();
    }

    // ========== UI 数据类型 ==========

    public sealed record SplitOption(SplitMode Mode, string Display);
    public sealed record FormatItem(string Id, string Display);

    public sealed partial class TaskItem : ObservableObject
    {
        public string FolderPath { get; }
        public string FolderName => Path.GetFileName(Path.GetFullPath(FolderPath).TrimEnd(Path.DirectorySeparatorChar));
        public int ImageCount { get; }
        public string ImageCountLabel => $"{ImageCount} 张";

        [ObservableProperty] private string status = "待转换";

        public TaskItem(string path, int count)
        {
            FolderPath = path;
            ImageCount = count;
        }
    }
}
