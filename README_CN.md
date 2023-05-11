![cover](https://github.com/DeJhon-Huang/FernNPR/blob/master/DocAssets/cover.jpg)
------------------------------------

# FernNPR

[中文](https://github.com/DeJhon-Huang/FernNPR/blob/master/README_CN.md) | [English](https://github.com/DeJhon-Huang/FernNPR/blob/master/README.md)

FernNPR 是一个Unity的NPR渲染库，尽可能多的渲染技术，角色渲染、环境渲染以及AI辅助!

## Render Example
下面是一些示例场景

![](DocAssets/11-22.jpg)
Model From: [模之屋](https://www.aplaybox.com/details/model/S5d7KiigvyIb), Background From: [GameVision Studios](https://gamevision.artstation.com/projects/ZGZxYG)

![](DocAssets/MaterialBall.jpg)

## 更多例子

如果你想要看更多的例子，可以移步到[Wiki](https://github.com/DeJhon-Huang/FernNPR/wiki)上的[More Example](https://github.com/DeJhon-Huang/FernNPR/wiki/More-Example) 页面

## Fern SD Graph

Fern SD Graph 是一个集成在Unity中的Graph工具，可以提取Unity的画面信息以及利用Stable Diffusion生成图片。

![](DocAssets/SD/SDInpaint.jpg)
![](DocAssets/SD/StableControlNet.jpg)

[More Example](https://github.com/DeJhon-Huang/FernNPR/wiki/Stable-Graph-Example)

### Fern SD Graph 目前的功能
1. Text2Img
2. Img2Img
3. Inpaint
4. Lora
5. ...

### 注意

想要使用本工具需要先在本地部署[stable diffusion webui](https://github.com/AUTOMATIC1111/stable-diffusion-webui), 并在webui-user.bat文件中的COMMANDLINE_ARGS添加--api命令。

### 未来的功能
1. ControlNet
2. 节点的持续优化，更加流畅，更加高效。
3. Lora权重。
4. 给ControlNet的场景预处理。
5. 使用Timeline渲染视频。
6. ...

___

## 工具
这里是一些有用的工具

### SmoothNormal And Texture Baker

![](DocAssets/texturebaketool.jpg)

这个工具由 [DumoeDss](https://github.com/DumoeDss) 开发.

Smooth Normals 用于解决发现外拓描边的断裂问题. 更详细的内容可以前往 [这里](https://github.com/DumoeDss/AquaSmoothNormals) 查阅.

Texture Baker 可以将贴图烘焙到网格上。

___

## Future Features

1. [ **Shader Tool** ] 用于只能生成Shader，自定义Shader功能，以及keyword的优化
2. [ **Volume Render** ] 体积渲染系列将包含体积光、体积云、体积雾等效果。
3. [ **Post Processing** ] 后处理系列将尽可能地扩展URP build-in的后处理，添加更多效果
4. [ **AI** ] AI系列将包括Stable Diffusion Graph以及ChatGPT

更详细的开发计划可以前往[ Roadmap ](https://github.com/orgs/FernRender/projects/1)查看
___

## Related links

- [BiliBili](https://space.bilibili.com/477693184)

- [知乎专栏](https://www.zhihu.com/column/c_1587028302690304000)

- [LWGUI](https://github.com/JasonMa0012/LWGUI)

- [VRoid Studio](https://vroid.com/en)
