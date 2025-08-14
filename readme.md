# 已实现组件类

|组件名|描述|父类|
|----|----|----|
|[Component](script/Components/Component.cs)|所有组件的基类|无|
|[TickComponent](script/Components/TickComponent.cs)|时刻组件,所有方块组件的基类|Component|
|[ExpandComponent](script/Components/ExpandComponent.cs)|扩展组件,让TickComponent可以自定义操作对象|TickComponent|
|[FluidComponent](script/Components/FluidComponent.cs)|流体组件,让方块具有流体属性|ExpandComponent|
|[PhysicsComponent](script/Components/PhysicsComponent.cs)|物理组件,方块会像沙子一样下坠|ExpandComponent|

# 已实现组件功能
|组件类型|行为类型|描述|
|----|----|----|
|[ExpandComponent](script/Components/ExpandComponent.cs)|BlockCover|配置BlockName,当方块被覆盖时变成指定方块|
|[ExpandComponent](script/Components/ExpandComponent.cs)|BlockSpread|配置BlockName,让方块能够被任意方块蔓延|
|[TickComponent](script/Components/TickComponent.cs)|BottomCheck|检查底部是否为完整方块，如果不是则消失|
|[FluidComponent](script/Components/FluidComponent.cs)|FluidComponent|流体组件,配置BlockName,即可让任意方块实现流体功能|
|[PhysicsComponent](script/Components/PhysicsComponent.cs)|PhysicsComponent|物理组件,配置BlockName,即可让任意方块能够像让沙子一样下坠|
# 注册方块
### 1.打开[配置文件](config\block\Materials.json)
#### 添加配置
    {
        ....
        "方块名1":{}, //可省略内部，按需配置
        "方块名2":{
            "collide": false,   //碰撞.
            "cube": false,      //是否为完整方块,非完整方块将会绘制其背后的方块.
            "tiletype":"tile",  //tile:需要一个(长n:宽n)*16*16的贴图作为tile,绘制tile时会根据坐标自动映射，可以配置不同state的不同贴图.
                                //atlas:模式需要(长1:宽n)*16*16的贴图，会根据方块状态自动去映射贴图，只支持一张贴图.
            "components":{
                "TickComponent": {  //组件的类型,如果要配置组件,这里Name必须配置，其他字段可以省略
                    "Name": "BottomCheck",  //组件的功能名
                    "Max": 20               //组件属性，名称大小写和值必须完全一致，会被自动反射构建为 lambda
                                            //()=> new TickComponent(){Name="BottomCheck",Max=20}
                }
            }
            "state":{   
                "状态一":{ //状态名是占位和提高可读性用的，按顺序配置  
                    "texture": "oak_log_状态1"    //不同状态的方块会渲染不同的Tile,省略图片类型,统一使用.png
                },
                "状态二"{
                    "texture": "oak_log_状态2"
                }
            }
        }
    }
#### 如果直接配置已有的组件可以省略以下步骤
### 2.新建组件类,继承[组件](script/Components/Component.cs),注册新的组件类型
    [MemoryPackable]
    public partial class MyComponent : TickComponent
    {
        //这里只写字段，最好不要包含任何方法
    }
### 3.打开[组件管理器](script/Components/ComponentManager.cs),注册新的组件类型
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
### 4.在[组件](script/Components/Component.cs)类中标记新建的组件,用于MemoryPack序列化
    ...
    //在这里添加，注意下标请勿重复
    [MemoryPackUnion(999, typeof(MyComponent))]
    public abstract partial class Component
    {
        public string Name;
    }
### 5.在[LambdaCreater](script/Components/LambdaCreater.cs#L12)类中注册新建的组件类用于自动创建Lambda函数
    static LambdaCreater()
    {
        ...
        //在尾部添加
        Register<MyComponent>();
    }
# 注册实体
## 此功能是半成品，只实现了跟随区块加载和卸载
### 1.创建一个新场景，继承[EntityNode](script/Entity/EntityNode.cs) 作为实体的游戏实列
### 2.在[Materials](script/Materials.cs)中创建[EntityMeta](script/Entity/EntityMeta.cs)注册实体,并绑定tscn文件
### 3.暂时不支持自定义[EntityData](script/Entity/Entitydata.cs)
###

# 生物群系
|生物群系类型|继承|描述|
|----|----|----|
|BaseBiome|无|生物群系基类
|LandBiome|BaseBiome|地表群系
|Biome|BaseBiome|二维群系

LandBiome生成基于X轴计算,Biome是在LandBiome的基础上再计算的结果,两则不会冲突

