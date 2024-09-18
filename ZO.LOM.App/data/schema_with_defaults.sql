-- Script Date: 9/15/2024 11:35 PM  - ErikEJ.SqlCeScripting version 3.5.2.95
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
CREATE TABLE [ModGroups] (
  [GroupID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [Ordinal] bigint NULL
, [GroupName] text NULL
, [Description] text NULL
, [ParentID] bigint NULL
, CONSTRAINT [FK_ModGroups_0_0] FOREIGN KEY ([ParentID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [Plugins] (
  [PluginID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [PluginName] text NOT NULL
, [Description] text NULL
, [Achievements] bigint NOT NULL
, [DTStamp] text NOT NULL
, [Version] text NULL
, [GroupID] bigint NULL
, [GroupOrdinal] bigint NULL
, CONSTRAINT [FK_Plugins_0_0] FOREIGN KEY ([GroupID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [LoadOutProfiles] (
  [ProfileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ProfileName] text NOT NULL
);
CREATE TABLE [ProfilePlugins] (
  [ProfileID] bigint NOT NULL
, [PluginID] bigint NOT NULL
, CONSTRAINT [sqlite_autoindex_ProfilePlugins_1] PRIMARY KEY ([ProfileID],[PluginID])
, CONSTRAINT [FK_ProfilePlugins_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE CASCADE ON UPDATE NO ACTION
, CONSTRAINT [FK_ProfilePlugins_1_0] FOREIGN KEY ([ProfileID]) REFERENCES [LoadOutProfiles] ([ProfileID]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [InitializationStatus] (
  [Id] bigint NOT NULL
, [IsInitialized] bigint NOT NULL
, [InitializationTime] text NOT NULL
, CONSTRAINT [sqlite_autoindex_InitializationStatus_1] PRIMARY KEY ([Id])
);
CREATE TABLE [FileInfo] (
  [FileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [PluginID] bigint NULL
, [Filename] text NOT NULL
, [RelativePath] text NULL
, [DTStamp] datetime NOT NULL
, [HASH] text NULL
, [IsArchive] bigint NOT NULL
, CONSTRAINT [FK_FileInfo_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
CREATE TABLE [ExternalIDs] (
  [ExternalID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [PluginID] bigint NULL
, [BethesdaID] text NULL
, [NexusID] text NULL
, CONSTRAINT [FK_ExternalIDs_0_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
CREATE TABLE [Config] (
  [GameFolder] text NOT NULL
, [AutoCheckForUpdates] bigint NULL
);
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
9,'A1_EMPTY_STUB_XXXXXXXXXX.esm','JMPz11''s stub for converting mods between xEdit and Creation Kit, WILL crash your game if you try to load it.',0,'2024-06-24 19:53:00',NULL,-998,1);
INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName]) VALUES (
1,'(Default Profile)');
INSERT INTO [ExternalIDs] ([ExternalID],[PluginID],[BethesdaID],[NexusID]) VALUES (
1,9,NULL,'10189');
CREATE UNIQUE INDEX [ModGroups_ModGroups_idx_ModGroups_GroupName] ON [ModGroups] ([GroupName] ASC);
CREATE INDEX [ModGroups_ModGroups_idx_ModGroups_GroupID] ON [ModGroups] ([GroupID] ASC);
CREATE UNIQUE INDEX [Plugins_Plugins_idx_Plugins_PluginName] ON [Plugins] ([PluginName] ASC);
CREATE INDEX [ProfilePlugins_idx_ProfilePlugins_ProfileID] ON [ProfilePlugins] ([ProfileID] ASC);
CREATE INDEX [ProfilePlugins_idx_ProfilePlugins_PluginID] ON [ProfilePlugins] ([PluginID] ASC);
CREATE INDEX [FileInfo_idx_FileInfo_PluginID] ON [FileInfo] ([PluginID] ASC);
CREATE UNIQUE INDEX [ExternalIDs_idx_ExternalIDs_BethesdaID] ON [ExternalIDs] ([BethesdaID] ASC);
CREATE UNIQUE INDEX [ExternalIDs_idx_ExternalIDs_NexusID] ON [ExternalIDs] ([NexusID] ASC);
CREATE TRIGGER [fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_FileInfo_PluginID_Plugins_PluginID] BEFORE Insert ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_FileInfo_PluginID_Plugins_PluginID] BEFORE Update ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Insert ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Update ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ModGroups_ParentID_ModGroups_GroupID] BEFORE Insert ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fku_ModGroups_ParentID_ModGroups_GroupID] BEFORE Update ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fki_Plugins_GroupID_ModGroups_GroupID] BEFORE Insert ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fku_Plugins_GroupID_ModGroups_GroupID] BEFORE Update ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fki_ModGroups_ParentID_ModGroups_GroupID] BEFORE Insert ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fku_ModGroups_ParentID_ModGroups_GroupID] BEFORE Update ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fki_Plugins_GroupID_ModGroups_GroupID] BEFORE Insert ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fku_Plugins_GroupID_ModGroups_GroupID] BEFORE Update ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fki_FileInfo_PluginID_Plugins_PluginID] BEFORE Insert ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_FileInfo_PluginID_Plugins_PluginID] BEFORE Update ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Insert ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Update ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE VIEW vwLoadOuts AS  
SELECT  
    l.ProfileID,  
    l.ProfileName,  
    p.PluginID,  
    p.PluginName,  
    p.Description,  
    p.Achievements,  
    p.DTStamp AS TimeStamp,  
    p.Version,  
    e.BethesdaID,  
    e.NexusID,  
    p.GroupID,  
    p.GroupOrdinal  
FROM  
    LoadOutProfiles l  
LEFT JOIN  
    ProfilePlugins pp ON l.ProfileID = pp.ProfileID  
LEFT JOIN  
    Plugins p ON pp.PluginID = p.PluginID  
LEFT JOIN  
    ExternalIDs e ON p.PluginID = e.PluginID  
ORDER BY l.ProfileID, p.GroupID, p.GroupOrdinal;
CREATE VIEW vwModGroups AS  
SELECT  
    g.GroupID,  
    g.Ordinal,  
    g.GroupName,  
    g.Description AS GroupDescription,  
    g.ParentID,  
    p.PluginID,  
    p.PluginName,  
    p.Description AS PluginDescription,  
    p.Achievements,  
    p.DTStamp AS TimeStamp,  
    p.Version,  
    e.BethesdaID,  
    e.NexusID,  
    p.GroupOrdinal  
FROM  
    ModGroups g  
LEFT JOIN  
    Plugins p ON g.GroupID = p.GroupID  
LEFT JOIN  
    ExternalIDs e ON p.PluginID = e.PluginID  
ORDER BY g.ParentID, g.Ordinal, p.GroupOrdinal;
CREATE VIEW vwPluginFiles AS  
SELECT  
    fi.FileID,  
    p.PluginID,  
    p.PluginName,  
    fi.Filename,  
    fi.RelativePath,  
    fi.DTStamp,  
    fi.HASH,  
    fi.IsArchive  
FROM  
    Plugins p  
JOIN  
    FileInfo fi ON p.PluginID = fi.PluginID;
CREATE VIEW vwPlugins AS  
SELECT  
    p.PluginID,  
    p.PluginName,  
    p.Description,  
    p.Achievements,  
    p.DTStamp,  
    p.Version,  
    p.GroupID,  
    p.GroupOrdinal,  
    g.Description AS GroupName,  
    e.BethesdaID,  
    e.NexusID  
FROM  
    Plugins p  
LEFT JOIN  
    ModGroups g ON p.GroupID = g.GroupID  
LEFT JOIN  
    ExternalIDs e ON p.PluginID = e.PluginID;
CREATE VIEW vwPluginGrpUnion AS     
SELECT        
    p.PluginID,        
    p.PluginName,        
    p.Description,        
    p.Achievements,        
    p.DTStamp,        
    p.Version,        
    p.GroupID AS PluginGroupID,        
    p.GroupOrdinal,        
    g.GroupID AS GroupID, 
    g.GroupName AS GroupName,     
    g.Description AS GroupDescription,        
    g.ParentID,        
    g.Ordinal AS GroupGroupOrdinal,       
    pp.ProfileID,  
    e.BethesdaID,  
    e.NexusID  
FROM        
    Plugins p        
LEFT JOIN        
    ModGroups g ON p.GroupID = g.GroupID       
LEFT JOIN       
    ProfilePlugins pp ON p.PluginID = pp.PluginID  
LEFT JOIN  
    ExternalIDs e ON p.PluginID = e.PluginID  
UNION        
SELECT        
    NULL AS PluginID,        
    NULL AS PluginName,        
    NULL AS Description,        
    NULL AS Achievements,        
    NULL AS DTStamp,        
    NULL AS Version,        
    g.GroupID AS PluginGroupID,        
    NULL AS GroupOrdinal,        
    g.GroupID AS GroupID,   
    g.GroupName AS GroupName,     
    g.Description AS GroupDescription,        
    g.ParentID,        
    g.Ordinal AS GroupGroupOrdinal,       
    NULL AS ProfileID,  
    NULL AS BethesdaID,  
    NULL AS NexusID  
FROM        
    ModGroups g;
COMMIT;

