using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MicaComic2PDF.App.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace MicaComic2PDF.App;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(820, 640));

        // 创建 ViewModel
        _vm = new MainViewModel();
        _vm.SetDispatcher(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
    }

    // Window 没有 DataContext —— 在 XAML 根 Grid 的 Loaded 里设
    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            fe.DataContext = _vm;
    }

    // ============ 拖放 ============

    private void TaskList_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "添加漫画文件夹";
            e.Handled = true;
        }
    }

    private async void TaskList_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
        {
            if (item.IsOfType(Windows.Storage.StorageItemTypes.Folder))
                _vm.AddFolderCommand.Execute(item.Path);
            else
            {
                var parent = Path.GetDirectoryName(item.Path);
                if (!string.IsNullOrEmpty(parent))
                    _vm.AddFolderCommand.Execute(parent);
            }
        }
    }

    // ============ 按钮 ============

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
            _vm.AddFolderCommand.Execute(folder.Path);
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e) => _vm.RemoveSelectedCommand.Execute(null);
    private void BtnClear_Click(object sender, RoutedEventArgs e) => _vm.ClearAllCommand.Execute(null);

    // ============ 列表选择变化 ============

    private void TaskListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = new List<MainViewModel.TaskItem>();
        foreach (var item in TaskListView.SelectedItems)
            if (item is MainViewModel.TaskItem ti) selected.Add(ti);
        _vm.UpdateSelection(selected);
    }
}
