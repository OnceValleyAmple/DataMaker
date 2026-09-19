# DataMaker 重构版

该目录是 DataMaker 的分层重构实现。旧的 `Models.cs`、`Services.cs` 和窗口代码仍保留在上层，重构代码按职责拆分，便于逐步替换。

## 分层

- `Models`：强类型配置模型
- `Expressions`：表达式解析
- `Generators`：整数、浮点、字符串、数组、树、图生成
- `Services`：配置、验证、编译、运行、打包
- `ViewModels`：界面状态与命令
- `Views`：后续替换主窗口

## 当前迁移顺序

1. 使用 `Models` 和 `Expressions`
2. 替换旧 `DataGenerator`
3. 接入 `GenerationService`
4. 替换 WPF 窗口为 ViewModel 绑定
5. 删除旧服务文件
