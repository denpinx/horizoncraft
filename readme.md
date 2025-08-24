# 前言

    代码迭代的很快，文档不一定准确。

# 目前已实现功能

| 功能         | 相关类                                                       | 描述                      |
|------------|-----------------------------------------------------------|-------------------------|
| 旧异步区块加载和卸载 | WorldBase, IWorldService, IWorldTickable                  | 需要通过接口组合实现              |
| 实体生成和保存    | [EntityManage](script/Features/EntityManage.cs)           | 跟随区块卸载和加载               |
| 世界生成器      | [WorldGenerator](script/WorldControl/WorldGenerator.cs)   | 通过预算周围结构方块实现了方块伪跨区块生成   |
| 生物群系       | [BiomeManage](script/WorldControl/BiomeManage.cs)         | 根据当前生物群系数量自动配置权重        |
| 组件系统       | [ComponentManager](script/Components/ComponentManager.cs) | 根据组件搭配可以快速构建方块          |
| 多人联机       | WorldHostService                                          | 实现了区块同步,以及玩家位置同步,性能还待优化 |
| 物品系统,容器组件  | Materials                                                 | 实现了容器的多人同步,仅在玩家打开容器时同步  |

# 待实现功能

| 功能   | 描述     |
|------|--------|
| 游戏菜单 | 配合物品系统 | 已完成3个
| 合成功能 | 配合物品系统 |最后一块拼图

# 距离真正的可玩还差 20% 的进度

# 多人相关

服务器性能测试结果:
增量同步，静默状态

| 在线人数(不包含主机) | 同步范围 | 加载区块数 | 上传字节数     | 下载字节数  | 描述   |
|-------------|------|-------|-----------|--------|------|
| 3人          | 5*5  | 100区块 | 0KiB/s    | 180B/s | 玩家分散 |
| 3人          | 5*5  | 25区块  | 4.66KiB/s | 180B/s | 玩家聚集 |

总结:客户端做了增量同步给服务端，只有客户端移动时才会同步给服务端,服务端也做了增量同步,区块信息,玩家背包信息全部都是增量同步,当有一个玩家高速跑图时，服务端上传峰值是14KiB/s

| 类名                  | 继承与接口                                                       | 描述                            |
|---------------------|-------------------------------------------------------------|-------------------------------|
| WorldHostService    | WorldBase, IWorldService, IWorldHostService, IWorldTickable | 联机主机,拥有单机的全部功能,额外增加的远程同步功能    |
| WorldClientService  | WorldBase, IWorldService, IWorldTickable                    | 联机客户端,没有任何存储功能                |
| WorldSingleService  | WorldBase, IWorldService, IWorldTickable                    | 单机,拥有除联机外全部功能                 |
| WorldPreviewService | WorldBase, IWorldService, IWorldTickable                    | 预览模式,没有任何存储功能                 |
| WorldBase           | objcet                                                      | 存储世界的基本信息,如已加载的区块,待加载的区块,玩家等等 |
| IWorldService       | Interface                                                   | 通过接口强制要求每个子类实现所有方法,以免遗漏       |
| IWorldHostService   | Interface                                                   | 网络同步接口,要求必须实现里面的所有方法          |
| IWorldTickable      | Interface                                                   | Tick接口                        |

# 已实现组件类

| 组件名                                                           | 描述                           | 父类              |
|---------------------------------------------------------------|------------------------------|-----------------|
| [Component](script/Components/Component.cs)                   | 所有组件的基类                      | 无               |
| [TickComponent](script/Components/TickComponent.cs)           | 时刻组件,所有方块组件的基类               | Component       |
| [ExpandComponent](script/Components/ExpandComponent.cs)       | 扩展组件,让TickComponent可以自定义操作对象 | TickComponent   |
| [FluidComponent](script/Components/FluidComponent.cs)         | 流体组件,让方块具有流体属性               | ExpandComponent |
| [PhysicsComponent](script/Components/PhysicsComponent.cs)     | 物理组件,方块会像沙子一样下坠              | ExpandComponent |
| [InventoryComponent](script/Components/InventoryComponent.cs) | 容器组件，能够让方块存储物品               | ExpandComponent |

