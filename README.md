# DataMaker MVP

中文 WPF/.NET 8 工程骨架，当前包含：

- 字段表单：整数、浮点、字符串、数组、树、图模型
- 配置 JSON 保存/加载
- 序列 + 询问模板入口
- 基础整数、字符串、数组随机生成
- 标程输出捕获、超时终止
- `.in/.out` ZIP 打包到桌面
- 单文件自包含发布配置（win-x64）

## 构建

在 Windows 安装 .NET 8 SDK 后执行：

```powershell
dotnet publish -c Release
```

输出位于 `bin/Release/net8.0-windows/win-x64/publish/`。

> 当前沙盒没有安装 dotnet SDK，因此本轮无法在此处执行编译验证。树/图生成器、表达式解析、MinGW 自动探测和 Windows Job Object 限制是下一步应补齐的核心项。
