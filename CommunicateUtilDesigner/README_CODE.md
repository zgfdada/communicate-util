# CommunicateUtil Designer 代码工程师说明

本文档面向维护和二次开发 `CommunicateUtilDesigner` 的代码工程师。

## 目标

`CommunicateUtilDesigner` 在现有 `CommunicateUtil` 基库上增加一层 Web 可视化配置能力，用于生成通讯数据层 C# 类库源码。

核心约束：

- 生成目标框架固定为 `netstandard2.0`，与 `CommunicateUtil` 基库保持一致。
- Web 持久化使用 `SqlSugarCore + SQLite`，不使用 EF Core。
- Web 服务只保存配置和生成源码，不执行用户填写的校验方法代码片段。
- 本地写入只允许输出到 `CommunicateUtilDesigner/artifacts/generated/{ProjectName}`。

## 目录结构

```text
CommunicateUtilDesigner/
  CommunicateUtilDesigner.sln
  README.md
  README_CODE.md
  README_USER.md
  src/
    CommunicateUtil.Generator/
      Models/
      CommunicationCodeGenerator.cs
      CommunicationSchemaValidator.cs
      ICommunicationCodeGenerator.cs
      ICommunicationSchemaValidator.cs
    CommunicateUtil.Web/
      Controllers/
      Data/
      Services/
      Views/
      wwwroot/
  tests/
    CommunicateUtil.Generator.Tests/
```

## 项目职责

### `CommunicateUtil.Generator`

生成器核心库，目标框架为 `netstandard2.0`。

主要职责：

- 定义生成配置模型：`ProtocolSchema`、`CommClassSchema`、`CommFieldSchema`、`EnumSchema`、`ValidationMethodSchema`。
- 校验配置合法性：命名空间、类名、字段名、枚举名、重复 Order、未知类型引用等。
- 生成 C# 源码、示例、配置 JSON、README 和 `.csproj`。
- 支持完整项目生成和局部源码生成。

主要接口：

```csharp
public interface ICommunicationCodeGenerator
{
    Task<GenerationResult> GenerateAsync(GenerateRequest request);
    Task<GenerationResult> GenerateClassAsync(ProtocolSchema schema, string className);
    Task<GenerationResult> GenerateEnumAsync(ProtocolSchema schema, string enumName);
}
```

### `CommunicateUtil.Web`

ASP.NET Core MVC/Razor Web 项目，负责配置录入、持久化和调用生成器。

主要职责：

- 命名空间、类、字段、枚举、枚举值的 CRUD。
- 字段 Order 和枚举值 Value 的插入式顺序维护。
- 完整源码预览、下载 ZIP、本地写入。
- 当前类源码预览、当前枚举源码预览。

持久化：

- 使用 `SqlSugarCore`。
- SQLite 文件路径：`CommunicateUtilDesigner/src/CommunicateUtil.Web/App_Data/designer.db`。
- `App_Data` 已加入 `.gitignore`，不要提交本地数据库。

### `CommunicateUtil.Generator.Tests`

xUnit 测试项目，覆盖生成器输出。

当前重点验证：

- 基础通讯类生成。
- 数组、List、枚举、嵌套通讯类、校验方法生成。
- 非法 schema 诊断。
- 当前类局部生成。
- 当前枚举局部生成。

## 关键数据模型

Web 持久化实体位于：

```text
src/CommunicateUtil.Web/Data/DesignerEntities.cs
```

主要实体：

- `ProtocolProjectEntity`：命名空间配置，包含项目名称、命名空间、程序集名、目标框架。
- `CommClassEntity`：通讯类配置，生成类继承 `BaseCommunicateArrtObject`。
- `CommFieldEntity`：字段配置，包含字段类型、具体类型、集合类型、Order、长度、字节序、备注等。
- `EnumDefinitionEntity`：枚举定义，包含枚举名、底层类型、注释。
- `EnumMemberEntity`：枚举值，包含成员名、值和注释。
- `ValidationMethodEntity`：校验方法配置，当前页面隐藏但生成器仍支持。

## 字段生成规则

字段生成到 `Models/{ClassName}.cs`。

每个通讯类：

```csharp
public class DeviceData : BaseCommunicateArrtObject
```

每个字段生成：

```csharp
[CommunicateArrtibute(OrderIndex = 1)]
public byte DeviceId { get; set; }
```

规则摘要：

- `TypeKind = String` 时，`TypeName` 固定为 `string`。
- `CollectionKind = Array` 生成 `T[]`。
- `CollectionKind = List` 生成 `List<T>`。
- Array/List 自动生成 `AutoLengthType = typeof(byte)`。
- `ArrayLength` 不为 `-1` 时输出固定长度。
- 非默认字节序输出 `EndianType = EndianType.xxx`。
- 配置校验方法时输出 `ValidCheckArrtibute`。

## 枚举生成规则

枚举生成到 `Enums/{EnumName}.cs`。

枚举定义的注释会输出到 XML summary：

```csharp
/// <summary>
/// 命令类型
/// </summary>
public enum CommandType : byte
{
    /// <summary>
    /// 读取命令
    /// </summary>
    Read = 1,
    Write = 2
}
```

枚举值按 `Value` 排序输出。枚举值注释会输出到成员上方的 XML summary。

## 顺序维护规则

### 字段 Order

字段页的 `Order` 可以手动输入。

新增或修改字段时：

- 如果插入到已有 Order，当前类内该 Order 以及后续字段会自动后移。
- 上移/下移按钮会重排当前类内的 Order。
- 保存后仍停留在字段配置页。

### 枚举值 Value

枚举值页的 `值` 可以手动输入。

新增或修改枚举值时：

- 新增行默认值为当前枚举最大值 + 1。
- 如果保存为已有值，同枚举内该值以及后续枚举值会自动后移。
- 该逻辑在后端 `DesignerRepository` 中处理，不依赖前端脚本。

## Web 路由

常用页面：

```text
/Projects
/Projects/Create
/Projects/Details/{id}
/Projects/Preview/{id}
/Projects/PreviewClass/{id}?classId={classId}
/Projects/PreviewEnum/{id}?enumId={enumId}
/Projects/Download/{id}
```

写入本地为 POST：

```text
/Projects/WriteLocal/{id}
```

## 本地开发

从仓库根目录执行：

```powershell
dotnet restore CommunicateUtilDesigner\CommunicateUtilDesigner.sln
dotnet build CommunicateUtilDesigner\CommunicateUtilDesigner.sln
dotnet test CommunicateUtilDesigner\tests\CommunicateUtil.Generator.Tests\CommunicateUtil.Generator.Tests.csproj
dotnet run --project CommunicateUtilDesigner\src\CommunicateUtil.Web\CommunicateUtil.Web.csproj --urls http://127.0.0.1:18057
```

打开：

```text
http://127.0.0.1:18057/
```

## 验证清单

提交前建议执行：

```powershell
dotnet build CommunicateUtilDesigner\CommunicateUtilDesigner.sln
dotnet test CommunicateUtilDesigner\tests\CommunicateUtil.Generator.Tests\CommunicateUtil.Generator.Tests.csproj
```

如果 Web 正在运行，Windows 上可能锁住 DLL，导致 build 报文件占用。先停止 `CommunicateUtil.Web` 进程后再构建。

## 不应提交的文件

以下内容是本地运行产物，不应进入 git：

```text
CommunicateUtilDesigner/src/CommunicateUtil.Web/App_Data/
CommunicateUtilDesigner/artifacts/
bin/
obj/
*.log
```

这些路径已由 `.gitignore` 覆盖。
