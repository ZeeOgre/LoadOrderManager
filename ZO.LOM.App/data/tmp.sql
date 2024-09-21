PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;

CREATE TABLE [Plugins] (
  [PluginID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginName] TEXT NOT NULL,
  [Description] TEXT NULL,
  [Achievements] INTEGER NOT NULL,
  [DTStamp] TEXT NOT NULL,
  [Version] TEXT NULL,
  [State] INTEGER NULL
);

CREATE TABLE [ModGroups] (
  [GroupID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [Ordinal] INTEGER NULL,
  [GroupName] TEXT NULL,
  [Description] TEXT NULL,
  [ParentID] INTEGER NULL,
  [GroupSetID] INTEGER NULL,
  CONSTRAINT [FK_ModGroups_ParentID] FOREIGN KEY ([ParentID]) REFERENCES [ModGroups] ([GroupID]) ON DELETE SET NULL ON UPDATE NO ACTION
);

CREATE TABLE [LoadOutProfiles] (
  [ProfileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [ProfileName] TEXT NOT NULL,
  [GroupSetID] INTEGER NULL
);

CREATE TABLE [ProfilePlugins] (
  [ProfileID] INTEGER NOT NULL,
  [PluginID] INTEGER NOT NULL,
  CONSTRAINT [PK_ProfilePlugins] PRIMARY KEY ([ProfileID], [PluginID]),
  CONSTRAINT [FK_ProfilePlugins_PluginID] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT [FK_ProfilePlugins_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [LoadOutProfiles] ([ProfileID]) ON DELETE CASCADE ON UPDATE NO ACTION
);

CREATE TABLE [InitializationStatus] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [IsInitialized] INTEGER NOT NULL,
  [InitializationTime] TEXT NOT NULL
);

CREATE TABLE [GroupSets] (
  [GroupSetID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [GroupSetName] TEXT NOT NULL,
  [GroupSetFlags] INTEGER DEFAULT (0) NOT NULL
);

CREATE TABLE [Groups] (
  [GroupID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [ParentID] INTEGER NULL,
  [Ordinal] INTEGER NOT NULL,
  [GroupName] TEXT NOT NULL,
  [Description] TEXT NULL,
  CONSTRAINT [FK_Groups_ParentID] FOREIGN KEY ([ParentID]) REFERENCES [Groups] ([GroupID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE [GroupSetPlugins] (
  [GroupSetID] INTEGER NOT NULL,
  [GroupID] INTEGER NOT NULL,
  [PluginID] INTEGER NOT NULL,
  [Ordinal] INTEGER NOT NULL,
  CONSTRAINT [PK_GroupSetPlugins] PRIMARY KEY ([GroupSetID], [GroupID], [PluginID]),
  CONSTRAINT [FK_GroupSetPlugins_GroupSetID] FOREIGN KEY ([GroupSetID]) REFERENCES [GroupSets] ([GroupSetID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT [FK_GroupSetPlugins_GroupID] FOREIGN KEY ([GroupID]) REFERENCES [Groups] ([GroupID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT [FK_GroupSetPlugins_PluginID] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE [FileInfo] (
  [FileID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginID] INTEGER NULL,
  [Filename] TEXT NOT NULL,
  [RelativePath] TEXT NULL,
  [DTStamp] TEXT NOT NULL,
  [HASH] TEXT NULL,
  [IsArchive] INTEGER NOT NULL,
  CONSTRAINT [FK_FileInfo_PluginID] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE [ExternalIDs] (
  [ExternalID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [PluginID] INTEGER NULL,
  [BethesdaID] TEXT NULL,
  [NexusID] TEXT NULL,
  CONSTRAINT [FK_ExternalIDs_PluginID] FOREIGN KEY ([PluginID]) REFERENCES [Plugins] ([PluginID]) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE [Config] (
  [GameFolder] TEXT NOT NULL,
  [AutoCheckForUpdates] INTEGER NULL
);



-- Insert into GroupSets
INSERT INTO [GroupSets] ([GroupSetID], [GroupSetName], [GroupSetFlags]) VALUES
(1, 'BASELINE_GS', 9),
(2, 'SINGLETON_GS', 12);



-- Insert into ModGroups
INSERT INTO [ModGroups] ([GroupID], [Ordinal], [GroupName], [Description], [ParentID]) VALUES 
(-999, 0, 'CoreGameFiles', 'This is a reserved group for mods that are an integral part of the game and can''t be controlled by the player', NULL),
(-998, 0, 'NeverLoad', 'This is a reserved group for mods which should never be loaded', NULL),
(-997, 0, 'Uncategorized', 'This is a reserved group to temporarily hold uncategorized mods', NULL),
(1, 0, '(Default Root)', 'This is the Default Root group which holds all the other groups', NULL);

-- Insert into Plugins
INSERT INTO [Plugins] ([PluginID], [PluginName], [Description], [Achievements], [DTStamp], [Version], [State]) VALUES 
(1, 'blueprintships-starfield.esm', 'Core game file containing all the ship models (We think!)', 1, '2024-08-20 18:18:57', NULL, NULL),
(2, 'constellation.esm', 'Premium Edition Content', 1, '2024-06-28 00:43:13', NULL, NULL),
(3, 'oldmars.esm', 'Premium Edition - Old Mars Skins', 1, '2024-09-19', NULL, NULL),
(4, 'sfbgs003.esm', 'Tracker''s Alliance update', 1, '2024-08-20 18:18:57', NULL, NULL),
(5, 'sfbgs004.esm', 'REV-8 Vehicle', 1, '2024-08-20 18:19:01', NULL, NULL),
(6, 'sfbgs006.esm', 'Empty Ship Habs and Decorations', 1, '2024-06-28 00:22:40', NULL, NULL),
(7, 'sfbgs007.esm', 'Add "GamePlay Options" Menu', 1, '2024-08-20 18:19:16', NULL, NULL),
(8, 'sfbgs008.esm', 'New Map design (3d maps)', 1, '2024-08-20 18:18:57', NULL, NULL),
(9, 'starfield.esm', 'The core Starfield game', 1, '2024-08-20 18:18:57', NULL, NULL),
(10, 'A1_EMPTY_STUB_XXXXXXXXXX.esm', 'JMPz11''s stub for converting mods between xEdit and Creation Kit, WILL crash your game if you try to load it.', 0, '2024-06-24 19:53:00', NULL, NULL);

-- Insert into LoadOutProfiles
INSERT INTO [LoadOutProfiles] ([ProfileID], [ProfileName], [GroupSetID]) VALUES 
(1, 'Baseline', 1),
(2, '(Default)', 1);

-- Insert into GroupSetPlugins
INSERT INTO [GroupSetPlugins] ([GroupSetID], [GroupID], [PluginID], [Ordinal]) VALUES 
(1, -999, 1, 1),
(1, -999, 2, 2),
(1, -999, 3, 3),
(1, -999, 4, 4),
(1, -999, 5, 5),
(1, -999, 6, 6),
(1, -999, 7, 7),
(1, -999, 8, 8),
(1, -999, 9, 9),
(1, -998, 10, 1),
(2, -999, 1, 1),
(2, -999, 2, 2),
(2, -999, 3, 3),
(2, -999, 4, 4),
(2, -999, 5, 5),
(2, -999, 6, 6),
(2, -999, 7, 7),
(2, -999, 8, 8),
(2, -998, 9, 1),
(2, -998, 10, 1);

-- Insert into ExternalIDs
INSERT INTO [ExternalIDs] ([ExternalID], [PluginID], [BethesdaID], [NexusID]) VALUES 
(1, 9, NULL, '10189');

CREATE UNIQUE INDEX [Plugins_Plugins_idx_Plugins_PluginName] ON [Plugins] ([PluginName] ASC);
CREATE UNIQUE INDEX [ModGroups_ModGroups_idx_GroupName] ON [ModGroups] ([GroupName] ASC,[GroupSetID] ASC);
CREATE UNIQUE INDEX [ModGroups_ModGroups_sqlite_autoindex_ModGroups_1] ON [ModGroups] ([GroupID] ASC,[GroupSetID] ASC);
CREATE UNIQUE INDEX [GroupSetPlugins_GroupSetPlugins_GroupSetPlugins_sqlite_autoindex_GroupSetPlugins_2] ON [GroupSetPlugins] ([PluginID] ASC,[GroupSetID] ASC);
CREATE TRIGGER [fki_ModGroups_ParentID_ModGroups_GroupID] BEFORE Insert ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fku_ModGroups_ParentID_ModGroups_GroupID] BEFORE Update ON [ModGroups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ModGroups violates foreign key constraint FK_ModGroups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM ModGroups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_PluginID_Plugins_PluginID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_0_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Insert ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID] BEFORE Update ON [ProfilePlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table ProfilePlugins violates foreign key constraint FK_ProfilePlugins_1_0') WHERE (SELECT ProfileID FROM LoadOutProfiles WHERE  ProfileID = NEW.ProfileID) IS NULL; END;
CREATE TRIGGER [fki_Groups_ParentID_Groups_GroupID] BEFORE Insert ON [Groups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Groups violates foreign key constraint FK_Groups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM Groups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fku_Groups_ParentID_Groups_GroupID] BEFORE Update ON [Groups] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Groups violates foreign key constraint FK_Groups_0_0') WHERE NEW.ParentID IS NOT NULL AND(SELECT GroupID FROM Groups WHERE  GroupID = NEW.ParentID) IS NULL; END;
CREATE TRIGGER [fki_GroupSetPlugins_GroupSetID_GroupSets_GroupSetID] BEFORE Insert ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_0_0') WHERE (SELECT GroupSetID FROM GroupSets WHERE  GroupSetID = NEW.GroupSetID) IS NULL; END;
CREATE TRIGGER [fku_GroupSetPlugins_GroupSetID_GroupSets_GroupSetID] BEFORE Update ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_0_0') WHERE (SELECT GroupSetID FROM GroupSets WHERE  GroupSetID = NEW.GroupSetID) IS NULL; END;
CREATE TRIGGER [fki_GroupSetPlugins_GroupID_Groups_GroupID] BEFORE Insert ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_1_0') WHERE (SELECT GroupID FROM Groups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fku_GroupSetPlugins_GroupID_Groups_GroupID] BEFORE Update ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_1_0') WHERE (SELECT GroupID FROM Groups WHERE  GroupID = NEW.GroupID) IS NULL; END;
CREATE TRIGGER [fki_GroupSetPlugins_PluginID_Plugins_PluginID] BEFORE Insert ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_2_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
CREATE TRIGGER [fku_GroupSetPlugins_PluginID_Plugins_PluginID] BEFORE Update ON [GroupSetPlugins] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table GroupSetPlugins violates foreign key constraint FK_GroupSetPlugins_2_0') WHERE (SELECT PluginID FROM Plugins WHERE  PluginID = NEW.PluginID) IS NULL; END;
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
    g.GroupName,    
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
PRAGMA foreign_keys=ON;

