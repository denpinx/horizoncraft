# 前言

    主要玩法功能已经可以支撑游戏的循环升级玩法了。
    接下来的工作就是完善次要功能和辅助工具,以及添加新的内容。

## 工作记录

| 功能       | 状态 | 描述                      |
|----------|----|-------------------------|
| 区块同步     | √  | 已完成                     |
| 玩家同步     | √  | 已完成                     |
| 配方合成     | √  | 已完成                     |
| 主动时刻更新   | √  | 已完成                     
| 双端事件处理   | √  | 已完成                     |
| 方块事件处理   | √  | 已完成                     |
| 物品事件处理   | √  | 已完成                     |
| 实体事件处理   | √  | 已完成                     |
| 增量同步     | √  | 已完成                     |
| 实体删除同步   | √  | 已完成                     |
| 被动时刻更新   | ×  | 未完成                     |
| 建筑结构生成工具 | ×  | 完成一半，但是结构生成未实装          |
| 物品管理器    | ×  | 完成一半，翻页和搜索功能未完成,配方查询未完成 |

# 最近更新

    1.优化UI操作体验，优化代码。

# 目前已实现功能

| 世界服务类               | 描述    |
|---------------------|-------|
| SingleWorldService  | 单机模式  |
| HostWorldService    | 主机模式  |
| ClientWorldService  | 客户端模式 |
| PreviewWorldService | 预览模式  |

| 服务端功能服务类          | 描述               |
|-------------------|------------------|
| HostPlayerService | 玩家服务类,额外增加玩家同步功能 |
| HostChunkService  | 区块服务类，额外增加区块同步功能 |

| 客户端功能服务类            | 描述               |
|---------------------|------------------|
| ClientPlayerService | 客户端玩家服务类,没有存档和加载 |
| ClientChunkService  | 客户端区块服务类,没有存档和加载 |

| 单机功能服务类             | 描述                     |
|---------------------|------------------------|
| SinglePlayerService | 客户端玩家服务类,没有任何修改，拥有全部功能 |
| SingleChunkService  | 客户端区块服务类,没有任何修改,拥有全部功能 |

| 基础功能服务类           | 描述                                             |
|-------------------|------------------------------------------------|
| PlayerServiceBase | 玩家服务类,提供玩家管理,资源异步加载功能,保存功能                     |
| EntityServiceBase | 实体服务类,提供实体管理,实例所属权管理,所属权转移到客户端计算物理功能,实体卸载和保存功能 |
| ChunkServiceBase  | 区块服务类，提供区块管理，区块异步加载和在异步内创建区块，并生成地形，光照计算，卸载功能   |

| 其他已完成功能        | 描述             |
|----------------|----------------|
| WorldGenerator | 区块生成器,支持异步生成区块 |
| BiomeManage    | 群系管理工厂类        |
| RecipeManage   | 合成配方管理         |

# 多人相关

    目前已经客户端实现增量更新至服务端，服务端增量同步给客户端。
    客户端代理服务端计算实体物理，并在服务端加载客户端的代理实体所在区块时拿回所属权。

### 不要直接在玩家节点中直接调用使世界服务等各种功能服务实现事件,会导致结果只会在客户端生效

    因为玩家节点的一切操作都是客户端操作，玩家节点内的输入操作只是决定什么时候调用该事件。
    必须先在 PlayerServiceBase.PlayerEvents类中 创建对应的事件方法。
    然后在PlayerServiceNode类型中实现该方法的远程调用版本。
    最后在ClientPlayerEvents类中override该方法,将其改成rpc远程调用。

    总结: 客户端PlayerNode调用->ClientPlayerEvents->调用服务端PlayerServiceNode调用服务端->PlayerEvents。
    注意: PlayerServiceNode的服务端rpc方法和服务端接收方法不要共用同一个会造成死循环。

# 注册方块

