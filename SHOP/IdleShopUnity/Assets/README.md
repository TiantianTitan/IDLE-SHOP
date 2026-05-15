# 口袋小店 Unity 项目

这是一个 Unity 2022.3 LTS 创建的 2D Android 挂机商店游戏 MVP。

## 主要脚本

- `Assets/Scripts/IdleShopGame.cs`：核心玩法、运行时 UI、存档、离线收益、阶段目标。
- `Assets/Scripts/LocalizationManager.cs`：多语种文本加载、语言切换、缺失 key 回退。
- `Assets/Editor/ProjectBootstrapper.cs`：生成主场景、设置 Android 构建参数、命令行打包。
- `Assets/Fonts/`：UI 字体，当前包含 Noto Sans CJK 和 Noto Naskh Arabic。

## 美术资源

图片统一放在 `Assets/Art/` 下，按类别分目录：

- `Products/`：商品图标。
- `Shop/`：店铺背景、货架、收银台。
- `Staff/`：员工角色。
- `UI/`：金币、口碑、按钮、通用图标。

当前版本已经接入 `Assets/Art/` 下的图片资源；如果某张图片缺失，运行时会回退到色块和文字占位。后续替换图片时，建议保持透明背景 PNG/WebP。
构建前会自动把 `Assets/Art/` 下的 PNG 设为 Sprite，并按用途设置压缩和最大尺寸。

## 多语种

语言文件放在 `Assets/Resources/Localization/`。当前内置 `zh-CN`、`en-US`、`ja-JP`、`fr-FR`、`ko-KR`、`es-ES`、`ru-RU`、`ar-SA`、`de-DE`、`it-IT`、`pt-PT`。新增语言时复制一个 JSON 文件，修改 `languageCode` 和 `languageName`，保持 `key` 不变并翻译 `value`。游戏设置页会自动显示新增语言。

## 当前 MVP 玩法主线

首页围绕“卖货赚钱 -> 补货不断货 -> 升级赚更多”组织信息。P2 已加入阶段目标线，当前目标会显示在首页底部信息区，并在完成后自动发放现金奖励，用来强化前期的短期追求。

当前销售逻辑已从“单个商品随机售出”重构为“顾客订单”。每个订单可以包含多种商品，结账需要时间，库存不足时订单会等待补货。阶段目标顺序：完成 3 个顾客订单、补货 1 次、升级售价 1 次、培养收银员 1 次、小店升到 Lv.3。首页推荐卡会优先匹配当前阶段目标，目标顺序覆盖前期核心循环，避免新玩家只看到按钮但不知道下一步该做什么。

商品扩展为 8 类，员工扩展为 6 名具名角色。数值已改为更偏挂机游戏的指数成长：售价升级、员工培养和进货成本会逐步拉开长期目标，降低早期快速通关的问题。

订单系统重构后存档版本升级为 V3。旧测试存档会自动重置，避免商品索引变化导致库存和售出数据错位。
