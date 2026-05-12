# 口袋小店

Unity 2D Android 挂机商店游戏项目。

项目路径：`IdleShopUnity/`

## 用 Unity Hub 打开

1. 打开 Unity Hub。
2. Add / Open project。
3. 选择 `IdleShopUnity`。
4. 使用 Unity `2022.3.62f3` 打开。

主场景：`Assets/Scenes/Main.unity`

## 命令行

创建/刷新场景与 Android 设置：

```bash
/home/ubuntu23/Unity/Hub/Editor/2022.3.62f3/Editor/Unity -batchmode -quit -projectPath /home/ubuntu23/Bureau/SHOP/IdleShopUnity -executeMethod ProjectBootstrapper.SetupProject -logFile /tmp/pocket-shop-setup.log
```

构建 Debug APK：

```bash
/home/ubuntu23/Unity/Hub/Editor/2022.3.62f3/Editor/Unity -batchmode -quit -projectPath /home/ubuntu23/Bureau/SHOP/IdleShopUnity -executeMethod ProjectBootstrapper.BuildAndroidDebug -logFile /tmp/pocket-shop-build.log
```

APK 输出：

```text
IdleShopUnity/Builds/Android/PocketShop-debug.apk
```

## 当前玩法

- 自动来客、自动购买库存商品。
- 手动快速收银。
- 宣传带来短期排队顾客。
- 商品补货、店铺升级、员工培养。
- 本地存档和最多 8 小时离线收益。
- 商品随店铺等级解锁。
- 支持多语种 UI，目前内置 `zh-CN`、`en-US`、`ja-JP`、`fr-FR`、`ko-KR`、`es-ES`、`ru-RU`、`ar-SA`、`de-DE`、`it-IT`、`pt-PT`。
- 设置页提供测试用重置存档按钮。
- UI 字体已接入 Noto Sans CJK 和 Noto Naskh Arabic。

## 多语种扩展

翻译文件路径：

```text
IdleShopUnity/Assets/Resources/Localization/
```

新增语种时复制 `zh-CN.json` 或 `en-US.json`，改成例如 `ja-JP.json`，保持 `key` 不变，只翻译 `value`。游戏运行时会自动加载该目录下的语言文件，并在设置页显示可切换语言。

之后新增任何功能文字，都必须同步加入所有语言 JSON。构建前会自动执行本地化校验：如果某个语言缺 key、有多余 key、重复 key 或空翻译，Android 构建会失败。

## 字体与资源

字体路径：

```text
IdleShopUnity/Assets/Fonts/
```

当前使用 `NotoSansCJK-Regular.ttc` 覆盖中文、日语、韩语等 CJK 文本，`NotoNaskhArabic-Regular.ttf` 用于阿拉伯语。阿拉伯语界面已做基础右对齐，但复杂 RTL 排版仍建议后续接入专门的 RTL 文本组件。

图片仍放在 `IdleShopUnity/Assets/Art/`，构建前会自动按用途设置 Sprite 和压缩参数：UI 256、商品/员工 512、店铺场景 1024。
