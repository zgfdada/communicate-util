# CommunicateUtil食用指南

## 总体功能介绍

使用c#编写的功能模块，致力于**通讯协议数据层编码解码**的便捷开发，适配各种**字节顺序**(大端/小端/大小端字间顺序交换)，实现**数据层**(由协议转换为C#的数据结构即类的定义)的**自动编码**(由数据对象转换为字节流)与**自动解码**(由字节流转换为数据对象)。

附带实现了所有值类型的数据转换(**T <---> Byte[])**,所有值类型数组及列表的数据转换(**List<T>/T[] <---> byte[]**),字符串的数据转换(**string <--> byte[]**),基于以上类型构成的数据结构对象的数据转换(**BaseCommArrtObj <--> byte[]**),复杂数据的数据转换(**List<BaseCommArrtObj>/BaseCommArrtObj[]/BaseCommArrtObj中嵌套BaseCommArrtObj <---> byte[]**)

## 食用思路

根据通讯协议定义一个类，通讯协议中的一项数据在类中对应为一个属性，该属性应当实现commArrt的特性定义，该类应当继承自BaseCommArrtObj类库，该基类实现了类库数据结构自动编码解码的方法。

## 模块编码解码基本逻辑

通过类库中各属性定义的commarrtde的OrderIndex的大小自动排序，再根据排序的顺序进行获取属性集合，对属性集合进行逐个的编码解码。属性未实现该特性的不会进行自动编码解码，可以自由发挥。

## 食用例程

### 数据解释器食用示例

```cs
 // ************************解释器测试***********************
 //值类型及其相关数组集合的测试
 int lengh;
 
 EnumType enumType = EnumType.item5;
 var b = ValueTypeAdapter.GetBytes(out lengh,enumType, EndianType.Big,typeof(uint));
 var dsd = b.Select(r => { if (r == 4) return r = 6; return r; }).ToArray();
 var c = ValueTypeAdapter.GetValue<EnumType>(out lengh, dsd, EndianType.Big, typeof(uint));
 var d = ValueTypeAdapter.GetValue(out lengh, dsd, EndianType.Big, typeof(uint),typeof(EnumType));

 object obj = (byte)5;
 byte[] bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
 object obj_2 = ValueTypeAdapter.GetValue<byte>(out lengh, bytearray, EndianType.Big);

 obj = "张图帅" as string;
 bytearray = StringTypeAdapter.GetBytes(obj.ToString(), 8);
 obj_2 = StringTypeAdapter.GetString(bytearray, 8);
 obj = (short)55;
 bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
 obj_2 = ValueTypeAdapter.GetValue<short>(out lengh, bytearray, EndianType.Big);
 obj = (float)6.666;
 bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
 obj_2 = ValueTypeAdapter.GetValue<float>(out lengh, bytearray, EndianType.Big);

 obj = new EnumType[] { EnumType.item5, (EnumType)6 };
 bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big, typeof(short));
 obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as EnumType[]).Length, EndianType.Big, typeof(short));
 obj = new float[] { 1.6f, 6f };
 bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big);
 obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as float[]).Length, EndianType.Big);

 obj = new List<EnumType>() { EnumType.item5, (EnumType)6 };
 bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big, typeof(short));
 obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<EnumType>).Count, EndianType.Big, typeof(short));
 obj = new List<float>() { 1.6f, 6f };
 bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big);
 obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<float>).Count, EndianType.Big);

```

### 类库食用示例

类库定义，协议中某命令的数据层翻译为类库

```cs
    public class TestCommArrObj:BaseCommunicateArrtObject
    {
        [CommunicateArrtibute(OrderIndex = 0, EnumEndType = typeof(short),AutoLengthType = typeof(byte))]
        public EnumType[] enumList { get; set; }
        [CommunicateArrtibute(OrderIndex = 1)]
        public byte aa { get; set; }

        [ValidCheckArrtibute("CommunicateUtil", "ValidateIsFalse")]
        [CommunicateArrtibute(OrderIndex = 2)]
        public short bb { get; set; }

        [CommunicateArrtibute(OrderIndex = 2.1f,ArrayLength = "32")]
        public string str { get; set; }

        [CommunicateArrtibute(OrderIndex = 3)]
        public EnumType EnumType { get; set; }

        [CommunicateArrtibute(OrderIndex = 34)]
        public EnumType EnumType_1 { get; set; }
        [ValidCheckArrtibute("CommunicateUtil", "ValidateIsFalse")]
        [CommunicateArrtibute(OrderIndex = 35,AutoLengthType = typeof(byte))]
        public TestCommArrObj_1[] arrObj_1 { get; set; }

    }
    public class TestCommArrObj_1 : BaseCommunicateArrtObject
    {
        [CommunicateArrtibute(OrderIndex = 1)]
        public byte DoubleLengh { get; set; }
        [CommunicateArrtibute(OrderIndex = 2, EnumEndType = typeof(short))]
        public EnumType Doubles { get; set; }

    }
```

编码解码

```cs
//复杂对象测试basecommobj嵌套
int objlengh = 0;
TestCommArrObj commArrObj = new TestCommArrObj();
commArrObj.enumList = new EnumType[] { EnumType.item5, EnumType.item4 };
commArrObj.aa = 1;
commArrObj.bb = 2;
commArrObj.str = "张图超帅";
commArrObj.EnumType = EnumType.item5;
commArrObj.EnumType_1 = (EnumType)6;
commArrObj.arrObj_1 = new TestCommArrObj_1[]
{ 
    new TestCommArrObj_1() { DoubleLengh = 5, Doubles = EnumType.item5 },
    new TestCommArrObj_1() { DoubleLengh = 3, Doubles = EnumType.item3 },
};
ValidationUtil.LoadValidationMethods();
byte[] bytes = commArrObj.ClassGetBytesLogic(out objlengh);
TestCommArrObj commArrObj_1 = new TestCommArrObj();
commArrObj_1.ClassGetValueLogic(out objlengh, bytes.ToList());
```

## 食用细节介绍

```cs
    /// <summary>
    /// 通讯特征类
    /// </summary>
    public class CommunicateArrtibute : Attribute
    {
        /// <summary>
        /// 通讯字段或属性的排序索引
        /// </summary>
        public float OrderIndex { get; set; } = 0;

        /// <summary>
        /// 通讯字段或属性在字节流数据中的起始索引,食用于上一属性结束所有与当前属性起始索引不相同的情况
        /// </summary>
        public int StartIndex { get; set; } = -1;

        /// <summary>
        /// 通讯字段或属性的数组或集合长度(适用于协议中数组长度后未紧跟数组内容的情况)
        /// 固定长度时填写数字，动态长度时填写动态长度所在的属性名称
        /// </summary>
        public string ArrayLength { get; set; } = "-1";

        /// <summary>
        /// 通讯字段或属性的数组或集合长度类型(适用于协议中数组长度后紧跟数组内容的情况)
        /// 数据解析器会根据类型进行解析及编码(不用在类中的属性中再做定义了)
        /// </summary>
        public Type AutoLengthType { get; set; }
        
        /// <summary>
        /// 枚举结束类型(根据枚举在协议中所占的长度决定)
        /// </summary>
        public Type EnumEndType { get; set; }

        /// <summary>
        /// 通讯字段或属性的字节编码类型
        /// </summary>
        public EndianType EndianType { get; set; } = EndianType.Big;
    }
```