# 已实现组件功能

| 组件类型                                                        | 行为类型               | 描述                                 |
|-------------------------------------------------------------|--------------------|------------------------------------|
| [ExpandComponent](script/Components/ExpandComponent.cs)     | BlockCover         | 配置BlockName,当方块被覆盖时变成指定方块          |
| [ExpandComponent](script/Components/ExpandComponent.cs)     | BlockSpread        | 配置BlockName,让方块能够被任意方块蔓延           |
| [TickComponent](script/Components/TickComponent.cs)         | BottomCheck        | 检查底部是否为完整方块，如果不是则消失                |
| [FluidComponent](script/Components/FluidComponent.cs)       | FluidComponent     | 流体组件,配置BlockName,即可让任意方块实现流体功能     |
| [PhysicsComponent](script/Components/PhysicsComponent.cs)   | PhysicsComponent   | 物理组件,配置BlockName,即可让任意方块能够像让沙子一样下坠 |
| [InventoryComponent](script/Components/PhysicsComponent.cs) | InventoryComponent | 容器组件,默认无任何功能                       |

# 注册方块

### 1.打开[配置文件](config\block\Materials.json)

#### 添加配置

~~~json
    {
  ....
  "方块名1": {},
  //可省略内部，按需配置
  "方块名2": {
    "collide": false,
    //碰撞.
    "cube": false,
    //是否为完整方块,非完整方块将会绘制其背后的方块.
    "tiletype": "tile",
    //tile:需要一个(长n:宽n)*16*16的贴图作为tile,绘制tile时会根据坐标自动映射，可以配置不同state的不同贴图.
    //atlas:模式需要(长1:宽n)*16*16的贴图，会根据方块状态自动去映射贴图，只支持一张贴图.
    "components": {
      "TickComponent": {
        //组件的类型,如果要配置组件,这里Name必须配置，其他字段可以省略
        "Name": "BottomCheck",
        //组件的功能名
        "Max": 20
        //组件属性，名称大小写和值必须完全一致，会被自动反射构建为 lambda
        //()=> new TickComponent(){Name="BottomCheck",Max=20}
      }
    }
    "state": {
      "状态一": {
        //状态名是占位和提高可读性用的，按顺序配置  
        "texture": "oak_log_状态1"
        //不同状态的方块会渲染不同的Tile,省略图片类型,统一使用.png
      },
      "状态二": {
        "texture": "oak_log_状态2"
      }
    }
  }
}
~~~

#### 如果直接配置已有的组件可以省略以下步骤

### 2.新建组件类,继承[组件](script/Components/Component.cs),注册新的组件类型

```C#
    [MemoryPackable]
    public partial class MyComponent : TickComponent
    {
        //这里只写字段，最好不要包含任何方法
    }
```

### 3.打开[组件管理器](script/Components/ComponentManager.cs),注册新的组件类型

```C#
    static ComponentManager()
    {
        ...
        //在尾部添加
        Register("组件行为", () => new MyComponent(), new TickSystem()
        {
            Tick = (BlockTickEvent e, TickComponent cmp) =>
            {

            }
        });
    }
```

### 4.在[组件](script/Components/Component.cs)类中标记新建的组件,用于MemoryPack序列化

```C#
    //在这里添加，注意下标请勿重复
    [MemoryPackUnion(999, typeof(MyComponent))]
    public abstract partial class Component
    {
        public string Name;
    }
```

### 5.在[LambdaCreater](script/Components/LambdaCreater.cs#L12)类中注册新建的组件类用于自动创建Lambda函数

```C#
    static LambdaCreater()
    {
        ...
        //在尾部添加
        Register<MyComponent>();
    }
```

### 6.创建容器菜单

（1）打开Godot编辑器,创建新场景,将根节点设置为CanvasLayer类型

（2）添加C#脚本，继承InventoryNode,配置容器配置

（3）打开[InventoryManage]("script/Inventory/InventoryManage.cs") 在其中注册当前菜单

