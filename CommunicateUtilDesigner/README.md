# CommunicateUtil Designer

`CommunicateUtil Designer` 是基于 `CommunicateUtil` 基库的 Web 可视化通讯类库生成器。

它用于通过浏览器配置命名空间、通讯类、字段、枚举和枚举值，并生成目标框架固定为 `netstandard2.0` 的 C# 通讯数据层类库源码。

## 文档入口

- [README_CODE.md](README_CODE.md)：面向代码工程师，说明项目结构、开发启动、生成器接口和验证方式。
- [README_USER.md](README_USER.md)：面向使用者，说明 Web 页面如何配置并生成通讯类库源码。

## 快速启动

```powershell
dotnet run --project CommunicateUtilDesigner\src\CommunicateUtil.Web\CommunicateUtil.Web.csproj --urls http://127.0.0.1:18057
```

打开：

```text
http://127.0.0.1:18057/
```

