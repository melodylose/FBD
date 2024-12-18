# FBDApp - 功能方塊圖設計應用程式

一個基於 WPF 的應用程式，用於創建和編輯具有拖放功能的功能方塊圖。

## 功能特點

- 在畫布上拖放模組
- 在模組之間建立連接
- 以 JSON 格式保存和載入配置數據
- 移動或調整模組大小時動態更新連接線
- 現代化且直觀的使用者介面

## 技術細節

### 開發技術
- .NET Core
- WPF (Windows Presentation Foundation)
- MVVM 架構
- System.Text.Json 用於配置序列化

### 主要組件

- **DraggableModule**: 可拖放的功能方塊模組自定義控制項
- **ConnectionLine**: 模組間連接的視覺化表示
- **ConfigurationData**: 用於保存和載入應用程式狀態的數據結構
- **MainViewModel**: 核心應用程式邏輯和數據管理

## 開始使用

### 系統需求

- .NET Core SDK
- Visual Studio 2019 或更新版本

### 安裝步驟

1. 複製（Clone）程式碼庫
2. 在 Visual Studio 中開啟解決方案
3. 建置並運行應用程式

## 使用說明

1. 從工具箱將模組拖放到畫布上
2. 通過點擊連接點來連接模組
3. 使用保存配置選項保存您的工作
4. 載入先前保存的配置

## 授權條款

本專案採用 MIT 授權 - 詳情請參閱 [LICENSE](LICENSE) 文件。