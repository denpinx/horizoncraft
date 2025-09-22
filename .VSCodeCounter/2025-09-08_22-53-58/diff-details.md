# Diff Details

Date : 2025-09-08 22:53:58

Directory c:\\Users\\Administrator\\Documents\\horizoncraft\\script

Total : 67 files,  276 codes, 35 comments, 9 blanks, all 320 lines

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details

## Files
| filename | language | code | comment | blank | total |
| :--- | :--- | ---: | ---: | ---: | ---: |
| [script/Components/Component.cs](/script/Components/Component.cs) | C# | 1 | 0 | 0 | 1 |
| [script/Components/ComponentManager.cs](/script/Components/ComponentManager.cs) | C# | 1 | 0 | 0 | 1 |
| [script/Components/ComponentSystem.cs](/script/Components/ComponentSystem.cs) | C# | 2 | 0 | 0 | 2 |
| [script/Components/EntityComponents/EntityComponent.cs](/script/Components/EntityComponents/EntityComponent.cs) | C# | -13 | 0 | -2 | -15 |
| [script/Components/Systems/FurnaceSystem.cs](/script/Components/Systems/FurnaceSystem.cs) | C# | -1 | 0 | 0 | -1 |
| [script/Components/Systems/ItemComponentSystem.cs](/script/Components/Systems/ItemComponentSystem.cs) | C# | 5 | 0 | 3 | 8 |
| [script/Components/Systems/ItemDurableSystem.cs](/script/Components/Systems/ItemDurableSystem.cs) | C# | 1 | 0 | 0 | 1 |
| [script/Components/TickSystem.cs](/script/Components/TickSystem.cs) | C# | 5 | 0 | 1 | 6 |
| [script/Entity/EntityData.cs](/script/Entity/EntityData.cs) | C# | 45 | 0 | 5 | 50 |
| [script/Entity/EntityMeta.cs](/script/Entity/EntityMeta.cs) | C# | -8 | 0 | -1 | -9 |
| [script/Entity/EntityNode.cs](/script/Entity/EntityNode.cs) | C# | -11 | -21 | -1 | -33 |
| [script/Entity/Entitydata.cs](/script/Entity/Entitydata.cs) | C# | -28 | 0 | -3 | -31 |
| [script/Entity/IEntityNode.cs](/script/Entity/IEntityNode.cs) | C# | -5 | 0 | -1 | -6 |
| [script/Events/PlayerEvents.cs](/script/Events/PlayerEvents.cs) | C# | 282 | 1 | 34 | 317 |
| [script/Events/player/PlayerEvent.cs](/script/Events/player/PlayerEvent.cs) | C# | 63 | 0 | 16 | 79 |
| [script/Features/EntityManage.cs](/script/Features/EntityManage.cs) | C# | -160 | -3 | -20 | -183 |
| [script/Horizoncraft.cs](/script/Horizoncraft.cs) | C# | -8 | 0 | 0 | -8 |
| [script/Interface/ICreateService.cs](/script/Interface/ICreateService.cs) | C# | 9 | 5 | 2 | 16 |
| [script/Interface/ITarget.cs](/script/Interface/ITarget.cs) | C# | 5 | 0 | 1 | 6 |
| [script/Materials.cs](/script/Materials.cs) | C# | -21 | 0 | -3 | -24 |
| [script/Net/ChunkSnapshot.cs](/script/Net/ChunkSnapshot.cs) | C# | 12 | 4 | 1 | 17 |
| [script/Net/EntityDataSnapShot.cs](/script/Net/EntityDataSnapShot.cs) | C# | 22 | 0 | 4 | 26 |
| [script/Net/EntityPack.cs](/script/Net/EntityPack.cs) | C# | 11 | 0 | 2 | 13 |
| [script/Net/PlayerDataSnapshot.cs](/script/Net/PlayerDataSnapshot.cs) | C# | 33 | 0 | 5 | 38 |
| [script/Net/PlayerdataSnapshot.cs](/script/Net/PlayerdataSnapshot.cs) | C# | -19 | 0 | -3 | -22 |
| [script/Net/UUIDPack.cs](/script/Net/UUIDPack.cs) | C# | 8 | 0 | 2 | 10 |
| [script/Net/WorldSnapshot.cs](/script/Net/WorldSnapshot.cs) | C# | 1 | 0 | 0 | 1 |
| [script/PlayerData.cs](/script/PlayerData.cs) | C# | 43 | 1 | 6 | 50 |
| [script/Services/Events/ClientPlayerEvents.cs](/script/Services/Events/ClientPlayerEvents.cs) | C# | 77 | 1 | 9 | 87 |
| [script/Services/chunk/ChunkServiceBase.cs](/script/Services/chunk/ChunkServiceBase.cs) | C# | 359 | 88 | 52 | 499 |
| [script/Services/chunk/ClientChunkService.cs](/script/Services/chunk/ClientChunkService.cs) | C# | 59 | 1 | 10 | 70 |
| [script/Services/chunk/HostChunkService.cs](/script/Services/chunk/HostChunkService.cs) | C# | 134 | 10 | 16 | 160 |
| [script/Services/chunk/PreviewChunkService.cs](/script/Services/chunk/PreviewChunkService.cs) | C# | 23 | 0 | 5 | 28 |
| [script/Services/chunk/SingleChunkService.cs](/script/Services/chunk/SingleChunkService.cs) | C# | 13 | 0 | 3 | 16 |
| [script/Services/entity/ClientEntityService.cs](/script/Services/entity/ClientEntityService.cs) | C# | 9 | 0 | 2 | 11 |
| [script/Services/entity/EntityServiceBase.cs](/script/Services/entity/EntityServiceBase.cs) | C# | 218 | 6 | 28 | 252 |
| [script/Services/entity/HostEntityService.cs](/script/Services/entity/HostEntityService.cs) | C# | 27 | 1 | 3 | 31 |
| [script/Services/player/ClientPlayerService.cs](/script/Services/player/ClientPlayerService.cs) | C# | 51 | 0 | 9 | 60 |
| [script/Services/player/HostPlayerService.cs](/script/Services/player/HostPlayerService.cs) | C# | 106 | 8 | 10 | 124 |
| [script/Services/player/PlayerServiceBase.cs](/script/Services/player/PlayerServiceBase.cs) | C# | 223 | 3 | 27 | 253 |
| [script/Services/player/PreviewPlayerService.cs](/script/Services/player/PreviewPlayerService.cs) | C# | 31 | 7 | 6 | 44 |
| [script/Services/player/SinglePlayerService.cs](/script/Services/player/SinglePlayerService.cs) | C# | 11 | 0 | 2 | 13 |
| [script/Services/world/ClientWorldService.cs](/script/Services/world/ClientWorldService.cs) | C# | 40 | 0 | 7 | 47 |
| [script/Services/world/HostWorldService.cs](/script/Services/world/HostWorldService.cs) | C# | 52 | 0 | 6 | 58 |
| [script/Services/world/PreviewWorldService.cs](/script/Services/world/PreviewWorldService.cs) | C# | 21 | 0 | 4 | 25 |
| [script/Services/world/SingleWorldService.cs](/script/Services/world/SingleWorldService.cs) | C# | 21 | 0 | 5 | 26 |
| [script/Services/world/WorldServiceBase.cs](/script/Services/world/WorldServiceBase.cs) | C# | 31 | 6 | 8 | 45 |
| [script/Test/test.cs](/script/Test/test.cs) | C# | 0 | 0 | 1 | 1 |
| [script/WorldControl/Chunk.cs](/script/WorldControl/Chunk.cs) | C# | 1 | -1 | 1 | 1 |
| [script/WorldControl/Service/IWorldClientService.cs](/script/WorldControl/Service/IWorldClientService.cs) | C# | -5 | 0 | -1 | -6 |
| [script/WorldControl/Service/IWorldHostService.cs](/script/WorldControl/Service/IWorldHostService.cs) | C# | -7 | -5 | -3 | -15 |
| [script/WorldControl/Service/IWorldService.cs](/script/WorldControl/Service/IWorldService.cs) | C# | -16 | -28 | -11 | -55 |
| [script/WorldControl/Service/IWorldTickable.cs](/script/WorldControl/Service/IWorldTickable.cs) | C# | -5 | -1 | -1 | -7 |
| [script/WorldControl/Service/WorldBase.cs](/script/WorldControl/Service/WorldBase.cs) | C# | -607 | -34 | -89 | -730 |
| [script/WorldControl/Service/WorldClientService.cs](/script/WorldControl/Service/WorldClientService.cs) | C# | -250 | -8 | -43 | -301 |
| [script/WorldControl/Service/WorldHostService.cs](/script/WorldControl/Service/WorldHostService.cs) | C# | -472 | -21 | -71 | -564 |
| [script/WorldControl/Service/WorldPreviewService.cs](/script/WorldControl/Service/WorldPreviewService.cs) | C# | -100 | -4 | -17 | -121 |
| [script/WorldControl/Service/WorldSingleService.cs](/script/WorldControl/Service/WorldSingleService.cs) | C# | -213 | -6 | -35 | -254 |
| [script/WorldControl/Struct/PerbuildStruct.cs](/script/WorldControl/Struct/PerbuildStruct.cs) | C# | 37 | 0 | 5 | 42 |
| [script/WorldControl/Tool/SqliteTool.cs](/script/WorldControl/Tool/SqliteTool.cs) | C# | 11 | 3 | 3 | 17 |
| [script/WorldParials/Host/WorldHostRpc\_Player.cs](/script/WorldParials/Host/WorldHostRpc_Player.cs) | C# | -101 | 0 | -12 | -113 |
| [script/WorldParials/WorldClientRpc.cs](/script/WorldParials/WorldClientRpc.cs) | C# | -121 | -1 | -16 | -138 |
| [script/WorldParials/WorldHostRpc.cs](/script/WorldParials/WorldHostRpc.cs) | C# | -51 | -1 | -7 | -59 |
| [script/rpc/ChunkServiceNode.cs](/script/rpc/ChunkServiceNode.cs) | C# | 120 | 3 | 14 | 137 |
| [script/rpc/EntityServiceNode.cs](/script/rpc/EntityServiceNode.cs) | C# | 49 | 3 | 9 | 61 |
| [script/rpc/PlayerInventoryServiceNode.cs](/script/rpc/PlayerInventoryServiceNode.cs) | C# | 105 | 3 | 11 | 119 |
| [script/rpc/PlayerServiceNode.cs](/script/rpc/PlayerServiceNode.cs) | C# | 120 | 15 | 11 | 146 |

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details