### 1.打开“config\block\”目录,往里面添加任意文件，文件名不限

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
    },
    //配置掉落物列表
    "loot": [
      {
        //掉落物名
        "name": "方块名1",
        //掉落几率[0.0-1.0]，1为100%
        "drop-chance": 1,
        //掉落数量叠加几率，不写默认100%掉落1个，可写多个不同的数量和概率，最终会叠加计算
        "amount-chance": [
          {
            //数量
            "amount": 1,
            //100%掉落
            "chance": 1
          }
        ]
      }
    ]
    //扩展标签
    "tags": {
      //方块的破坏工具类型
      "type": "shovel"
    },
    "state": {
      "状态一": {
        //状态名是占位和提高可读性用的，按顺序配置  
        "texture": "oak_log_状态1"
        //不同状态的方块会渲染不同的Tile,省略图片类型,统一使用.png
      },
      "状态二": {
        //如果texture为场景名,可以添加上以下配置,将会注册场景tile
        //"scene": true
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

EntityData同样是使用了Component作为数据容器

## 如果要有就自定义数据就创建一个继承了EntityComponent的对象作为容器,同样的,标记上[MemoryPack]和在Component类上标记唯一Union

## 打开Materials.cs文件找到以下函数

```c#
        //处理实体注册
        private static void ProcessEntity()
        {
            ....
            //注册实体元数据,绑定实体名和实体场景文件
            RegEntityMeta(new EntityMeta("item_entity", "res://tscn/Entity/ItemEntity.tscn"));
        }
```

## 在世界中生成创建实体，创建EntityData对象,配置好名称和所需的组件，找到实体服务的SpawnEntity函数,把EntityData传递进去，它会生成实体,以及实体的uuid

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
        //rand的种子是已经计算好的，确定性的，不用担心结果不一样，直接使用就行，多次random.Next可以使结果更加平滑
        public override int GetHigh(Random random,FastNoiseLite noise, int x, int z)
        {
            return random.Next(-32, 10) + random.Next(-32, 10);
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

# 注册合成配方

## 打开 "config/recipes/" 目录，往里面添加任意json文件，可以创建任何目录，注意好分类即可（分类不是加载的条件）

##                               

```json
    {
  "type": "craft",
  //配方类型，这里表示是合成
  "tag": "player",
  //配方标签，表示属于玩家合成
  "recipes": [
    //这里支持多个配方,会和其他定义了相同tag和type的配方合并
    {
      "cost": [
        //这里是模板
        "#"
      ],
      "mask": {
        //模板的替换
        "#": "oak_log"
      },
      "result": [
        "oak_plank",
        4
      ]
    },
    {
      "cost": [
        //空气必须表明，不能直接用"!"来表示
        //必须满足长*宽都有填充
        "###",
        "-!-",
        "-!-"
      ],
      "mask": {
        "-": "air",
        //空气占位必须表明
        "!": "stick",
        "#": "oak_plank"
      },
      //因为是合成，结果只有1个物品
      "result": [
        "wood_pickaxe",
        1
      ]
    }
    }
```

```json
{
  "type": "process",
  //处理配方
  "tag": "furnace",
  //对象是熔炉
  "recipes": [
    {
      //处理tick时长
      "process": 20,
      //消耗
      "cost": [
        [
          "grass",
          1
        ]
      ],
      //结果
      "result": [
        [
          "dirt",
          1
        ]
      ]
    },
    {
      "process": 20,
      "cost": [
        [
          "dirt",
          1
        ]
      ],
      "result": [
        [
          "grass",
          1
        ]
      ]
    }
  ]
}
```

# 注册物品

## 打开"config/item/"目录,在该目录内的所有层级的json文件都会被自动加载，根据所需创建一个任意名称.json文件

```json
{
  //具体配置可以为空,会有默认配置的
  "iron_ingot": {},
  "copper_ingot": {},
  //物品名
  "wood_pickaxe": {
    //最大叠加
    "max": 1,
    //扩展tag标签
    "tags": {
      //燃烧值
      "fuel": "10"
    },
    //组件,这里只支持继承了ItemComponent的组件,
    "components": {
      //耐久组件类名
      "ItemDurableComponent": {
        //功能组件名
        "Name": "ItemDurableComponent",
        //工具破坏标签
        "Tag": "any|pickaxe",
        //工具等级
        "ToolLevel": 0,
        //工具效率
        "Efficiency": 1,
        //最大耐久
        "Max": 100,
        //当前耐久
        "Value": 100
      }
    }
  }
}
```