## 注册生物群系
### 1.在 'script\WorldControl\worldbiomes' 中创建一个名为 XXXBiome.cs的文件
创建地下群系:
    public class MyDeepBiome : Biome
    {
        public MyDeepBiome()
        {
            //群系名
            name = "我的地下群系";
            //群系类型,决定当前群戏属于天空群戏还是地下群系
            biomeType = BiomeType.Deep;
            //权重,越大生成概率越高
            weight = 1;


            //外循环驱动
            //Chunk 区块
            //highmap 一维高度图,在地下群系和天空群系一般没什么用
            //noise [-1,1]的噪音值
            //x,y,z 方块局部坐标
            //gx,gy 方块全局坐标
            GeneratorTerrain = (Chunk, highmap, noise, x, y, z, gx, gy) =>
            {
                if (noise > 0.3f && z == 1) Chunk[x, y, z] = Materials.Valueof("air").Blockdata();
                else
                    Chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
            };

            //二维群系的结构生成和一维不同，这里没有外部循环在区块内随机生成就行,将结果添加到struct中
            //fastnoiselite 噪音函数
            //rand 随机数生成器
            //structs 存储结构集合
            GeneratorStrcut = (fastnoiselite,rand,structs,gx,gy)=>{
                BlockStruct blockStrcut = new BlockStruct();//结构集合
                SetBlockWork sbw = blockStrcut.work;        //单个结构方块集合
                //
                sbw.ExclList.Add(
                    new(
                        //全局坐标
                        new(x,y,0),
                        //方块类型
                        Materials.Valueof("oak_log"),
                        //方块状态 
                        1
                        ));
                //返回结果
                structs.add(blockStrcut)
            };
        }
    }
创建地表群系
    public class MyLandBiome : LandBiome
    {
        public MyLandBiome()
        {
            name = "我的地表群系";
            weight = 3;
            //计算当前区块的高度,不用连续，只要相同输入能给出固定输出就行,之后会交给生成器和周边区块插值
            GetHigh = (noise, x, z) => ((int)(noise.GetNoise2D(x * Chunk.Size, z) * 64)) - new Random(HashCode.Combine(x, z)).Next(8);
            //外循环驱动
            //fastnoiselite 噪音函数
            //random 随机数生成器
            //structs 存储结构集合
            //gx,gy,z当前全局坐标
            GeneratorStrcut = (fastnoiselite, random, structs, gx, gy, z) =>
            {
                //控制生成概率,这里一定要用这个random对象生成，确保每个区块生成的结构多次调用能够有相同的结果
                if (random.Next(7) != 1) return;
                BlockStruct blockStrcut = new BlockStruct();
                SetBlockWork sbw = blockStrcut.work;
                //在这个坐标系中，-y是向上，+y是向下
                //这里是判断在海平面以上才生成结构 
                if (gy < 0)
                    for (int h = 0; h < 5 + random.Next(4); h++)
                    {
                        sbw.ExclList.Add(new(new(gx, gy - h, z), Materials.Valueof("oak_log"), 0));
                        //随机分支
                        if (random.Next(4) == 1)
                        {
                            sbw.ExclList.Add(new(new(gx - 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                            sbw.ExclList.Add(new(new(gx - 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                            sbw.ExclList.Add(new(new(gx - 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                        }
                        if (random.Next(4) == 2)
                        {
                            sbw.ExclList.Add(new(new(gx + 1, gy - h, z), Materials.Valueof("oak_log"), 1));
                            sbw.ExclList.Add(new(new(gx + 1, gy - h - 1, z), Materials.Valueof("oak_leaves"), 1));
                            sbw.ExclList.Add(new(new(gx + 2, gy - h, z), Materials.Valueof("oak_leaves"), 1));
                        }
                    }
                structs.Add(blockStrcut);
            };
            //外循环驱动
            GeneratorTerrain = (noise,chunk, highMap, random, x, y, z, gx, gy) =>
            {
                //这里的num是指当前方块与高度图的差距
                int num = highMap[x, z] - gy;
                if (gy > 0 && highMap[x, z] > 0)//地下
                {
                    if (num > 0) chunk[x, y, z] = Materials.Valueof("water").Blockdata();
                    if (num == 0) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -2) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                    if (num == -3)
                    {
                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("sand").Blockdata();
                        else chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                    }
                }
                else//地上
                {
                    if (num == 1)
                    {
                        if (random.Next(2) == 1) chunk[x, y, z] = Materials.Valueof("bush").Blockdata();
                    }
                    if (num == 0) chunk[x, y, z] = Materials.Valueof("grass").Blockdata();
                    if (num == -1) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -2) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -3) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                }
                if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
            };
        }
    }

### 2.打开[群系管理类](script/WorldControl/BiomeManage.cs),调用 'RegBiome(Biome biome)' 方法注册新的生物群系,并配置好权重
    static BiomeManage()
    {
        ...
        //在尾部添加
        Register(new MyBiome());
    }