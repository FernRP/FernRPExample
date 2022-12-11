# FernNPR

[中文](https://github.com/DeJhon-Huang/FernNPR/blob/master/README_CN.md) | [English](https://github.com/DeJhon-Huang/FernNPR/blob/master/README.md)

[TOC]

FernNPR 是一个Unity的NPR渲染库（未来会开发UE版本），它不仅仅是卡通渲染，而是包含尽可能多的NPR渲染技术，这也意味着会同时包含PBR的材质。

FernNPR 会尽量做好代码封装，保持扩展性 ，同时兼顾PC/Mobile的性能和效果。

## Render Example
下面是一些示例场景

### NPR 角色

![](DocAssets/11-22.jpg)
模型来自: [模之屋](https://www.aplaybox.com/details/model/S5d7KiigvyIb)

背景图来自: [GameVision Studios](https://gamevision.artstation.com/projects/ZGZxYG)

### 材质案例

FernNPR 可以轻松的创建多种风格的材质，包括NPR和PBR。

![](DocAssets/MaterialBall.jpg)

![](DocAssets/MaterialBall_AdditonalLight.jpg)

### 各向异性头发
![](DocAssets/aniso-hair.gif)

### 天使轮高光
![](DocAssets/compression/angleringspecular.gif)

### 眼睛
![](DocAssets/compression/eyeexample.gif)

### 脸

![](DocAssets/compression/SDFFace.gif)

### Depth Shadow 

使用深度偏移的技巧得到清晰的投影。

![](DocAssets/DepthShadow.jpg)

刘海投影的例子。

![](DocAssets/compression/DepthShadow-min.gif)

### Depth Offset Rim

使用深度偏移得到在屏幕空间下宽度一致的边缘光。

![](DocAssets/DepthOffsetRim.jpg)

### 后续计划

- 皮肤
- 布料
- 工具
- 后处理
- 延迟管线

## Related links

- [LWGUI](https://github.com/JasonMa0012/LWGUI)

- [VRoid Studio](https://vroid.com/en)
