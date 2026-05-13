# CommunicateUtil Designer 使用者说明

本文档面向使用 Web 页面配置和生成通讯类库源码的使用者。

## 这个工具做什么

`CommunicateUtil Designer` 用浏览器表格配置的方式生成 `CommunicateUtil` 通讯数据层 C# 类库。

你不需要手写每个 `[CommunicateArrtibute]`，只需要在页面里配置：

- 命名空间
- 通讯类
- 枚举
- 字段
- 字段顺序、长度、字节序和集合类型

工具会生成可编译的 C# 源码，目标框架固定为：

```text
netstandard2.0
```

## 启动

在仓库根目录运行：

```powershell
dotnet run --project CommunicateUtilDesigner\src\CommunicateUtil.Web\CommunicateUtil.Web.csproj --urls http://127.0.0.1:18057
```

打开浏览器：

```text
http://127.0.0.1:18057/
```

## 页面顺序

推荐按下面顺序配置：

1. 命名空间配置
2. 类配置
3. 枚举配置
4. 字段配置
5. 预览源码、下载 ZIP 或写入本地

页面顶部也按这个顺序提供页签。

## 1. 命名空间配置

命名空间配置代表一次生成输出。

需要填写：

- 命名空间名称：页面中显示的配置名称。
- 程序集名称：生成 `.csproj` 的程序集名。
- 命名空间：生成 C# 文件中的 namespace。
- 目标框架：固定为 `netstandard2.0`。

命名空间配置页会显示所有已创建的命名空间。切换命名空间后，类配置、枚举配置、字段配置会自动筛选当前命名空间下的数据。

删除命名空间会同时删除它下面的类、字段、枚举和枚举值配置。

## 2. 类配置

类配置用于创建通讯数据类。

每个类最终会生成：

```csharp
public class ClassName : BaseCommunicateArrtObject
```

需要填写：

- 类名：合法 C# 类名，例如 `DeviceData`。
- 描述：生成到类的 XML 注释。

类配置下方可以看到每个类已有字段数量。

## 3. 枚举配置

枚举配置用于定义字段可以引用的枚举类型。

### 枚举定义

先选择或新增枚举定义。

需要填写：

- 枚举名：合法 C# 枚举名，例如 `CommandType`。
- 底层类型：如 `byte`、`short`、`int` 等。
- 注释：生成到 enum 的 XML summary。

当前枚举可以单独点击“生成当前枚举源码”，只预览当前枚举的 `.cs` 文件，不需要生成整个项目。

### 枚举值

枚举值只编辑当前选中的枚举。

需要填写：

- 成员名：合法 C# 枚举成员名，例如 `Read`。
- 值：枚举成员值。

值的规则：

- 新增行默认自动填当前枚举最大值 + 1。
- 可以手动输入值。
- 如果输入的值和已有枚举值重复，已有的该值以及后续值会自动后移。

示例：

```text
已有：
zero = 0
one = 1

新增 Read，手动填 1 后保存：
zero = 0
Read = 1
one = 2
```

## 4. 字段配置

字段配置用于定义当前类的通讯字段。

配置前先选择：

- 命名空间
- 类库

选择类库后，字段表只显示当前类库下的字段。

当前类可以单独点击“生成当前类源码”，只预览当前类的 `Models/{ClassName}.cs`，不需要生成整个项目。

### 字段列说明

- 实体属性：生成 C# 属性名。
- 字段类型：
  - `Basic`：基础类型。
  - `String`：字符串。
  - `Enum`：枚举。
  - `CommClass`：嵌套通讯类。
- 类型：根据字段类型选择具体类型。
- 集合：
  - `None`：单个值。
  - `Array`：数组。
  - `List`：集合。
- Order：通讯字段顺序。
- 长度：字符串、数组、List 的长度配置。
- 字节序：字段字节序。
- 备注：生成到字段特性的 Remarks。

### 字段 Order 规则

Order 可以手动输入。

新增字段时：

- 默认 Order 自动填当前类字段数量 + 1。
- 如果手动填了已有 Order，已有该 Order 以及后续字段会自动后移。

示例：

```text
已有：
Header Order = 1
Length Order = 2

新增 FrameHead，手动填 Order = 1 后保存：
FrameHead Order = 1
Header Order = 2
Length Order = 3
```

表格中的上移、下移按钮也会自动重排当前类库内的 Order。

### 长度和集合

当集合选择 `Array` 或 `List` 时，会自动生成：

```csharp
AutoLengthType = typeof(byte)
```

长度列的含义：

- 固定数字：固定长度。
- `-1`：由自动长度前缀处理。
- 字段名：动态长度字段名。

### String 类型

字段类型选择 `String` 后，后面的具体类型会自动固定为：

```text
string
```

## 5. 生成源码

页面顶部提供完整项目生成：

- 预览源码
- 下载 ZIP
- 写入本地

局部生成：

- 字段配置页：“生成当前类源码”
- 枚举配置页：“生成当前枚举源码”

写入本地会输出到：

```text
CommunicateUtilDesigner/artifacts/generated/{ProjectName}
```

下载 ZIP 会包含：

- 生成的 `.csproj`
- `Models/*.cs`
- `Enums/*.cs`
- 示例文件
- 配置 JSON
- README

## 生成后的使用方式

生成的通讯类继承 `BaseCommunicateArrtObject`，可以直接调用：

```csharp
var bytes = model.GetBytes();
var parsed = DeviceData.GetSelf<DeviceData>(bytes);
```

生成项目会引用或提示依赖 `CommunicateUtil`。

## 常见问题

### 生成失败提示名称不合法

类名、字段名、枚举名、枚举值名必须是合法 C# 标识符。

不要使用：

- 空格
- `-`
- 以数字开头的名称
- C# 关键字

### 字段引用不到枚举

先到“枚举配置”创建枚举定义，再回到“字段配置”，字段类型选择 `Enum`，具体类型下拉框会出现枚举名。

### 字段引用不到嵌套类

先到“类配置”创建另一个通讯类，再回到“字段配置”，字段类型选择 `CommClass`，具体类型下拉框会出现类名。

### 枚举值顺序变了

这是预期行为。为了避免同一个枚举内出现重复值，当你把某个枚举值保存为已有值时，后面的值会自动后移。

