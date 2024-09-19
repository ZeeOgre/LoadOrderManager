-- Script Date: 9/19/2024 11:40 AM  - ErikEJ.SqlCeScripting version 3.5.2.95
-- Database information:
-- Database: S:\DevRepo\ZO.LoadOrderManager\ZO.LOM.App\data\LoadOrderManager_CLEAN.db
-- ServerVersion: 3.40.0
-- DatabaseSize: 112 KB
-- Created: 9/18/2024 10:57 AM

-- User Table information:
-- Number of tables: 9
-- Config: -1 row(s)
-- ExternalIDs: -1 row(s)
-- FileInfo: -1 row(s)
-- GroupSet: -1 row(s)
-- InitializationStatus: -1 row(s)
-- LoadOutProfiles: -1 row(s)
-- ModGroups: -1 row(s)
-- Plugins: -1 row(s)
-- ProfilePlugins: -1 row(s)

SELECT 1;
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE [ModGroups] (
  [GroupID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [Ordinal] bigint NULL,
  [GroupName] text NULL,
  [Description] text NULL,
  [ParentID] bigint NULL,
  [GroupSetID] bigint NULL,
  CONSTRAINT [FK_ModGroups_0_0] FOREIGN KEY ([ParentID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [Plugins] (
  [PluginID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginName] text NOT NULL,
  [Description] text NULL,
  [Achievements] bigint NOT NULL,
  [DTStamp] text NOT NULL,
  [Version] text NULL,
  [State] bigint NULL,
  [GroupID] bigint NULL,
  [GroupOrdinal] bigint NULL,
  CONSTRAINT [FK_Plugins_0_0] FOREIGN KEY ([GroupID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [LoadOutProfiles] (
  [ProfileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [ProfileName] text NOT NULL,
  [GroupSetID] bigint NULL
);
CREATE TABLE [ProfilePlugins] (
  [ProfileID] bigint NOT NULL,
  [PluginID] bigint NOT NULL,
  CONSTRAINT [sqlite_autoindex_ProfilePlugins_1] PRIMARY KEY ([ProfileID],[PluginID]),
  CONSTRAINT [FK_ProfilePlugins_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT [FK_ProfilePlugins_1_0] FOREIGN KEY ([ProfileID]) REFERENCES [LoadOutProfiles] ([ProfileID]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [InitializationStatus] (
  [Id] bigint NOT NULL,
  [IsInitialized] bigint NOT NULL,
  [InitializationTime] text NOT NULL,
  CONSTRAINT [sqlite_autoindex_InitializationStatus_1] PRIMARY KEY ([Id])
);
CREATE TABLE [GroupSet] (
  [GroupSetID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [GroupSetName] text NOT NULL,
  [GroupSetFlags] int DEFAULT (0) NOT NULL
);
CREATE TABLE [FileInfo] (
  [FileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginID] bigint NULL,
  [Filename] text NOT NULL,
  [RelativePath] text NULL,
  [DTStamp] datetime NOT NULL,
  [HASH] text NULL,
  [IsArchive] bigint NOT NULL,
  CONSTRAINT [FK_FileInfo_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
CREATE TABLE [ExternalIDs] (
  [ExternalID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginID] bigint NULL,
  [BethesdaID] text NULL,
  [NexusID] text NULL,
  CONSTRAINT [FK_ExternalIDs_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
CREATE TABLE [Config] (
  [GameFolder] text NOT NULL,
  [AutoCheckForUpdates] bigint NULL
);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-999,0,'CoreGameFiles','This is a reserved group for mods that are an integral part of the game and can''t be controlled by the player',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-998,0,'NeverLoad','This is a reserved group for mods which should never be loaded',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-997,0,'Uncategorized','This is a reserved group to temporarily hold uncategorized mods',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
1,0,'(Default Group)','This is the Default Root group which holds all the other groups',NULL,1);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
1,'blueprintships-starfield.esm','Core game file containing all the ship models (We think!)',1,'2024-08-20 18:18:57',NULL,NULL,-999,1);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
2,'constellation.esm','Premium Edition Content',1,'2024-06-28 00:43:13',NULL,NULL,-999,2);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
4,'sfbgs003.esm','Tracker''s Alliance update',1,'2024-08-20 18:18:57',NULL,NULL,-999,4);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
5,'sfbgs004.esm','REV-8 Vehicle',1,'2024-08-20 18:19:01',NULL,NULL,-999,5);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
6,'sfbgs006.esm','Empty Ship Habs and Decorations',1,'2024-06-28 00:22:40',NULL,NULL,-999,6);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
7,'sfbgs007.esm','Add "GamePlay Options" Menu',1,'2024-08-20 18:19:16',NULL,NULL,-999,7);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
8,'sfbgs008.esm','New Map design (3d maps)',1,'2024-08-20 18:18:57',NULL,NULL,-999,8);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
9,'starfield.esm','The core Starfield game',1,'2024-08-20 18:18:57',NULL,NULL,-999,9);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
10,'A1_EMPTY_STUB_XXXXXXXXXX.esm','JMPz11''s stub for converting mods between xEdit and Creation Kit, WILL crash your game if you try to load it.',0,'2024-06-24 19:53:00',NULL,NULL,-998,1);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[State],[GroupID],[GroupOrdinal]) VALUES (
3,'oldmars.esm','Premium Edition - Old Mars Skins',1,'2024-09-19',NULL,NULL,-999,3);
INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName],[GroupSetID]) VALUES (
1,'(Default Profile)',1);
INSERT INTO [GroupSet] ([GroupSetID],[GroupSetName],[GroupSetFlags]) VALUES (
1,'Default',9);
INSERT INTO [ExternalIDs] ([ExternalID],[PluginID],[BethesdaID],[NexusID]) VALUES (
1,9,NULL,'10189');
CREATE UNIQUE INDEX [ModGroups_ModGroups_ModGroups_idx_ModGroups_GroupName] ON [ModGroups] ([GroupName] ASC);
CREATE UNIQUE INDEX [Plugins_Plugins_Plugins_idx_Plugins_PluginName] ON [Plugins] ([PluginName] ASC);
COMMIT;
PRAGMA foreign_keys=ON;
