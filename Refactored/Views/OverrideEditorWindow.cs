using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataMaker.Refactored.Models;

namespace DataMaker.Refactored.Views;

public sealed class OverrideEditorWindow : Window
{
    readonly Dictionary<string, Control> controls = new();
    readonly FieldConfig fieldConfig;
    public string ResultText = "";
    readonly StackPanel formPanel = new();

    public OverrideEditorWindow(FieldConfig field, string initial)
    {
        fieldConfig = field;
        Title = $"Subtask 参数覆盖 - {field.Name}";
        Width = 460;
        SizeToContent = SizeToContent.Height; // 高度随内容自适应，彻底消除空白
        MaxHeight = 620;                      // 防止内容过多超出屏幕
        MinWidth = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });           // 0: 顶部提示
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: 表单内容
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });           // 2: 底部操作栏

        // 1. 顶部提示条
        var headerBanner = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(244, 246, 249)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(228, 231, 237)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 10, 16, 10)
        };
        headerBanner.Child = new TextBlock
        {
            Text = "💡 提示：输入覆盖值；留空表示使用全局配置。",
            Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            FontSize = 12
        };
        Grid.SetRow(headerBanner, 0);
        root.Children.Add(headerBanner);

        // 2. 表单主体内容 (左右两列对齐)
        var scroll = new ScrollViewer
        {
            Content = formPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(20, 12, 20, 12)
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        // 3. 底部操作栏
        var footerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(228, 231, 237)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 左侧: 继承按钮
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 右侧: 取消 + 确定

        // 继承全局按钮 (弱化样式)
        var inheritBtn = CreateStyledButton("↺ 继承全局 (重置)", 125, false);
        inheritBtn.Click += (s, e) => { ResultText = ""; DialogResult = true; };
        Grid.SetColumn(inheritBtn, 0);
        footerGrid.Children.Add(inheritBtn);

        // 右侧确定与取消
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal };
        var cancelBtn = CreateStyledButton("取消", 75, false);
        cancelBtn.Click += (s, e) => DialogResult = false;
        
        var okBtn = CreateStyledButton("确定", 80, true); // 高亮主按钮
        okBtn.Margin = new Thickness(8, 0, 0, 0);
        okBtn.Click += (s, e) =>
        {
            ResultText = string.Join(";", controls.Select(x =>
            {
                var value = x.Value switch
                {
                    TextBox t => t.Text,
                    ComboBox c => c.SelectedItem?.ToString() ?? "",
                    CheckBox b => b.IsChecked == true ? "true" : "false",
                    _ => ""
                };
                return string.IsNullOrWhiteSpace(value) ? "" : $"{x.Key}={Normalize(x.Key, value)}";
            }).Where(x => x.Length > 0));
            DialogResult = true;
        };

        rightStack.Children.Add(cancelBtn);
        rightStack.Children.Add(okBtn);
        Grid.SetColumn(rightStack, 2);
        footerGrid.Children.Add(rightStack);

        footerBorder.Child = footerGrid;
        Grid.SetRow(footerBorder, 2);
        root.Children.Add(footerBorder);

        Content = root;

        // 解析现有值
        var values = initial.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Split('=', 2))
                            .Where(x => x.Length == 2)
                            .ToDictionary(x => x[0].Trim(), x => x[1].Trim());

        // 动态生成两列对齐的表单行
        foreach (var key in Keys(field))
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // 标签列
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 控件列

            var label = new TextBlock
            {
                Text = Label(key),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Color.FromRgb(70, 75, 85)),
                FontSize = 13,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(label, 0);
            rowGrid.Children.Add(label);

            Control control = CreateControl(field, key, values.GetValueOrDefault(key));
            control.Height = 30;
            control.VerticalContentAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            rowGrid.Children.Add(control);

            controls[key] = control;
            formPanel.Children.Add(rowGrid);
        }
    }

    // 辅助创建现代风格按钮
    private static Button CreateStyledButton(string text, double width, bool isPrimary)
    {
        var btn = new Button
        {
            Content = text,
            Width = width,
            Height = 32,
            FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        if (isPrimary)
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(0, 102, 204));     // 主色调蓝
            btn.Foreground = Brushes.White;
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 90, 180));
            btn.FontWeight = FontWeights.SemiBold;
        }
        else
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // 次要按钮白底
            btn.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(210, 215, 220));
        }

        return btn;
    }

    static Control CreateControl(FieldConfig field, string key, string? value)
    {
        if (field is TreeField && key == "shape")
            return Combo(new[] { "随机树", "链", "菊花", "二叉树" }, value switch { "Random" => "随机树", "Chain" => "链", "Star" => "菊花", "Binary" => "二叉树", _ => value });
        if (field is GraphField && key == "shape")
            return Combo(new[] { "随机图", "链图", "菊花图", "二叉树", "网格图", "稀疏图", "稠密图" }, value switch { "Random" => "随机图", "Chain" => "链图", "Flower" => "菊花图", "BinaryTree" => "二叉树", "Grid" => "网格图", "Sparse" => "稀疏图", "Dense" => "稠密图", _ => value });
        if (field is ArrayField && key == "charsetPreset")
            return Combo(new[] { "", "Digits", "Lowercase", "Uppercase", "Letters", "DigitsAndLetters" }, value);
        if (field is StringField && key == "pattern")
            return Combo(new[] { "随机", "回文", "周期", "全部相同" }, value switch { "Random" => "随机", "Palindrome" => "回文", "Periodic" => "周期", "Same" => "全部相同", _ => value });
        if (field is ArrayField && key == "separator")
            return Combo(new[] { "空格", "换行" }, value switch { "Space" => "空格", "NewLine" => "换行", _ => value });
        if (field is ArrayField && key == "pattern")
            return Combo(new[] { "随机", "非递减", "严格递增", "非递增", "严格递减", "全部相同" }, value switch { "Random" => "随机", "NonDecreasing" => "非递减", "StrictIncreasing" => "严格递增", "NonIncreasing" => "非递增", "StrictDecreasing" => "严格递减", "Same" => "全部相同", _ => value });
        if (field is ArrayField && key == "sort")
            return Combo(new[] { "无", "升序", "降序" }, value switch { "None" => "无", "Ascending" => "升序", "Descending" => "降序", _ => value });
        if (field is ArrayField && key == "unique")
            return new CheckBox { IsChecked = bool.TryParse(value, out var b) && b, VerticalAlignment = VerticalAlignment.Center };

        return new TextBox { Text = value, Padding = new Thickness(4, 2, 4, 2) };
    }

    static ComboBox Combo(string[] items, string? selected)
    {
        var withBlank = new[] { "" }.Concat(items).ToArray();
        var c = new ComboBox { ItemsSource = withBlank, SelectedItem = selected ?? "" };
        if (c.SelectedIndex < 0) c.SelectedIndex = 0;
        return c;
    }

    string Normalize(string key, string value) => key switch
    {
        "shape" when fieldConfig is TreeField => value switch { "随机树" => "Random", "链" => "Chain", "菊花" => "Star", "二叉树" => "Binary", _ => value },
        "shape" when fieldConfig is GraphField => value switch { "随机图" => "Random", "链图" => "Chain", "菊花图" => "Flower", "二叉树" => "BinaryTree", "网格图" => "Grid", "稀疏图" => "Sparse", "稠密图" => "Dense", _ => value },
        "pattern" when fieldConfig is StringField => value switch { "随机" => "Random", "回文" => "Palindrome", "周期" => "Periodic", "全部相同" => "Same", _ => value },
        "pattern" => value switch { "随机" => "Random", "非递减" => "NonDecreasing", "严格递增" => "StrictIncreasing", "非递增" => "NonIncreasing", "严格递减" => "StrictDecreasing", "全部相同" => "Same", _ => value },
        "separator" => value switch { "空格" => "Space", "换行" => "NewLine", _ => value },
        "sort" => value switch { "无" => "None", "升序" => "Ascending", "降序" => "Descending", _ => value },
        _ => value
    };

    IEnumerable<string> Keys(FieldConfig f) => f switch
    {
        IntField => new[] { "min", "max" },
        FloatField => new[] { "min", "max", "precision" },
        StringField => new[] { "length", "minLength", "maxLength", "charset", "pattern" },
        ArrayField a => a.ElementType == ArrayElementType.String ? new[] { "length", "stringMinLength", "stringMaxLength", "separator", "charsetPreset", "charset" } :
                        a.ElementType == ArrayElementType.Float ? new[] { "length", "separator", "min", "max", "precision" } :
                        new[] { "length", "separator", "pattern", "min", "max", "sort", "unique" },
        TreeField => new[] { "nodes", "shape", "root", "weightMin", "weightMax" },
        GraphField => new[] { "nodes", "edges", "shape", "density", "weightMin", "weightMax" },
        _ => Array.Empty<string>()
    };

    static string Label(string key) => key switch
    {
        "min" => "最小值",
        "max" => "最大值",
        "length" => "固定长度",
        "minLength" => "最小长度",
        "maxLength" => "最大长度",
        "charset" => "自定义字符集",
        "charsetPreset" => "字符集预设",
        "pattern" => "构造/特殊性质",
        "precision" => "精度",
        "stringMinLength" => "字符串最小长度",
        "stringMaxLength" => "字符串最大长度",
        "separator" => "元素分隔方式",
        "sort" => "排序",
        "unique" => "去重",
        "nodes" => "节点数",
        "edges" => "边数",
        "shape" => "结构形态",
        "root" => "根节点",
        "density" => "密度",
        "weightMin" => "权值下界",
        "weightMax" => "权值上界",
        _ => key
    };
}