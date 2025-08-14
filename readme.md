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

# 注册生物群系
### 1.打开[群系管理类](script/WorldControl/BiomeManage.cs),调用 'RegBiome(Biome biome)' 方法注册新的生物群系,并配置好权重
    static BiomeManage()
    {
        ...
        //在尾部添加
            Register(new Biome
            {
                name = "我的地形",
                //生成权重
                weight = 2,
                //生成区块的高度,会由生成器插值生成高度图,所以这里不用考虑X轴和Y轴连续性
                GetHigh = (noise, x, z) => (int)(noise.GetNoise2D(x * Chunk.Size, z) * 8),
                //生成地形,x,y,z是外部循环,gx,gy是全局x,y坐标
                GeneratorTerrain = (chunk, highMap, blockStrcut, random, x, y, z, gx, gy) =>
                {
                    //这里的num,是当前坐标与地平线highmap的差值,以下计算皆在Chunk范围内,不会越界
                    int num = highMap[x, z] - gy;
                    if (num == 0) chunk[x, y, z] = Materials.Valueof("grass").Blockdata();
                    if (num == -1) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -2) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num == -3) chunk[x, y, z] = Materials.Valueof("dirt").Blockdata();
                    if (num <= -4) chunk[x, y, z] = Materials.Valueof("stone").Blockdata();
                },
                //生成建筑结构
                //用来交给生成器判断有没有建筑结构跨区块命中了某个区块
                GeneratorStrcut = (noise, random, strcuts, gx, gy, z) =>
                {
                    //控制生成概率
                    if (random.Next(14) != 1) return;
                    BlockStrcut blockStrcut = new BlockStrcut();
                    SetBlockWork sbw = blockStrcut.work;
                    //具体结构的生成
                    ..


                    strcuts.Add(blockStrcut);
                }
            });
    }