-- Script Date: 9/20/2024 9:25 PM  - ErikEJ.SqlCeScripting version 3.5.2.95
-- Database information:
-- Database: M:\ZO_Repos\ZO.Applications\ZO.LoadOrderManager\ZO.LOM.App\data\LoadOrderManager.db
-- ServerVersion: 3.40.0
-- DatabaseSize: 72 KB
-- Created: 9/19/2024 6:42 PM

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
  [GroupID] bigint NOT NULL
, [Ordinal] bigint NULL
, [GroupName] text NULL
, [Description] text NULL
, [ParentID] bigint NULL
, [GroupSetID] bigint NULL
, CONSTRAINT [FK_ModGroups_0_0] FOREIGN KEY ([ParentID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [Plugins] (
  [PluginID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [PluginName] text NOT NULL
, [Description] text NULL
, [Achievements] bigint NOT NULL
, [DTStamp] text NOT NULL
, [Version] text NULL
, [State] bigint NULL
, [GroupID] bigint NULL
, [GroupSetID] bigint NULL
, [GroupOrdinal] bigint NULL
, CONSTRAINT [FK_Plugins_0_0] FOREIGN KEY ([GroupID], [GroupSetID]) REFERENCES [ModGroups] ([GroupID], [GroupSetID]) ON DELETE SET NULL ON UPDATE NO ACTION
);
CREATE TABLE [LoadOutProfiles] (
  [ProfileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ProfileName] text NOT NULL
, [GroupSetID] bigint NULL
);
CREATE TABLE [ProfilePlugins] (
  [ProfileID] bigint NOT NULL
, [PluginID] bigint NOT NULL
, CONSTRAINT [sqlite_autoindex_ProfilePlugins_1] PRIMARY KEY ([ProfileID],[PluginID])
, CONSTRAINT [FK_ProfilePlugins_0_0] FOREIGN KEY ([ProfileID]) REFERENCES [LoadOutProfiles] ([ProfileID]) ON DELETE CASCADE ON UPDATE NO ACTION
, CONSTRAINT [FK_ProfilePlugins_1_0] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [InitializationStatus] (
  [Id] bigint NOT NULL
, [IsInitialized] bigint NOT NULL
, [InitializationTime] text NOT NULL
, CONSTRAINT [sqlite_autoindex_InitializationStatus_1] PRIMARY KEY ([Id])
);
CREATE TABLE [GroupSet] (
  [GroupSetID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [GroupSetName] text NOT NULL
, [GroupSetFlags] int DEFAULT (0) NOT NULL
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
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-999,0,'CoreGameFiles','This is a reserved group for mods that are an integral part of the game and can''t be controlled by the player',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-998,0,'NeverLoad','This is a reserved group for mods which should never be loaded',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-997,0,'Uncategorized','This is a reserved group to temporarily hold uncategorized mods',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
1,0,'(Default Group)','This is the Default Root group which holds all the other groups',NULL,1);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-999,0,'CoreGameFiles','This is a reserved group for mods that are an integral part of the game and can''t be controlled by the player',NULL,2);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-998,0,'NeverLoad','This is a reserved group for mods which should never be loaded',NULL,2);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
-997,0,'Uncategorized','This is a reserved group to temporarily hold uncategorized mods',NULL,2);
INSERT INTO [ModGroups] ([GroupID],[Ordinal],[GroupName],[Description],[ParentID],[GroupSetID]) VALUES (
1,0,'(Default Group)','This is the Default Root group which holds all the other groups',NULL,2);
INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName],[GroupSetID]) VALUES (
1,'BASELINE_PROFILE',1);
INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName],[GroupSetID]) VALUES (
2,'(Default Profile)',2);
INSERT INTO [GroupSet] ([GroupSetID],[GroupSetName],[GroupSetFlags]) VALUES (
1,'BASELINE_GRPSET',9);
INSERT INTO [GroupSet] ([GroupSetID],[GroupSetName],[GroupSetFlags]) VALUES (
2,'Singleton_GRPSET',24);
INSERT INTO [ExternalIDs] ([ExternalID],[PluginID],[BethesdaID],[NexusID]) VALUES (
1,9,NULL,'10189');
CREATE UNIQUE INDEX [ModGroups_ModGroups_ModGroups_ModGroups_idx_GroupName] ON [ModGroups] ([GroupName] ASC,[GroupSetID] ASC);
CREATE UNIQUE INDEX [ModGroups_ModGroups_ModGroups_sqlite_autoindex_ModGroups_1] ON [ModGroups] ([GroupID] ASC,[GroupSetID] ASC);
CREATE UNIQUE INDEX [Plugins_Plugins_idx_Plugins_PluginName] ON [Plugins] ([PluginName] ASC,[GroupSetID] ASC);
CREATE TRIGGER [fki_ModGroups_ParentID_ModGroups_GroupID] BEFORE Insert ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fku_ModGroups_ParentID_ModGroups_GroupID] BEFORE Update ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fki_Plugins_GroupID_ModGroups_GroupID] BEFORE Insert ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND NEW.GroupSetID IS NOT NULL AND(SELECT GroupID, GroupSetID FROM ModGroups WHERE  GroupID = NEW.GroupID AND GroupSetID = NEW.GroupSetID) IS NULL; END;
CREATE TRIGGER [fku_Plugins_GroupID_ModGroups_GroupID] BEFORE Update ON [Plugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Plugins violates foreign key constraint FK_Plugins_0_0') WHERE NEW.GroupID IS NOT NULL AND NEW.GroupSetID IS NOT NULL AND(SELECT GroupID, GroupSetID FROM ModGroups WHERE  GroupID = NEW.GroupID AND GroupSetID = NEW.GroupSetID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_FileInfo_PluginID_Plugins_PluginID] BEFORE Insert ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_FileInfo_PluginID_Plugins_PluginID] BEFORE Update ON [FileInfo] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table FileInfo violates foreign key constraint FK_FileInfo_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Insert ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ExternalIDs_PluginID_Plugins_PluginID] BEFORE Update ON [ExternalIDs] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ExternalIDs violates foreign key constraint FK_ExternalIDs_0_0') WHERE NEW.PluginID IS NOT NULL AND(SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
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
    p.State,  
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
    p.State,   
    e.BethesdaID,     
    e.NexusID,     
    p.GroupID,     
    p.GroupOrdinal,  
    l.GroupSetID  -- Include GroupSetID from LoadOutProfiles  
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
	g.GroupSetID,      
    p.PluginID,     
    p.PluginName,     
    p.Description AS PluginDescription,     
    p.Achievements,     
    p.DTStamp AS TimeStamp,     
    p.Version,     
    p.State,   
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
CREATE VIEW vwPluginGrpUnion AS          
SELECT  
    COALESCE(p.PluginID, NULL) AS PluginID,  
    COALESCE(p.PluginName, NULL) AS PluginName,  
    COALESCE(p.Description, NULL) AS Description,  
    COALESCE(p.Achievements, NULL) AS Achievements,  
    COALESCE(p.DTStamp, NULL) AS DTStamp,  
    COALESCE(p.Version, NULL) AS Version,  
    COALESCE(p.State, NULL) AS State,  
    COALESCE(p.GroupID, g.GroupID) AS PluginGroupID,  
    COALESCE(p.GroupOrdinal, NULL) AS GroupOrdinal,  
    g.GroupID AS GroupID,  
    g.GroupName AS GroupName,  
    g.Description AS GroupDescription,  
    g.ParentID,  
    g.Ordinal AS GroupGroupOrdinal,  
    COALESCE(pp.ProfileID, NULL) AS ProfileID,  
    COALESCE(e.BethesdaID, NULL) AS BethesdaID,  
    COALESCE(e.NexusID, NULL) AS NexusID,  
    l.GroupSetID,  
    gs.GroupSetName  
FROM  
    Plugins p  
FULL OUTER JOIN  
    ModGroups g ON p.GroupID = g.GroupID  
LEFT JOIN  
    ProfilePlugins pp ON p.PluginID = pp.PluginID  
LEFT JOIN  
    ExternalIDs e ON p.PluginID = e.PluginID  
LEFT JOIN  
    LoadOutProfiles l ON pp.ProfileID = l.ProfileID OR g.GroupSetID = l.GroupSetID  
LEFT JOIN  
    GroupSet gs ON l.GroupSetID = gs.GroupSetID;
COMMIT;

