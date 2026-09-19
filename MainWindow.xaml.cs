using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataMaker;

public partial class MainWindow : Window
{
    private ProjectConfig C = new();
    private CancellationTokenSource? _generationCts;
    private string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataMaker", "config.json");

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        try { C = ConfigStore.Load(ConfigPath); } catch { }
        Refresh();
    }

    private void Refresh()
    {
        Fields.ItemsSource = null;
        Fields.ItemsSource = C.Fields;
        CountBox.Text = C.TestCount.ToString();
        ArgsBox.Text = C.CompileArgs;
        StandardBox.Text = C.StandardPath;
        MingwBox.Text = C.MingwPath;
        TimeBox.Text = C.TimeLimit.ToString();
        MemoryBox.Text = C.MemoryLimit.ToString();
        ZipBox.Text = string.IsNullOrWhiteSpace(C.OutputDirectory) ? C.ZipName : Path.Combine(C.OutputDirectory, C.ZipName);
        GroupsBox.IsChecked = C.MultiGroupMode;
        GroupCountMinBox.Text = C.GroupCountMin.ToString();
        GroupCountMaxBox.Text = C.GroupCountMax.ToString();
        GroupsList.ItemsSource = null;
        GroupsList.ItemsSource = C.Groups;
        if (C.Fields.Count > 0) Fields.SelectedIndex = 0;
    }

    private void UpdateFieldPanel(FieldType type)
    {
        var minMax = type is FieldType.Int or FieldType.Float or FieldType.Array;
        var floatOnly = type == FieldType.Float;
        var stringOnly = type == FieldType.String;
        var arrayOnly = type == FieldType.Array;
        var structure = type is FieldType.Tree or FieldType.Graph;
        var graphOnly = type == FieldType.Graph;
        MinBox.Visibility = minMax || stringOnly || structure ? Visibility.Visible : Visibility.Collapsed;
        MaxBox.Visibility = minMax || stringOnly || structure ? Visibility.Visible : Visibility.Collapsed;
        PrecisionBox.Visibility = floatOnly || (arrayOnly && ElementTypeBox.SelectedIndex == 1) ? Visibility.Visible : Visibility.Collapsed;
        CharsetBox.Visibility = stringOnly || (arrayOnly && ElementTypeBox.SelectedIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
        StringPatternBox.Visibility = stringOnly ? Visibility.Visible : Visibility.Collapsed;
        ElementTypeBox.Visibility = arrayOnly ? Visibility.Visible : Visibility.Collapsed;
        UniqueBox.Visibility = arrayOnly && ElementTypeBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        SortBox.Visibility = arrayOnly && ElementTypeBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        StructureBox.Visibility = structure ? Visibility.Visible : Visibility.Collapsed;
        WeightedBox.Visibility = structure ? Visibility.Visible : Visibility.Collapsed;
        ConnectedBox.Visibility = graphOnly ? Visibility.Visible : Visibility.Collapsed;
        DirectedBox.Visibility = graphOnly ? Visibility.Visible : Visibility.Collapsed;
        BipartiteBox.Visibility = graphOnly ? Visibility.Visible : Visibility.Collapsed;
        SelfLoopBox.Visibility = graphOnly ? Visibility.Visible : Visibility.Collapsed;
        MultiEdgeBox.Visibility = graphOnly ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Field_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (Fields.SelectedItem is not FieldConfig f) return;
        NameBox.Text = f.Name;
        TypeBox.SelectedIndex = (int)f.Type;
        MinBox.Text = f.Min;
        MaxBox.Text = f.Type == FieldType.Array ? f.Length : f.Max;
        PrecisionBox.Text = f.Precision.ToString();
        CharsetBox.Text = f.Charset;
        StringPatternBox.SelectedIndex = f.StringPattern == "palindrome" ? 1 : f.StringPattern == "periodic" ? 2 : f.StringPattern == "same" ? 3 : 0;
        UniqueBox.IsChecked = f.Unique;
        SortBox.SelectedIndex = f.SortOrder == "asc" ? 1 : f.SortOrder == "desc" ? 2 : 0;
        StructureBox.Text = f.Structure;
        WeightedBox.IsChecked = f.Weighted;
        ElementTypeBox.SelectedIndex = f.ElementType == "float" ? 1 : f.ElementType == "string" ? 2 : 0;
        ConnectedBox.IsChecked = f.Connected;
        DirectedBox.IsChecked = f.Directed;
        BipartiteBox.IsChecked = f.Bipartite;
        SelfLoopBox.IsChecked = f.AllowSelfLoop;
        MultiEdgeBox.IsChecked = f.AllowMultiEdge;
        UpdateFieldPanel(f.Type);
    }

    private void Config_Changed(object sender, RoutedEventArgs e)
    {
        if (Fields.SelectedItem is not FieldConfig f) return;
        f.Name = NameBox.Text;
        if (TypeBox.SelectedIndex >= 0) f.Type = (FieldType)TypeBox.SelectedIndex;
        f.Min = MinBox.Text;
        if (f.Type == FieldType.Array) f.Length = MaxBox.Text;
        else f.Max = MaxBox.Text;
        if (int.TryParse(PrecisionBox.Text, out var precision)) f.Precision = Math.Clamp(precision, 0, 15);
        f.Charset = CharsetBox.Text;
        f.StringPattern = StringPatternBox.SelectedIndex == 1 ? "palindrome" : StringPatternBox.SelectedIndex == 2 ? "periodic" : StringPatternBox.SelectedIndex == 3 ? "same" : "random";
        f.Unique = UniqueBox.IsChecked == true;
        f.SortOrder = SortBox.SelectedIndex == 1 ? "asc" : SortBox.SelectedIndex == 2 ? "desc" : "none";
        if (!string.IsNullOrWhiteSpace(StructureBox.Text)) f.Structure = StructureBox.Text;
        f.Weighted = WeightedBox.IsChecked == true;
        f.ElementType = ElementTypeBox.SelectedIndex == 1 ? "float" : ElementTypeBox.SelectedIndex == 2 ? "string" : "int";
        f.Connected = ConnectedBox.IsChecked == true;
        f.Directed = DirectedBox.IsChecked == true;
        f.Bipartite = BipartiteBox.IsChecked == true;
        f.AllowSelfLoop = SelfLoopBox.IsChecked == true;
        f.AllowMultiEdge = MultiEdgeBox.IsChecked == true;
        UpdateFieldPanel(f.Type);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        C.Fields.Add(new FieldConfig { Name = $"field{C.Fields.Count + 1}" });
        Refresh();
        Fields.SelectedIndex = C.Fields.Count - 1;
    }

    private Point _dragStart;

    private void Fields_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(Fields);
    }

    private void Fields_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var delta = e.GetPosition(Fields) - _dragStart;
        if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4) return;
        if (Fields.SelectedItem is FieldConfig f)
            DragDrop.DoDragDrop(Fields, f, DragDropEffects.Move);
    }

    private void Fields_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(FieldConfig))) return;
        var source = (FieldConfig)e.Data.GetData(typeof(FieldConfig))!;
        var point = e.GetPosition(Fields);
        var target = Fields.InputHitTest(point) as DependencyObject;
        while (target is not null && target is not ListBoxItem)
            target = System.Windows.Media.VisualTreeHelper.GetParent(target);
        var targetField = (target as ListBoxItem)?.DataContext as FieldConfig;
        var oldIndex = C.Fields.IndexOf(source);
        var newIndex = targetField is null ? C.Fields.Count - 1 : C.Fields.IndexOf(targetField);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;
        C.Fields.RemoveAt(oldIndex);
        if (oldIndex < newIndex) newIndex--;
        C.Fields.Insert(newIndex, source);
        Refresh();
        Fields.SelectedItem = source;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Fields.SelectedItem is FieldConfig f)
        {
            C.Fields.Remove(f);
            Refresh();
            Log.Text += $"已删除字段：{f.Name}\n";
        }
    }

    private void BrowseCpp_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "C++ 源文件|*.cpp;*.cc;*.cxx" };
        if (dialog.ShowDialog() == true) StandardBox.Text = dialog.FileName;
    }

    private void BrowseMingw_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "g++.exe|g++.exe|可执行文件|*.exe" };
        if (dialog.ShowDialog() == true) MingwBox.Text = dialog.FileName;
    }

    private void BrowseZip_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP 压缩包|*.zip",
            FileName = string.IsNullOrWhiteSpace(ZipBox.Text) ? "data.zip" : Path.GetFileName(ZipBox.Text),
            DefaultExt = ".zip",
            AddExtension = true
        };
        if (dialog.ShowDialog() == true) ZipBox.Text = dialog.FileName;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _generationCts?.Cancel();
        Log.Text += "正在请求中止生成...\n";
    }

    private void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsList.SelectedItem is GroupOverride group)
        {
            C.Groups.Remove(group);
            GroupsList.ItemsSource = null;
            GroupsList.ItemsSource = C.Groups;
            Log.Text += "已删除选中分组。\n";
        }
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(GroupFromBox.Text, out var from) || !int.TryParse(GroupToBox.Text, out var to) || from < 1 || to < from)
        {
            Log.Text += "分组范围无效。\n";
            return;
        }
        var group = new GroupOverride { From = from, To = to };
        foreach (var item in GroupOverrideBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = item.Split(':', 2);
            if (p.Length == 2 && !string.IsNullOrWhiteSpace(p[0])) group.Overrides[p[0].Trim()] = p[1].Trim();
        }
        C.Groups.Add(group);
        GroupsList.ItemsSource = null;
        GroupsList.ItemsSource = C.Groups;
        Log.Text += $"已添加分组 {from}-{to}。\n";
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        C = new ProjectConfig();
        Refresh();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Sync();
        ConfigStore.Save(C, ConfigPath);
        Log.Text += "配置已保存。\n";
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "DataMaker 配置|*.json" };
        if (dialog.ShowDialog() == true)
        {
            C = ConfigStore.Load(dialog.FileName);
            Refresh();
        }
    }

    private void Template_Click(object sender, RoutedEventArgs e)
    {
        C.Fields = new List<FieldConfig>
        {
            new() { Name = "n", Min = "1", Max = "1000" },
            new() { Name = "q", Min = "1", Max = "1000" },
            new() { Name = "a", Type = FieldType.Array, Length = "n", Min = "-100", Max = "100" }
        };
        Refresh();
    }

    private void Sync()
    {
        if (int.TryParse(CountBox.Text, out var count)) C.TestCount = Math.Max(1, count);
        C.CompileArgs = ArgsBox.Text;
        C.StandardPath = StandardBox.Text;
        C.MingwPath = MingwBox.Text;
        var zipPath = ZipBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(zipPath)) zipPath = "data.zip";
        if (!Path.IsPathRooted(zipPath)) zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), zipPath);
        C.OutputDirectory = Path.GetDirectoryName(zipPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        C.ZipName = Path.GetFileNameWithoutExtension(zipPath) + ".zip";
        C.MultiGroupMode = GroupsBox.IsChecked == true;
        if (int.TryParse(GroupCountMinBox.Text, out var groupMin)) C.GroupCountMin = Math.Max(1, groupMin);
        if (int.TryParse(GroupCountMaxBox.Text, out var groupMax)) C.GroupCountMax = Math.Max(C.GroupCountMin, groupMax);
        if (int.TryParse(TimeBox.Text, out var time)) C.TimeLimit = Math.Max(1, time);
        if (int.TryParse(MemoryBox.Text, out var memory)) C.MemoryLimit = Math.Max(0, memory);
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_generationCts is not null)
        {
            Log.Text += "已有生成任务正在运行。\n";
            return;
        }
        _generationCts = new CancellationTokenSource();
        var generationToken = _generationCts.Token;
        Sync();
        if (C.Fields.Count == 0)
        {
            Log.Text = "请先添加字段。";
            return;
        }

        var dir = Path.Combine(Path.GetTempPath(), "DataMaker_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Progress.Maximum = C.TestCount;
        Progress.Value = 0;
        StatusList.Items.Clear();
        Log.Text = "开始生成...\n";

        string? exe = null;
        if (!string.IsNullOrWhiteSpace(C.StandardPath))
        {
            if (!File.Exists(C.StandardPath))
            {
                Log.Text += "标程文件不存在。\n";
                return;
            }

            if (Path.GetExtension(C.StandardPath).Equals(".cpp", StringComparison.OrdinalIgnoreCase))
            {
                var gpp = string.IsNullOrWhiteSpace(C.MingwPath)
                    ? CompilerService.FindGpp()
                    : C.MingwPath;

                if (string.IsNullOrWhiteSpace(gpp))
                {
                    Log.Text += "未找到 g++.exe，请设置 MinGW 路径。\n";
                    return;
                }

                exe = Path.Combine(dir, "solution.exe");
                var compile = await CompilerService.CompileAsync(
                    gpp, C.StandardPath, C.CompileArgs, exe);

                if (!compile.Success)
                {
                    Log.Text += "标程编译失败：\n" + compile.Output + "\n";
                    return;
                }

                Log.Text += "标程编译成功。\n";
            }
            else
            {
                Log.Text += "标程必须是 .cpp 源文件，不能直接使用 exe。\n";
                return;
            }
        }
        else
        {
            Log.Text += "请选择 C++ 源文件。\n";
            return;
        }

        for (var i = 1; i <= C.TestCount; i++)
        {
            if (generationToken.IsCancellationRequested)
            {
                Log.Text += "生成已中止。\n";
                break;
            }
            StatusList.Items.Add($"测试点 {i}: 生成中");
            try
            {
                var input = DataGenerator.Generate(C, Environment.TickCount + i, i);
                File.WriteAllText(Path.Combine(dir, $"{i}.in"), input);

                if (exe is not null)
                {
                    var result = await StandardRunner.RunAsync(
                        exe, input, C.TimeLimit, generationToken, C.MemoryLimit);
                    File.WriteAllText(Path.Combine(dir, $"{i}.out"), result.Output);
                    var status = result.Success ? "完成" : $"失败：{result.Error}";
                    StatusList.Items.Add($"测试点 {i}: {status}");
                    Log.Text += $"测试点 {i}: {status}\n";
                }
                else
                {
                    File.WriteAllText(Path.Combine(dir, $"{i}.out"), "");
                    StatusList.Items.Add($"测试点 {i}: 已生成输入");
                    Log.Text += $"测试点 {i}: 已生成输入\n";
                }
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"测试点 {i}: 生成失败");
                Log.Text += $"测试点 {i} 生成失败：{ex.Message}\n";
            }

            Progress.Value = i;
        }

        var zip = Path.Combine(
            string.IsNullOrWhiteSpace(C.OutputDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : C.OutputDirectory,
            string.IsNullOrWhiteSpace(C.ZipName) ? "data.zip" : C.ZipName);

        // Pack 会自动排除 solution.exe，不删除仍可能被 Windows 占用的文件。
        try
        {
            StandardRunner.Pack(dir, zip);
            Log.Text += $"完成：{zip}\n";
        }
        catch (Exception ex)
        {
            Log.Text += $"打包失败：{ex.Message}\n";
        }

        ConfigStore.Save(C, ConfigPath);
        _generationCts.Dispose();
        _generationCts = null;
    }
}
