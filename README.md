<div align="center">

# Math Symbol Pad

**数学符号，一点即输入。**  
**Math symbols, one click away.**

Windows · 便携离线 / Portable & offline · .NET Framework 4.8

### [下载便携版 · Download portable](https://github.com/shiyu429/MathSymbolPad/releases/latest/download/MathSymbolPad-Portable.zip)

[中文](#中文) · [English](#english) · [版本下载 / Releases](https://github.com/shiyu429/MathSymbolPad/releases)

</div>

| 浅色 · Light | 深色 · Dark |
| :---: | :---: |
| ![浅色界面 / Light interface](https://raw.githubusercontent.com/shiyu429/MathSymbolPad/main/light.png) | ![深色界面 / Dark interface](https://raw.githubusercontent.com/shiyu429/MathSymbolPad/main/dark.png) |

## 中文

一款轻量的 Windows 数学符号悬浮输入工具。先点击输入框，再点击面板上的符号，即可在光标位置输入，适用于聊天框、浏览器和普通文本输入框。

### 功能

- **符号分类**：常用、集合、代数、几何、模板五个分页，包含常用数学符号，悬停可查看中文说明。
- **快捷模板**：一键插入成对括号、根号、分数、幂、下标和区间，光标自动定位到待填写位置。
- **悬浮面板**：始终置顶，点击符号时保留输入框焦点；支持拖动、收成浮球和托盘操作。
- **外观设置**：跟随系统、浅色、深色及背景透明度调整，自动记住位置、分页和外观。
- **便携离线**：无需安装或联网，图标内置；输入不占用剪贴板，也不会自动发送消息。

### 使用

1. [下载便携包](https://github.com/shiyu429/MathSymbolPad/releases/latest/download/MathSymbolPad-Portable.zip)并解压，运行 `MathSymbolPad.exe`；保留同目录下的 `MathSymbolPad.exe.config`。
2. 点击目标输入框放好光标，再点击所需符号或模板。
3. 拖动标题移动面板，点“−”收成浮球，点“◐”设置外观，点“×”退出。

可运行 `Create-DesktopShortcut.ps1` 创建桌面快捷方式。

### 运行要求与说明

- 64 位 Windows，需有 .NET Framework 4.8。
- 模板输入普通数学文本，不提供公式排版或自动求解。
- 普通权限运行时，不保证能向管理员权限窗口输入。
- 支持系统缩放和多显示器定位；展开约需 344×330 个逻辑像素，极低分辨率配合高缩放时可能显示不全。

## English

A lightweight floating math symbol pad for Windows. Focus a text field, then click a symbol to insert it at the caret. Designed for chat boxes, browser text fields and standard text inputs.

### Features

- **Organized symbols**: five fixed tabs for common symbols, sets, algebra, geometry and templates, with Chinese tooltips.
- **Quick templates**: insert paired brackets, roots, fractions, powers, subscripts and intervals, with the caret positioned for typing.
- **Floating panel**: stays on top without taking focus when inserting symbols; supports dragging, a compact floating button and a tray menu.
- **Appearance controls**: system, light and dark themes with adjustable background opacity; remembers position, tab and appearance.
- **Portable and offline**: no installer or network connection required; embedded icons, no clipboard usage for input and no automatic message sending.

### Getting started

1. [Download the portable ZIP](https://github.com/shiyu429/MathSymbolPad/releases/latest/download/MathSymbolPad-Portable.zip), extract it and run `MathSymbolPad.exe`. Keep `MathSymbolPad.exe.config` beside the executable.
2. Focus your target text field, then click a symbol or template.
3. Drag the header to move the panel. Click “−” to collapse, “◐” for appearance settings or “×” to exit.

Optionally run `Create-DesktopShortcut.ps1` to create a desktop shortcut. Interface labels and tooltips are currently in Chinese.

### Requirements and notes

- 64-bit Windows with .NET Framework 4.8.
- Templates insert plain mathematical text; formula typesetting and equation solving are not included.
- Input into administrator-level windows is not guaranteed when the pad runs with standard permissions.
- Supports display scaling and multi-monitor positioning. The expanded panel needs approximately 344×330 logical pixels; very small work areas at high scaling may clip the panel.
