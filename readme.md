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
            //地下名称
            name = "我的地形",
            //生成权重
            weight = 1,
            //地形基本高度,x为区块坐标，相同输入，相同输出，在多个区块之间的高度插值生成地形,实现可控地形高度
            GetHigh = (noise, x, z) => -Math.Abs((int)(noise.GetNoise2D(x * Chunk.Size, z) * 8) - new Random(HashCode.Combine(x, z)).Next(4)),
            //生成地形,如果不想写，可以直接复用,遍历区块所有方块
            GeneratorTerrain = (chunk, highMap, blockStrcut)=>{
                for (int z = 0; z < 2; z++)
                {
                    Random random = new Random(chunk.X * 3 + chunk.Y * 7 + z * 11);
                    for (int x = 0; x < Chunk.Size; x++)
                        for (int y = 0; y < Chunk.Size; y++)
                        {
                            //特别注意,这个函数内只能用 chunk[x, y, z] 去设置方块，请勿用 chunk[x+K, y+J, z]等等,这样就不符合区块的生成守则了
                            //每个区块只能生成自己区块内的方块，不能跨区块生成

                            int gx = chunk.X * Chunk.Size + x;
                            int gy = chunk.Y * Chunk.Size + y;
                            int num = highMap[x, z] - gy;//和当前的插值
                            if (gy > 0 && highMap[x, z] > 0)//小于海平面填充水，生成沙子覆盖
                            {
                                //这里开始自定义
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
                            else//高于海平面，生成草方块和泥土
                            {
                                //这里开始自定义
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

                            //
                            (BlockMeta, int) data = WorldGenerator.GetStructData(blockStrcut, gx, gy, z);
                            if (data.Item1 != null)
                            {
                                chunk[x, y, z] = data.Item1.Blockdata();
                                chunk[x, y, z].STATE = data.Item2;
                            }
                        }
                }
            },
            //生成建筑结构表,也可以复用,注意,这里只是生成结构表，并不是真的生成结构
            GeneratorStrcut =  (noise, x, y, z)=>{
                List<BlockStrcut> strcuts = new List<BlockStrcut>();
                int[,] highmap = WorldGenerator.GetHighMap(x);
                //你的结构生成判断写在这,其他区块生成时会调用这个来判断当前结构是否命中了某个方块
                return strcuts;
            }
        });
    }