（4）打开Materials.json文件找到你想添加容器组件的方块,将其添加容器组件后在组件内配置 "InventoryName":"容器菜单"
属性,这个属性是决定玩家右键打开的是什么菜单

# 注册实体

## 此功能是半成品，只实现了跟随区块加载和卸载

### 1.创建一个新场景，继承[EntityNode](script/Entity/EntityNode.cs) 作为实体的游戏实列

### 2.在[Materials](script/Materials.cs)中创建[EntityMeta](script/Entity/EntityMeta.cs)注册实体,并绑定tscn文件

### 3.暂时不支持自定义[EntityData](script/Entity/Entitydata.cs)

###

# 生物群系

| 生物群系类型    | 继承        | 描述     |
|-----------|-----------|--------|
| BaseBiome | 无         | 生物群系基类 
| LandBiome | BaseBiome | 地表群系   
| Biome     | BaseBiome | 二维群系   

LandBiome生成基于X轴计算,Biome是在LandBiome的基础上再计算的结果,两则不会冲突

## 注册生物群系

### 1.在 'script\WorldControl\worldbiomes' 中创建一个名为 XXXBiome.cs的文件

### 创建地下群系:

```C#
    public class MyDeepBiome : Biome
    {
        public MyDeepBiome()
        {
            name = "MyDeepBiome";
            biomeType = BiomeType.Deep;
            weight = 100;
        }
        //外部循环驱动,只用管当前坐标的方块生成即可
        public override void GeneratorTerrain(BiomeTerrainContext btc)
        {

        }
        //无外循环驱动,根据当前区块坐标自定义建筑结构
        public override void GeneratorStruct(BiomeStructContext bsc)
        {
            //控制生成概率
            if(bsc.Random.Next(16)!=1)return;
            
            var struct = new BlockStruct();
            //生成结构
            struct.AddBlock(bsc.GlobalX,bsc.GlobalY,0,Materials.Valueof("stone"));
            ...

            
            bsc.BlockStructs.Add(struct)
        }
    }
```

### 创建地表群系

```C#
    public class MyLandBiome : LandBiome
    {
        public MyLandBiome()
        {
            name = "MyLandBiome";
            weight = 1;
        }
        //用HashCode.Combine来控制随机数种子,不要求连续,只要确定性
        //！！禁止用 return new Random().Next(); 来返回不确定的高度,会导致地形无法预测。
        public override int GetHigh(FastNoiseLite noise, int x, int z)
        {
            return -Math.Abs(
                (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8) - new Random(HashCode.Combine(x, z)).Next(4));
        }

        public override void GeneratorTerrain(BiomeTerrainContext context)
        {
            int num = context.HighMap[context.LocalX, context.GlobalZ] - context.GlobalY; //和当前的插值
            if (context.GlobalY > 0 && context.HighMap[context.LocalX, context.GlobalZ] > 0) //地下
            {
                switch (num)
                {
                    case > 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("water").Blockdata();
                        break;
                    case 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -1:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -2:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        break;
                    case -3:
                        if (context.Random.Next(2) == 1)
                            context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("sand").Blockdata();
                        else context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                    case <= -4:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                }
            }
            else //地上
            {
                switch (num)
                {
                    case 1:
                        if (context.Random.Next(2) == 1)
                            context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("bush").Blockdata();
                        break;
                    case 0:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("grass").Blockdata();
                        break;
                    case -1:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case -2:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case -3:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("dirt").Blockdata();
                        break;
                    case <= -4:
                        context.Chunk[context.LocalX, context.LocalY, context.GlobalZ] = Materials.Valueof("stone").Blockdata();
                        break;
                }
            }
        }
    }
```

### 2.打开[群系管理类](script/WorldControl/BiomeManage.cs),调用 'Register(BaseBiome baseBiome)' 方法注册新的生物群系,并配置好权重

```C#
    static void RegBiomes()
    {
        ...
        //在尾部添加
        Register(new MyDeepBiome());
        Register(new MyLandBiome());
    }
```