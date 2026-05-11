# File Note Manager

> 为 Windows 资源管理器中的任意文件和文件夹添加备注、标签，并在鼠标悬停时直接显示。

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 功能特性

| 功能 | 说明 |
|------|------|
| **右键添加备注** | 在任意文件或文件夹上右键 → 「编辑文件备注」，即可添加/编辑备注 |
| **鼠标悬停显示** | 在资源管理器中悬停文件或文件夹，Tooltip 直接显示备注内容 |
| **标签管理** | 为备注添加多个标签，方便分类检索 |
| **全文搜索** | 主界面支持按备注内容、路径、标签搜索 |
| **备注随文件迁移** | 移动或复制文件后，备注自动跟随（NTFS ADS + 数据库迁移双重保障） |
| **历史版本** | 每次修改自动保留历史，可回滚到任意版本 |
| **打开位置** | 主界面点击「打开位置」，在资源管理器中定位并高亮选中对应文件 |
| **无需管理员权限** | 所有注册表操作写入 HKCU，普通用户即可使用 |

---

## 截图

> *（可在此处添加截图）*

---

## 快速开始

### 方式一：直接运行（推荐）

1. 前往 [Releases](../../releases) 下载最新 `dist.zip`
2. 解压到任意位置（如 `C:\Tools\FileNoteManager\`）
3. 运行 `FileNoteManager.exe`
4. 首次运行弹出设置向导，点击「立即注册右键菜单」
5. 在资源管理器中右键任意文件 → 「编辑文件备注」

> **前提条件**：需要安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（选 Windows x64 桌面运行时）

### 方式二：自包含版（无需安装 .NET）

```powershell
.\publish.ps1 -SelfContained
```

生成的 `dist\` 文件夹包含完整运行时，约 120 MB，复制到任意 Windows 10/11 x64 机器即可运行。

---

## 从源码构建

**环境要求**
- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
git clone https://github.com/<your-username>/windows_mark.git
cd windows_mark

# 构建
dotnet build FileNoteManager.sln

# 运行测试
dotnet test FileNoteManager.sln

# 运行应用
dotnet run --project src/FileNoteManager.UI
```

### 发布打包

```powershell
# 框架依赖版（~15 MB，需目标机安装 .NET 8）
.\publish.ps1

# 自包含版（~120 MB，无需任何前置依赖）
.\publish.ps1 -SelfContained
```

输出在 `dist\` 文件夹。

---

## 项目结构

```
windows_mark/
├── src/
│   ├── FileNoteManager.Core/       # 核心逻辑：数据模型、Repository、Service
│   ├── FileNoteManager.Shell/      # Windows Shell 扩展：右键菜单 + 悬停 Tooltip
│   └── FileNoteManager.UI/         # WPF 主界面 + 快速编辑弹窗
├── tests/
│   ├── FileNoteManager.Core.Tests/ # 单元测试（xUnit + Moq）
│   └── FileNoteManager.PropertyTests/ # 属性测试（FsCheck）
├── docs/                           # 设计文档
├── publish.ps1                     # 一键打包脚本
└── FileNoteManager.sln
```

### 技术栈

- **UI**：WPF (.NET 8)，MVVM（CommunityToolkit.Mvvm）
- **数据库**：SQLite（Microsoft.Data.Sqlite + Dapper）
- **Shell 扩展**：COM IQueryInfo（悬停 Tooltip）+ 注册表右键菜单
- **文件监听**：FileSystemWatcher（自动同步重命名/删除）
- **备注迁移**：NTFS Alternate Data Streams (ADS) + 数据库软删除迁移

---

## 数据存储

所有数据保存在当前用户的 AppData 目录，不影响系统：

```
%APPDATA%\FileNoteManager\
├── fnm.db          # SQLite 数据库（备注、标签、历史）
└── logs/           # 日志文件
```

注册表项写入 `HKCU\Software\Classes`，随时可从主界面注销。

---

## 悬停 Tooltip 说明

Tooltip 功能通过 Windows COM Shell 扩展（`IQueryInfo`）实现，注册后：
- **首次注册**需完全重启资源管理器（任务管理器 → 结束 `explorer.exe` → 重新打开）才能生效
- 悬停 Tooltip 与文件右键菜单共用同一套注册流程，一次注册全部启用
- 备注随文件移动/复制自动迁移，Tooltip 始终显示最新内容

---

## 卸载

1. 打开 File Note Manager 主界面
2. 点击「注销右键菜单」按钮
3. 删除 `%APPDATA%\FileNoteManager\` 文件夹
4. 删除应用程序文件夹

---

## 许可证

[MIT License](LICENSE)
