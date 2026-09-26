# DataMaker

面向 OI / ACM 竞赛出题人的 **测试数据生成器**（中文界面，WPF + .NET 8）。

用声明式的「字段」描述一份输入文件的结构，DataMaker 负责：随机生成 `1.in … N.in` → 调用 g++ 编译你的标程 → 逐点运行得到 `1.out … N.out` → 打包成 ZIP。全程带时间限制、内存限制、输出大小限制、失败重试与中止控制。

```
配置字段(整数/浮点/字符串/数组/树/图)  ──►  表达式求值 + 随机生成  ──►  N.in
                                                                        │
                                     校验配置 ──► g++ 编译标程 ──► solution.exe
                                                                        │
                                          逐点运行(超时/内存/输出限制) ──► N.out
                                                                        │
                                                              ZIP(排除 solution.exe)
```

---

## 目录

- [功能特性](#功能特性)
- [运行环境](#运行环境)
- [构建与运行](#构建与运行)
- [界面与操作流程](#界面与操作流程)
- [字段类型详解](#字段类型详解)
- [表达式引擎](#表达式引擎)
- [Subtask 子任务与参数覆盖](#subtask-子任务与参数覆盖)
- [运行时限制](#运行时限制)
- [配置与模板文件](#配置与模板文件)
- [项目结构](#项目结构)
- [修复记录](#修复记录)
- [扩展：新增一种字段类型](#扩展新增一种字段类型)
- [本仓库的验证状态](#本仓库的验证状态)

---

## 功能特性

| 能力 | 说明 |
| --- | --- |
| 6 种字段类型 | 整数、浮点、字符串、数组（整/浮/字符串元素）、树、图 |
| 表达式参数 | 数量、范围、权值等参数均可写表达式（支持 `+ - * / %`、括号与小数，不支持函数与幂运算），并支持引用**前序字段**的值 |
| 结构约束 | 数组去重/排序/单调性；树的链、菊花、二叉形态；图的连通、DAG、二分图、自环、重边、稀疏/稠密/网格等 |
| Subtask 覆盖 | 按测试点区间分组，每个 Subtask 可覆盖任意字段的参数（强类型覆盖对象 + 覆盖矩阵） |
| 标程闭环 | 自动探测 / 手动指定 `g++.exe`，编译标程并逐点运行生成 `.out` |
| 资源限制 | 时间限制（每点）、内存限制（Windows Job Object）、输出大小上限 |
| 容错 | 失败重试次数、失败策略（继续 / 停止）、随时中止 |
| 配置持久化 | 关闭自动保存 `last-project.json`（带 `.bak`），启动自动恢复；模板可导入/导出/复制/重命名/删除 |
| 校验 | 生成前统一校验，逐条给出「路径: 原因」错误信息，不合法不生成 |
| 打包 | 输出 `.in/.out` 到 ZIP，自动排除 `solution.exe` |

## 运行环境

| 项 | 要求 |
| --- | --- |
| 操作系统 | **Windows x64**（`net8.0-windows` + WPF；内存限制依赖 kernel32 Job Object） |
| SDK | .NET 8 SDK |
| 编译器 | g++（MinGW-w64 / MSYS2 均可），用于编译标程 |
| IDE | Visual Studio 2022 17.8+ 或任意支持 .NET 8 的编辑器（`DataMaker.sln`） |

## 构建与运行

```powershell
# 开发运行
dotnet run

# 或
dotnet build
```

发布单文件自包含 exe（`DataMaker.csproj` 已内置 `PublishSingleFile` / `SelfContained` / `RuntimeIdentifier=win-x64`）：

```powershell
dotnet publish -c Release
```

产物：`bin/Release/net8.0-windows/win-x64/publish/DataMaker.exe`（自包含，目标机无需安装 .NET 运行时）。

> `DataMaker.csproj` 里设置了 `EnableWindowsTargeting=true`，因此在非 Windows 上也能 `dotnet build` 做编译检查，但**不能运行**。

## 界面与操作流程

启动窗口为 `Refactored/Views/RefactoredWindow.xaml`（由 `App.xaml` 的 `StartupUri` 指定），标题「DataMaker · 重构版」，1280×780，三栏布局：

```
┌ 顶部工具栏：题目名称 | 导入模板 导出模板 刷新 | 测试点数 时间限制 失败策略 重试次数 | 生成并打包 中止 ┐
├──────────────┬───────────────────┬──────────────────────────────────────────────┤
│ 左栏 300px   │ 中栏 380px        │ 右栏 TabControl                              │
│ 环境与存储配置│ 数据字段定义       │ ① 运行监控与测试点                            │
│  题目/源文件  │  + 整数 + 浮点     │    测试点执行详情：编号/状态/耗时/信息·错误    │
│  g++/存储目录 │  + 字符 + 数组     │    控制台日志                                │
│  编译参数/ZIP │  + 树   + 图       │ ② 子任务与覆盖矩阵 (Subtask)                  │
│ 模板文件列表  │  复制 删除         │    Subtask 数量、分组列表、测试点分组区间      │
│              │ 字段属性详细设置    │    子任务字段覆盖矩阵（双击单元格编辑）        │
└──────────────┴───────────────────┴──────────────────────────────────────────────┘
```

典型流程：

1. 左栏选择 **C++ 标程**（`.cpp`）与 **g++.exe**（可留空自动探测），设置编译参数（默认 `-O2 -std=c++17`）和 ZIP 输出路径。
2. 中栏用 `+ 整数 / + 浮点 / + 字符 / + 数组 / + 树 / + 图` 添加字段，字段按列表顺序 = 输出文件中的行顺序。可**拖拽排序**、复制、删除（被其他字段表达式引用的字段禁止删除）。
3. 选中字段后在「字段属性详细设置」里编辑参数（数量、范围等均可写表达式）。
4. 需要分 Subtask 时切到右栏第二个页签，设置 Subtask 数量、逐个指定测试点区间，再在覆盖矩阵里给具体字段填覆盖值。
5. 顶部工具栏设置测试点数、时间限制、失败策略、重试次数，点 **生成并打包**。
6. 右栏查看每个测试点的状态与耗时；**双击列表左半区打开 `.in`，右半区打开 `.out`**；随时可「中止」。

## 字段类型详解

每个字段生成**一行**（数组选「换行」分隔、树/图为多行时占多行），所有字段结果按顺序拼接，文件末尾带一个换行。

### 整数 `IntField`

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| Min / Max | `1` / `100` | 闭区间，支持表达式；`Max < Min` 报错 |

输出：`min + random.NextInt64(max - min + 1)`，生成后该字段名会被写入表达式变量表。

### 浮点 `FloatField`

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| Min / Max | `0` / `1` | 支持表达式与小数，例如 `n/2`、`0.5` |
| Precision | `2` | 小数位数，`Math.Clamp(precision, 0, 15)` |

输出：`F{Precision}` 格式化（InvariantCulture，即始终用 `.` 作小数点）。生成后同样会写入表达式变量表（取整数部分），因此后序字段可以引用它。

### 字符串 `StringField`

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| Length | `10` | 固定长度，支持表达式 |
| MinLength / MaxLength | 空 | 两者都填时改为区间随机长度 |
| CharsetPreset | None | 不选 / 数字 / 小写字母 / 大写字母 / 大小写字母 / 数字和大小写字母 |
| Charset | `""` | 自定义字符集，**非空时优先于预设** |
| Pattern | Random | 随机 / 回文 / 周期 / 全部相同 |

字符集为空会直接报错。Pattern 语义：`Same` = 首字符重复；`Palindrome` = 前半段镜像；`Periodic` = 先随机一个串，再随机一个周期 `p ∈ [1, n]`，用前 `p` 个字符循环铺满整串。

### 数组 `ArrayField`

| 参数 | 默认 | 适用 |
| --- | --- | --- |
| Length | `10` | 元素个数，支持表达式 |
| ElementType | Int | 整数 / 浮点数 / 字符串 |
| Min / Max | `1` / `100` | 整、浮元素范围（整数走表达式） |
| Precision | `2` | 浮点元素小数位 |
| Unique | false | 整数元素去重（范围不足会报错） |
| SortOrder | None | 无 / 升序 / 降序 |
| Pattern | Random | 随机 / 非递减 / 严格递增 / 非递增 / 严格递减 / 全部相同 |
| Separator | Space | 空格 / 换行 |
| StringMinLength / StringMaxLength | `1` / `10` | 字符串元素长度 |
| CharsetPreset / Charset | Lowercase / `abc…xyz` | 字符串元素字符集 |

> 三种元素类型都遵循 `Separator`（空格 / 换行）。

### 树 `TreeField`

输出 `n-1` 行 `parent child`（带权时 `parent child w`）。

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| Nodes | `10` | 节点数，支持表达式，必须 ≥ 1 |
| IndexStart | `1` | 编号起点 |
| Root | `1` | 根节点，必须落在 `[IndexStart, IndexStart+Nodes-1]`；Subtask 覆盖后链形态也会跟着换根 |
| Shape | Random | 随机树 / 链 / 菊花 / 二叉树 |
| Weighted | false | 是否输出边权 |
| WeightMin / WeightMax | `1` / `100` | 边权范围，支持表达式 |

### 图 `GraphField`

输出 `m` 行 `u v`（带权时 `u v w`）。

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| Nodes / Edges | `10` / `20` | 点数、边数，支持表达式 |
| IndexStart | `1` | 编号起点 |
| Shape | Random | 随机图 / 链图 / 菊花图 / 二叉树 / 网格图 / 稀疏图 / 稠密图 |
| Density | `0.2` | 仅对 Sparse / Dense 生效（Sparse 取 `min(density,0.2)`，Dense 取 `max(density,0.8)`），据此反算边数 |
| Directed | false | 有向图 |
| Connected | true | 随机补边前先生成一棵随机生成树 |
| Dag | false | 只保留 `u < v` 的边；与自环互斥 |
| Bipartite / LeftPartSize | false / `0` | 二分图；左部大小默认 `n/2` |
| AllowSelfLoops / AllowMultiEdges | false / false | 自环 / 重边 |
| Weighted / WeightMin / WeightMax | false / `1` / `100` | 边权 |

补边最多尝试 `m*100 + 1000` 次，仍凑不够 `m` 条边时抛出「无法按当前约束生成足够的边」。

## 表达式引擎

`Refactored/Expressions/ExpressionEvaluator.cs`，递归下降，整数与小数混合运算：

- 支持 `+ - * / %`、一元负号、括号、空白、小数字面量（`0.5`、`.5`）
- 标识符（字母/数字/下划线，不以数字开头）作为变量
- 内部以 `double` 计算；`Evaluate` 返回 `double`，`EvaluateInt` 在结果超出 `long` 范围时报错
- 除零、取模零、缺右括号、多余内容、未定义变量、非法字符、结果溢出都会抛出带中文说明的 `ExpressionException`

**变量来源**：整数与浮点字段生成后，都会以「字段名 → 生成值」写入本次生成的变量表（`GenerationContext.Variables`，浮点取整数部分），因此后出现的字段可以引用前面的字段，例如先定义 `n`，再让数组长度写 `n`。

配置校验会拒绝**前向引用**：若某字段的参数文本中出现了排在它之后的字段名，会报「引用了后置字段 X」。

## Subtask 子任务与参数覆盖

- `GroupConfig`：`Name` / `From` / `To`（测试点闭区间）/ `Description` / `Score` / `TypedOverrides`
- 生成第 `i` 个测试点时，取第一个满足 `From <= i <= To` 的分组；命中后，该分组里对应字段的覆盖值会**逐项覆盖**全局参数，未覆盖的项继承全局。
- 覆盖是**强类型**的：`IntegerFieldOverride` / `FloatFieldOverride` / `StringFieldOverride` / `ArrayFieldOverride` / `TreeFieldOverride` / `GraphFieldOverride`，字段全部可空（`null` = 继承全局），基类带 `IsEnabled`。
- 覆盖矩阵每格文本形如 `min=1;max=100` 或 `nodes=1000;weightMax=10000`，两种编辑方式：
  - **双击单元格** → 弹出 `OverrideEditorWindow` 表单（可「↺ 继承全局」清空）
  - **直接在单元格内输入** `key=value;key=value`（`CellEditEnding` 解析）
- 校验会检查：区间必须落在 `[1, TestCount]`、区间之间不得重叠、覆盖后的范围/长度/密度/边数上限是否仍然合法。

## 运行时限制

| 限制 | 实现 | 说明 |
| --- | --- | --- |
| 时间限制 | `TimeLimitMs`（默认 1000） | 每个测试点单独计时，超时 `Kill(true)` 并标记「超时」 |
| 内存限制 | `MemoryLimitMb`（默认 256） | Windows Job Object `PROCESS_MEMORY` 限额；创建失败则跳过限制 |
| 输出上限 | `MaxOutputMb`（默认 64） | 超限 `Kill(true)` 并标记「标程输出超过限制」 |
| 空输出 | — | 退出码为 0 但输出为空白 → 标记「标程输出为空」，视为失败 |
| 编译超时 | 30000ms（写死） | `PipelineService` 调用编译时传入 |

标程通过 **stdin** 接收输入（不创建临时 `.in` 文件重定向），stdout 全文即 `.out` 内容。

## 配置与模板文件

| 文件 | 位置 |
| --- | --- |
| 上次配置 | `%AppData%\DataMaker\last-project.json`（每次保存前把旧文件复制成 `last-project.json.bak`） |
| 模板库 | `<数据存储目录>\Templates\*.json`；数据存储目录未设置时用 `%AppData%\DataMaker\Templates` |

序列化：`System.Text.Json`，`WriteIndented = true`、`PropertyNameCaseInsensitive = true`、`JsonStringEnumConverter`（枚举写成字符串）。字段列表用多态判别属性 `"type"`（`int` / `float` / `string` / `array` / `tree` / `graph`）。保存时 `Version` 固定写为 `2`。

配置示例（对应「n 行 m 列网格，每行一个数组」）：

```json
{
  "Name": "示例题目",
  "Version": 1,
  "TestCount": 20,
  "TimeLimitMs": 1000,
  "MemoryLimitMb": 256,
  "MaxOutputMb": 64,
  "FailurePolicy": "Continue",
  "RetryCount": 0,
  "MingwPath": "",
  "SourcePath": "D:\\std\\solution.cpp",
  "CompileArgs": "-O2 -std=c++17",
  "StorageDirectory": "",
  "ZipPath": "data.zip",
  "MultiGroup": false,
  "GroupCountMin": 1,
  "GroupCountMax": 1,
  "Fields": [
    { "type": "int", "Name": "n", "Min": "1", "Max": "1000" },
    { "type": "int", "Name": "m", "Min": "n/2", "Max": "n*2" },
    {
      "type": "array",
      "Name": "a",
      "ElementType": "Int",
      "Length": "m",
      "Min": "1",
      "Max": "1000000000",
      "Unique": false,
      "SortOrder": "None",
      "Pattern": "Random",
      "Separator": "Space"
    }
  ],
  "Groups": [
    {
      "Id": "b1c2d3e4",
      "Name": "Subtask 1",
      "Description": "",
      "From": 1,
      "To": 10,
      "Score": 40,
      "TypedOverrides": {}
    }
  ]
}
```

**旧配置兼容**：`ConfigService.Load` 会把早期「`Groups[i].Overrides` = 字符串字典（小写 key）」的写法迁移成强类型 `TypedOverrides`；`Version < 2` 时还会补齐 TestCount/TimeLimitMs/MemoryLimitMb 下限、数组 Length 与字符串默认字符集。

**未暴露到界面的配置**（只能改 JSON）：`MemoryLimitMb`、`MaxOutputMb`、`MultiGroup` / `GroupCountMin` / `GroupCountMax`、`GroupConfig.Description` / `Score`。其中 `MultiGroup = true` 会在输出最前面加一行随机子任务数 `k`（`GroupCountMin…GroupCountMax`），随后拼接 `k` 段数据块。

## 项目结构

```
DataMaker/
├─ App.xaml / App.xaml.cs            入口（StartupUri 指向重构窗口）+ 全局异常弹窗
├─ DataMaker.csproj / DataMaker.sln  net8.0-windows、WPF、单文件自包含发布
│
├─ Refactored/                       ★ 当前生效的代码（分层重构版）
│  ├─ Models/
│  │  ├─ FieldModels.cs              FieldConfig 多态模型 + 各类型 Override + GroupConfig + ProjectConfig
│  │  └─ TestPointResult.cs          测试点状态机（等待/生成/运行/成功/跳过/失败/超时/崩溃/输出为空）
│  ├─ Expressions/ExpressionEvaluator.cs   表达式求值（整数 + 小数）与标识符提取
│  ├─ Generators/Generators.cs       GeneratorService：6 类字段的生成实现
│  ├─ Services/
│  │  ├─ ConfigService.cs            JSON 存取 + 旧格式迁移
│  │  ├─ TemplateService.cs          模板列举/删除（存取委托给 ConfigService）
│  │  ├─ ValidationService.cs        静态校验，返回 (路径, 原因) 列表
│  │  ├─ OverrideTextService.cs      Subtask 覆盖值的 key=value 文本 ⇄ 强类型对象（弹窗/单元格/旧版迁移共用）
│  │  ├─ CompilerService.cs          g++ 探测（配置路径 → C:\mingw64 → C:\msys64 → PATH）+ 异步编译
│  │  ├─ GenerationService.cs        批量生成 .in（含 MultiGroup 模式）
│  │  ├─ RunnerService.cs            运行标程：stdin 注入、超时、内存、输出上限
│  │  ├─ JobObjectService.cs         kernel32 Job Object 内存限额封装
│  │  ├─ ZipService.cs               顶层文件打包，排除 solution.exe
│  │  └─ PipelineService.cs          校验 → 编译 → 生成 → 逐点运行 → 打包 的总编排
│  ├─ ViewModels/MainViewModel.cs    Config/Messages/TestPoints + GenerateCommand/CancelCommand
│  ├─ Views/
│  │  ├─ RefactoredWindow.xaml(.cs)  主窗口（1280×780 三栏）
│  │  └─ OverrideEditorWindow.cs     Subtask 覆盖值编辑弹窗（纯代码构建 UI）
│  ├─ GlobalUsings.cs                全局 using
│  ├─ README.md                      分层说明与迁移顺序
│  └─ REFACTOR_STATUS.md             重构验证清单（Windows 上的手工验收项）
│
├─ Models.cs                         ⛔ 旧版模型（已从 csproj 排除）
├─ Services.cs                       ⛔ 旧版服务（已从 csproj 排除）
└─ MainWindow.xaml(.cs)              ⛔ 旧版主窗口（已从 csproj 排除）
```

`DataMaker.csproj` 中通过 `<Compile Remove>` / `<Page Remove>` 显式排除了根目录的 `Models.cs`、`Services.cs`、`MainWindow.xaml(.cs)`，它们仅作为重构前的参考保留，**不参与编译**。删除它们之前请先完成 `Refactored/REFACTOR_STATUS.md` 里的验收清单。

## 修复记录

下面 12 条是先前代码审查中发现的问题，现已全部处理。每条给出改动位置与修复方式。

| # | 问题 | 修复 | 位置 |
| --- | --- | --- | --- |
| 1 | `FieldOverrideBase` 没有多态标记，`TypedOverrides` 保存后只剩 `IsEnabled`，Subtask 覆盖值静默丢失 | 加 `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", UnknownDerivedTypeHandling = FallBackToBaseType)]` + 6 个 `[JsonDerivedType]`；同时在 `ConfigService.Load` 前置 `SanitizeLegacyTypedOverrides`，把**旧版没有 `$type` 的覆盖值**转成 `key=value` 文本再还原，避免旧配置因为缺少判别属性而整份打不开 | `Refactored/Models/FieldModels.cs`、`Refactored/Services/ConfigService.cs` |
| 2 | 浮点字段 Min/Max 走 `double.Parse`，写表达式直接 `FormatException` | 求值器改为 `double` 语义并支持小数字面量，新增 `EvaluateInt`；浮点字段、浮点数组元素范围改用 `EvalD` | `Refactored/Expressions/ExpressionEvaluator.cs`、`Refactored/Generators/Generators.cs` |
| 3 | 浮点元素数组恒用空格连接，忽略 `Separator` | 把分隔符提取为 `separator` 变量，整数/浮点/字符串三个分支统一使用 | `Refactored/Generators/Generators.cs` |
| 4 | 字符串/数组/树/图的参数为空时到生成阶段才报错 | 新增 `FieldExpressionIssues`，逐个检查必填表达式是否为空（`不能为空`）、可选表达式是否「只填了空格」；树/图补充 `IndexStart` 非负校验；浮点字段补充范围校验 | `Refactored/Services/ValidationService.cs` |
| 5 | `Periodic` 模式与随机结果无关（按字符集顺序循环） | 改为「先随机整串，再随机周期 `p ∈ [1, n]`，用前 `p` 个字符循环铺满」 | `Refactored/Generators/Generators.cs` |
| 6 | 树：`Chain` 用字段上的 `f.Root` 而非解析后的 `root`，Subtask 覆盖根节点不生效；`Shape = Random` 且 `IndexStart == Root` 时 `Random.Next(first, v)` 抛异常；二叉形态判断也用了 `f.Shape` | `Chain => i == 0 ? root : nodes[i-1]`；随机父节点改为 `v > first ? r.Next(first, v) : root`；二叉分支统一改用解析后的 `shape`；`available` 为空时回退到 `root` | `Refactored/Generators/Generators.cs` |
| 7 | 输出上限在 `ReadToEndAsync` 读完全文之后才检查，超大输出先被完整读进内存 | 新增 `ReadBoundedAsync`：64KB 一块边读边累计，`total > limitBytes` 立刻停止读取并终止进程 | `Refactored/Services/RunnerService.cs` |
| 8 | `FindGpp` 用 `File.Exists` 判断候选路径，填目录不生效；`PATH` 按 `;` 裸切会产生空条目；源文件缺失时只有 g++ 的英文报错 | 候选路径额外尝试 `Path.Combine(目录, "g++.exe")`；`Split(';', RemoveEmptyEntries)`；编译前校验源文件，给出「未选择 C++ 源文件」/「源文件不存在：…」 | `Refactored/Services/CompilerService.cs` |
| 9 | 前向引用检测把字段序列化成 JSON 再做 `\b名字\b` 正则匹配，容易误报 | 改为从表达式文本里按标识符提取变量名（`ExpressionEvaluator.Identifiers`），再与后序字段名比对；删除字段时的引用检查也复用同一份逻辑 `ValidationService.ReferencedVariables` | `Refactored/Services/ValidationService.cs`、`Refactored/Views/RefactoredWindow.xaml.cs` |
| 10 | 仅 Windows | **保留**。WPF + `net8.0-windows` + Job Object P/Invoke 是设计前提，非 Windows 可用 `EnableWindowsTargeting` 编译但无法运行 | — |
| 11 | `DataMaker.csproj` 有两行重复的 `<ImplicitUsings>enable</ImplicitUsings>` | 删除重复行 | `DataMaker.csproj` |
| 12 | 覆盖矩阵单元格直接输入时不支持浮点字段（无 `FloatField` 分支），数组分支也不解析 `charset` / `stringMinLength` / `stringMaxLength`；三份解析代码（弹窗、单元格、旧版迁移）各写一遍且不一致 | 抽出统一的 `OverrideTextService.Parse` / `ToText`，弹窗、单元格、`ConfigService` 旧版迁移三处全部改为调用它，key 集合一一对应（`min max precision length minLength maxLength charset charsetPreset pattern sort unique separator stringMinLength stringMaxLength nodes root shape density edges weightMin weightMax`） | `Refactored/Services/OverrideTextService.cs`（新增）等 |

修复过程中另外发现并处理了三个同源问题：

- **覆盖弹窗的数组「字符集」下拉会把中文标签当字符集写进去**：原来下拉项是「不选/数字/小写字母/…」，而 `Normalize` 没有 `charset` 的映射，选「小写字母」会写出 `charset=小写字母`，生成时就拿这三个汉字当字符集。现改为 `charsetPreset` 枚举下拉（直接输出 `Digits` / `Lowercase` …），另保留一个自由文本框填自定义字符集。
- **下拉框未选择时会静默写入第一项**：`Combo` 辅助方法在 `SelectedIndex < 0` 时强制选 0，于是原本「留空 = 继承全局」的单元格会被写成「随机 / 无 / 不选」。现在每个下拉都在最前面加了一个空项，留空就是不覆盖。
- **数组 `Pattern` 下拉第 4 项写成了「非递减」**（应为「非递增」），`NonIncreasing` 也被映射成「非递减」，导致该选项显示与回写都错位。

### 仍然存在的限制

- 只能在 Windows 上运行（见上表第 10 条）。
- 表达式引擎不支持函数、幂运算与比较运算。
- 图/树的补边、去重是「随机重试 + 上限」策略，约束过紧时会抛出「无法按当前约束生成足够的边」，需要放宽约束而不是换算法。
- `MultiGroup` 模式拼接多段数据块时，段与段之间会多出一个空行（`GeneratorService.GenerateTest` 的既有行为，未改动）。


## 扩展：新增一种字段类型

1. `Refactored/Models/FieldModels.cs`：继承 `FieldConfig` 定义新类型（如 `MatrixField`），并加 `[JsonDerivedType(typeof(MatrixField), "matrix")]`；如需支持 Subtask 覆盖，再加一个 `MatrixFieldOverride : FieldOverrideBase`。
2. `Refactored/Generators/Generators.cs`：在 `GenerateField` 的 `switch` 里加分支，实现生成函数（参数一律走 `Eval(...)` 以支持表达式）。
3. `Refactored/Services/ValidationService.cs`：补充该类型的静态校验规则。
4. `Refactored/Views/RefactoredWindow.xaml.cs`：在 `BuildEditor()` 里加 `case`，`ExpressionTexts()` 里登记参与引用检查的表达式字段，`ApplyOverrideText()` / `globalText()` / `OverrideMatrix_CellEditEnding` 里加覆盖解析；在 XAML 工具栏加一个 `+ 矩阵` 按钮。
5. `Refactored/Views/OverrideEditorWindow.cs`：加对应的覆盖表单控件。

## 本仓库的验证状态

本轮修复实际跑过的检查：

| 检查 | 结果 |
| --- | --- |
| C# 语法解析 | **通过**。用 `tree-sitter-c-sharp 0.23.5`（WASM，经 `web-tree-sitter 0.26` 加载）解析全部 22 个 `.cs` 文件（含新增的 `OverrideTextService.cs`）：0 个 `ERROR` / `MISSING` 节点。 |
| 修复点断言 | **23/23 通过**。用 tree-sitter 生成的语法树/源码逐条断言 12 个修复是否真的落在代码里，例如：`FieldOverrideBase` 上确有 6 个 `JsonDerivedType`、`Evaluate` 返回 `double` 而 `EvaluateInt` 返回 `long`、浮点分支的 `string.Join` 用的是 `separator`、`Chain => i == 0 ? root : nodes[i-1]`、`ReadBoundedAsync` 中存在 `total > limitBytes`、`csproj` 只剩 1 个 `ImplicitUsings`。 |
| XML 良构性 | **通过**。`App.xaml`、`MainWindow.xaml`、`Refactored/Views/RefactoredWindow.xaml`、`DataMaker.csproj` 均可被 `xml.etree.ElementTree` 解析。 |
| `System.Text.Json` 多态语义 | **已对照运行时源码确认**。从 `dotnet/runtime` 取到 `JsonPolymorphicAttribute.cs`（`TypeDiscriminatorPropertyName`、`UnknownDerivedTypeHandling` 均为可写属性）、`JsonUnknownDerivedTypeHandling.cs`（`FallBackToBaseType = 1`）以及 `Strings.resx` 中的 `DeserializationMustSpecifyTypeDiscriminator = "The JSON payload for polymorphic interface or abstract type '{0}' must specify a type discriminator."` —— 这正是旧配置缺少 `$type` 时会踩到的失败模式，所以补了 `SanitizeLegacyTypedOverrides`。 |
| `dotnet build` / `dotnet run` | **未能执行**。沙箱内 `dotnet: command not found`，`/usr/lib/dotnet`、`/opt/dotnet` 均不存在；`dot.net`、`aka.ms`、`api.nuget.org`、`objects.githubusercontent.com` 全部 `SSL_ERROR_SYSCALL`，只有 `github.com` / `registry.npmjs.org` / `pypi.org` 可达，因此无法获取 .NET 8 SDK 或任何 NuGet 包。 |
| 运行时行为 | **未验证**。所有生成结果、界面行为、序列化往返都没有真机跑过；`Refactored/REFACTOR_STATUS.md` 的阶段二~四验收清单仍是待办。 |

> 语法解析、断言和 XML 良构性都**不等于编译通过**，更不等于行为正确。合并前请在 Windows 上执行 `dotnet build` + `dotnet run`，并逐项走完 `Refactored/REFACTOR_STATUS.md`。特别建议先验证：打开一份**修复前保存的**配置（`%AppData%\DataMaker\last-project.json`），确认 Subtask 覆盖值能正常读回。

---

## 许可

仓库未包含 LICENSE 文件，默认保留所有权利。
