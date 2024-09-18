-- Script Date: 9/15/2024 11:37 PM  - ErikEJ.SqlCeScripting version 3.5.2.95
-- Database information:
-- Database: M:\ZO_Repos\ZO.Applications\ZO.LoadOrderManager\ZO.LOM.App\data\LoadOrderManager.db
-- ServerVersion: 3.40.0
-- DatabaseSize: 96 KB
-- Created: 9/14/2024 5:33 PM

-- User Table information:
-- Number of tables: 8
-- Config: -1 row(s)
-- ExternalIDs: -1 row(s)
-- FileInfo: -1 row(s)
-- InitializationStatus: -1 row(s)
-- LoadOutProfiles: -1 row(s)
-- ModGroups: -1 row(s)
-- Plugins: -1 row(s)
-- ProfilePlugins: -1 row(s)

SELECT 1;
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID]) VALUES (
-999,0,'CoreGameFiles','This is a reserved group for mods that are an integral part of the game and can''t be controlled by the player',NULL);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID]) VALUES (
-998,0,'NeverLoad','This is a reserved group for mods which should never be loaded',NULL);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID]) VALUES (
-997,0,'Uncategorized','This is a reserved group to temporarily hold uncategorized mods',NULL);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID]) VALUES (
1,0,'(Default Group)','This is the Default Root group which holds all the other groups',NULL);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
1,'blueprintships-starfield.esm','Core game file containing all the ship models (We think!)',1,'2024-08-20 18:18:57',NULL,-999,1);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
2,'constellation.esm','Premium Edition Content',1,'2024-06-28 00:43:13',NULL,-999,2);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
3,'sfbgs003.esm','Tracker''s Alliance update',1,'2024-08-20 18:18:57',NULL,-999,3);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
4,'sfbgs004.esm','REV-8 Vehicle',1,'2024-08-20 18:19:01',NULL,-999,4);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
5,'sfbgs006.esm','Empty Ship Habs and Decorations',1,'2024-06-28 00:22:40',NULL,-999,5);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
6,'sfbgs007.esm','Add "GamePlay Options" Menu',1,'2024-08-20 18:19:16',NULL,-999,6);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
7,'sfbgs008.esm','New Map design (3d maps)',1,'2024-08-20 18:18:57',NULL,-999,7);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
8,'starfield.esm','The core Starfield game',1,'2024-08-20 18:18:57',NULL,-999,8);
INSERT INTO [Plugins] ([PluginID],[PluginName],[Description],[Achievements],[DTStamp],[Version],[GroupID],[GroupOrdinal]) VALUES (
9,'a1_empty_stub_xxxxxxxxxx.esm','JMPz11''s stub for converting mods between xEdit and Creation Kit, WILL crash your game if you try to load it.',0,'2024-06-24 19:53:00',NULL,-998,1);
INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName]) VALUES (
1,'(Default Profile)');
INSERT INTO [ExternalIDs] ([ExternalID],[PluginID],[BethesdaID],[NexusID]) VALUES (
1,9,NULL,'10189');
COMMIT;

