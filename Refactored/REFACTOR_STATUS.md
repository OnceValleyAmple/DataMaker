# 重构验证清单

## 阶段一：代码整理

- [x] 强类型配置模型
- [x] 表达式解析器
- [x] 生成器服务
- [x] 配置服务
- [x] 校验服务
- [x] 编译服务
- [x] 运行服务
- [x] Job Object 服务
- [x] ZIP 服务
- [x] 流水线服务
- [x] ViewModel
- [x] 重构版 WPF 窗口

## 阶段二：Windows 构建

在 Windows PowerShell 执行：

```powershell
cd D:\Desktop\DataMaker
dotnet clean
dotnet restore
dotnet build
```

如果有错误，只修复第一个错误，再重新构建。

## 阶段三：运行验证

```powershell
dotnet run
```

依次验证：

- [ ] 启动窗口
- [ ] 添加字段
- [ ] 编辑字段
- [ ] 删除字段
- [ ] 复制字段
- [ ] 拖拽字段
- [ ] 添加分组
- [ ] 导入模板
- [ ] 导出模板
- [ ] 恢复上次配置
- [ ] 选择 cpp
- [ ] 选择 g++.exe
- [ ] 编译标程
- [ ] 生成 in
- [ ] 生成 out
- [ ] 超时处理
- [ ] 内存限制
- [ ] 输出超限
- [ ] 中止生成
- [ ] 失败重试
- [ ] 失败停止
- [ ] ZIP 不包含 solution.exe

## 阶段四：发布验证

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

发布成功并完成上述测试后，才允许删除旧版本文件。
