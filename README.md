# SmartWallpaper

SmartWallpaper is a lightweight Windows WPF app that allows you to set different wallpapers on each connected monitor — with support for individual scaling, DPI-aware placement, and a modern interface.

![image](https://github.com/user-attachments/assets/128741ac-5d62-400e-a371-5ed15109ba53)

## ✨ Features

- 🎯 Per-monitor wallpaper selection
- 🧠 Auto layout with DPI-awareness
- 🖼️ Live preview with resolution info
- 🌙 Dark UI with smooth rounded controls
- 🔧 No background services or heavy resource usage

## 📦 Requirements

- Windows 10 or 11 (x64)
- .NET Framework 4.8
- Admin rights to change wallpapers (optional)

## 🚀 How to use

1. Launch the app.
2. Select a wallpaper per monitor.
3. Click `Apply wallpaper` — done!

Wallpapers are automatically combined into one stretched image, and Windows is instructed to use **"tile" mode** for accurate alignment.

[![btn](https://github.com/user-attachments/assets/a7488eb8-2620-4046-b583-98955af15bfc)](https://github.com/trunjeee/SmartWallpaper/releases/download/v1.0/SmartWallpaper.exe)



## 🛠️ Manual build

You can build the app using Visual Studio:

```bash
Open SmartWallpaperWPF.sln
Build → Run (F5)

