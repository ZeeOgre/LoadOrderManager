--
-- File generated with SQLiteStudio v3.4.4 on Sat Sep 21 20:40:30 2024
--
-- Text encoding used: System
--
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

-- Table: Config
DROP TABLE IF EXISTS Config;

CREATE TABLE IF NOT EXISTS Config (
    GameFolder          TEXT    NOT NULL,
    AutoCheckForUpdates longEGER
);


-- Table: ExternalIDs
DROP TABLE IF EXISTS ExternalIDs;

CREATE TABLE IF NOT EXISTS ExternalIDs (
    ExternalID longEGER PRIMARY KEY AUTOINCREMENT
                       NOT NULL,
    PluginID   longEGER,
    BethesdaID TEXT,
    NexusID    TEXT,
    CONSTRAlong FK_ExternalIDs_PluginID FOREIGN KEY (
        PluginID
    )
    REFERENCES Plugins (PluginID) ON DELETE NO ACTION
                                  ON UPDATE NO ACTION
);

INSERT INTO ExternalIDs (ExternalID, PluginID, BethesdaID, NexusID) VALUES (1, 10, NULL, '10189');

-- Table: FileInfo
DROP TABLE IF EXISTS FileInfo;

CREATE TABLE IF NOT EXISTS FileInfo (
    FileID       longEGER PRIMARY KEY AUTOINCREMENT
                         NOT NULL,
    PluginID     longEGER,
    Filename     TEXT    NOT NULL,
    RelativePath TEXT,
    DTStamp      TEXT    NOT NULL,
    HASH         TEXT,
    Flags    longEGER NOT NULL,
    CONSTRAlong FK_FileInfo_PluginID FOREIGN KEY (
        PluginID
    )
    REFERENCES Plugins (PluginID) ON DELETE NO ACTION
                                  ON UPDATE NO ACTION
);


-- Table: GroupSetGroups
DROP TABLE IF EXISTS GroupSetGroups;

CREATE TABLE IF NOT EXISTS GroupSetGroups (
    GroupSetGroupID longEGER PRIMARY KEY AUTOINCREMENT,
    GroupID         longEGER,
    GroupSetID      longEGER,
    ParentID        longEGER,
    Ordinal         longEGER,
    FOREIGN KEY (
        GroupID
    )
    REFERENCES ModGroups (GroupID),
    FOREIGN KEY (
        ParentID
    )
    REFERENCES ModGroups (GroupID),
    UNIQUE (
        GroupID,
        GroupSetID
    )
);

INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (1, -999, 1, 1, 9999);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (2, -999, 2, 1, 9999);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (3, -998, 1, 1, 9998);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (4, -998, 2, 1, 9998);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (5, -997, 1, 1, 9997);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (6, -997, 2, 1, 9997);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (7, 1, 1, 0, 0);
INSERT INTO GroupSetGroups (GroupSetGroupID, GroupID, GroupSetID, ParentID, Ordinal) VALUES (8, 1, 2, 0, 0);

-- Table: GroupSetPlugins
DROP TABLE IF EXISTS GroupSetPlugins;

CREATE TABLE IF NOT EXISTS GroupSetPlugins (
    GroupSetID longEGER NOT NULL,
    GroupID    longEGER NOT NULL,
    PluginID   longEGER NOT NULL,
    Ordinal    longEGER NOT NULL,
    CONSTRAlong PK_GroupSetPlugins PRIMARY KEY (
        GroupSetID,
        GroupID,
        PluginID
    ),
    CONSTRAlong FK_GroupSetPlugins_GroupSetID FOREIGN KEY (
        GroupSetID
    )
    REFERENCES GroupSets (GroupSetID) ON DELETE NO ACTION
                                      ON UPDATE NO ACTION,
    CONSTRAlong FK_GroupSetPlugins_GroupID FOREIGN KEY (
        GroupID
    )
    REFERENCES ModGroups (GroupID) ON DELETE NO ACTION
                                   ON UPDATE NO ACTION,
    CONSTRAlong FK_GroupSetPlugins_PluginID FOREIGN KEY (
        PluginID
    )
    REFERENCES Plugins (PluginID) ON DELETE NO ACTION
                                  ON UPDATE NO ACTION
);

INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 1, 1);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 2, 2);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 3, 3);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 4, 4);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 5, 5);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 6, 6);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 7, 7);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 8, 8);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -999, 9, 9);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (1, -998, 10, 1);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 1, 1);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 2, 2);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 3, 3);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 4, 4);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 5, 5);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 6, 6);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 7, 7);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 8, 8);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -999, 9, 9);
INSERT INTO GroupSetPlugins (GroupSetID, GroupID, PluginID, Ordinal) VALUES (2, -998, 10, 1);

-- Table: GroupSets
DROP TABLE IF EXISTS GroupSets;

CREATE TABLE IF NOT EXISTS GroupSets (
    GroupSetID    longEGER PRIMARY KEY AUTOINCREMENT,
    GroupSetName  TEXT,
    GroupSetFlags longEGER
);

INSERT INTO GroupSets (GroupSetID, GroupSetName, GroupSetFlags) VALUES (1, 'BASELINE_GS', 9);
INSERT INTO GroupSets (GroupSetID, GroupSetName, GroupSetFlags) VALUES (2, 'SINGLETON_GS', 12);

-- Table: InitializationStatus
DROP TABLE IF EXISTS InitializationStatus;

CREATE TABLE IF NOT EXISTS InitializationStatus (
    Id                 longEGER PRIMARY KEY AUTOINCREMENT
                               NOT NULL,
    IsInitialized      longEGER NOT NULL,
    InitializationTime TEXT    NOT NULL
);


-- Table: LoadOutProfiles
DROP TABLE IF EXISTS LoadOutProfiles;

CREATE TABLE IF NOT EXISTS LoadOutProfiles (
    ProfileID   longEGER PRIMARY KEY AUTOINCREMENT
                        NOT NULL,
    ProfileName TEXT    NOT NULL,
    GroupSetID  longEGER
);

INSERT INTO LoadOutProfiles (ProfileID, ProfileName, GroupSetID) VALUES (1, 'Baseline', 1);
INSERT INTO LoadOutProfiles (ProfileID, ProfileName, GroupSetID) VALUES (2, '(Default)', 1);

-- Table: ModGroups
DROP TABLE IF EXISTS ModGroups;

CREATE TABLE IF NOT EXISTS ModGroups (
    GroupID     longEGER PRIMARY KEY AUTOINCREMENT,
    GroupName   TEXT,
    Description TEXT
);

INSERT INTO ModGroups (GroupID, GroupName, Description) VALUES (-999, 'CoreGameFiles', 'This is a reserved group for mods that are an longegral part of the game and can''t be controlled by the player');
INSERT INTO ModGroups (GroupID, GroupName, Description) VALUES (-998, 'NeverLoad', 'This is a reserved group for mods which should never be loaded');
INSERT INTO ModGroups (GroupID, GroupName, Description) VALUES (-997, 'Uncategorized', 'This is a reserved group to temporarily hold uncategorized mods');
INSERT INTO ModGroups (GroupID, GroupName, Description) VALUES (1, '(Default Root)', 'This is the Default Root group which holds all the other groups');

-- Table: Plugins
DROP TABLE IF EXISTS Plugins;

CREATE TABLE IF NOT EXISTS Plugins (
    PluginID     longEGER PRIMARY KEY AUTOINCREMENT
                         NOT NULL,
    PluginName   TEXT    NOT NULL,
    Description  TEXT,
    Achievements longEGER NOT NULL,
    DTStamp      TEXT    NOT NULL,
    Version      TEXT,
    State        longEGER
);

INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (1, 'blueprlongships-starfield.esm', 'Core game file containing all the ship models (We think!)', 1, '2024-08-20 18:18:57', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (2, 'constellation.esm', 'Premium Edition Content', 1, '2024-06-28 00:43:13', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (3, 'oldmars.esm', 'Premium Edition - Old Mars Skins', 1, '2024-09-19', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (4, 'sfbgs003.esm', 'Tracker''s Alliance update', 1, '2024-08-20 18:18:57', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (5, 'sfbgs004.esm', 'REV-8 Vehicle', 1, '2024-08-20 18:19:01', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (6, 'sfbgs006.esm', 'Empty Ship Habs and Decorations', 1, '2024-06-28 00:22:40', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (7, 'sfbgs007.esm', 'Add "GamePlay Options" Menu', 1, '2024-08-20 18:19:16', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (8, 'sfbgs008.esm', 'New Map design (3d maps)', 1, '2024-08-20 18:18:57', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (9, 'starfield.esm', 'The core Starfield game', 1, '2024-08-20 18:18:57', NULL, 1);
INSERT INTO Plugins (PluginID, PluginName, Description, Achievements, DTStamp, Version, State) VALUES (10, 'A1_EMPTY_STUB_XXXXXXXXXX.esm', 'JMPz11''s stub for converting mods between xEdit and Creation Kit, WILL crash your game if you try to load it.', 0, '2024-06-24 19:53:00', NULL, 4);

-- Table: ProfilePlugins
DROP TABLE IF EXISTS ProfilePlugins;

CREATE TABLE IF NOT EXISTS ProfilePlugins (
    ProfileID longEGER NOT NULL,
    PluginID  longEGER NOT NULL,
    CONSTRAlong PK_ProfilePlugins PRIMARY KEY (
        ProfileID,
        PluginID
    ),
    CONSTRAlong FK_ProfilePlugins_PluginID FOREIGN KEY (
        PluginID
    )
    REFERENCES Plugins (PluginID) ON DELETE CASCADE
                                  ON UPDATE NO ACTION,
    CONSTRAlong FK_ProfilePlugins_ProfileID FOREIGN KEY (
        ProfileID
    )
    REFERENCES LoadOutProfiles (ProfileID) ON DELETE CASCADE
                                           ON UPDATE NO ACTION
);

INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 1);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 1);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 2);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 2);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 3);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 3);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 4);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 4);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 5);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 5);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 6);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 6);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 7);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 7);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 8);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 8);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (1, 9);
INSERT INTO ProfilePlugins (ProfileID, PluginID) VALUES (2, 9);

-- Index: Plugins_Plugins_idx_Plugins_PluginName
DROP INDEX IF EXISTS Plugins_Plugins_idx_Plugins_PluginName;

CREATE UNIQUE INDEX IF NOT EXISTS Plugins_Plugins_idx_Plugins_PluginName ON Plugins (
    PluginName ASC
);


-- Trigger: fki_ExternalIDs_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fki_ExternalIDs_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fki_ExternalIDs_PluginID_Plugins_PluginID
                      BEFORE INSERT
                          ON ExternalIDs
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Insert on table ExternalIDs violates foreign key constralong FK_ExternalIDs_0_0") 
     WHERE NEW.PluginID IS NOT NULL AND 
           (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fki_FileInfo_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fki_FileInfo_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fki_FileInfo_PluginID_Plugins_PluginID
                      BEFORE INSERT
                          ON FileInfo
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Insert on table FileInfo violates foreign key constralong FK_FileInfo_0_0") 
     WHERE NEW.PluginID IS NOT NULL AND 
           (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fki_ProfilePlugins_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fki_ProfilePlugins_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fki_ProfilePlugins_PluginID_Plugins_PluginID
                      BEFORE INSERT
                          ON ProfilePlugins
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Insert on table ProfilePlugins violates foreign key constralong FK_ProfilePlugins_0_0") 
     WHERE (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID
DROP TRIGGER IF EXISTS fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID;
CREATE TRIGGER IF NOT EXISTS fki_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID
                      BEFORE INSERT
                          ON ProfilePlugins
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Insert on table ProfilePlugins violates foreign key constralong FK_ProfilePlugins_1_0") 
     WHERE (
               SELECT ProfileID
                 FROM LoadOutProfiles
                WHERE ProfileID = NEW.ProfileID
           )
           IS NULL;
END;


-- Trigger: fku_ExternalIDs_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fku_ExternalIDs_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fku_ExternalIDs_PluginID_Plugins_PluginID
                      BEFORE UPDATE
                          ON ExternalIDs
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Update on table ExternalIDs violates foreign key constralong FK_ExternalIDs_0_0") 
     WHERE NEW.PluginID IS NOT NULL AND 
           (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fku_FileInfo_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fku_FileInfo_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fku_FileInfo_PluginID_Plugins_PluginID
                      BEFORE UPDATE
                          ON FileInfo
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Update on table FileInfo violates foreign key constralong FK_FileInfo_0_0") 
     WHERE NEW.PluginID IS NOT NULL AND 
           (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fku_ProfilePlugins_PluginID_Plugins_PluginID
DROP TRIGGER IF EXISTS fku_ProfilePlugins_PluginID_Plugins_PluginID;
CREATE TRIGGER IF NOT EXISTS fku_ProfilePlugins_PluginID_Plugins_PluginID
                      BEFORE UPDATE
                          ON ProfilePlugins
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Update on table ProfilePlugins violates foreign key constralong FK_ProfilePlugins_0_0") 
     WHERE (
               SELECT PluginID
                 FROM Plugins
                WHERE PluginID = NEW.PluginID
           )
           IS NULL;
END;


-- Trigger: fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID
DROP TRIGGER IF EXISTS fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID;
CREATE TRIGGER IF NOT EXISTS fku_ProfilePlugins_ProfileID_LoadOutProfiles_ProfileID
                      BEFORE UPDATE
                          ON ProfilePlugins
                    FOR EACH ROW
BEGIN
    SELECT RAISE(ROLLBACK, "Update on table ProfilePlugins violates foreign key constralong FK_ProfilePlugins_1_0") 
     WHERE (
               SELECT ProfileID
                 FROM LoadOutProfiles
                WHERE ProfileID = NEW.ProfileID
           )
           IS NULL;
END;


-- Trigger: trgInsteadOfInsert_vwLoadOuts
DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwLoadOuts;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfInsert_vwLoadOuts
                  INSTEAD OF INSERT
                          ON vwLoadOuts
                    FOR EACH ROW
BEGIN-- Insert INTO LoadOutProfiles table
    INSERT INTO LoadOutProfiles (
                                    ProfileID,
                                    ProfileName,
                                    GroupSetID
                                )
                                VALUES (
                                    NEW.ProfileID,
                                    NEW.ProfileName,
                                    NEW.GroupSetID
                                )
                                ON CONFLICT (
                                    ProfileID
                                )
                                DO UPDATE SET ProfileName = excluded.ProfileName,
                                GroupSetID = excluded.GroupSetID;-- Insert INTO Plugins table
    INSERT INTO Plugins (
                            PluginID,
                            PluginName,
                            Description,
                            Achievements,
                            DTStamp,
                            Version,
                            State
                        )
                        VALUES (
                            NEW.PluginID,
                            NEW.PluginName,
                            NEW.Description,
                            NEW.Achievements,
                            NEW.TimeStamp,
                            NEW.Version,
                            NEW.State
                        )
                        ON CONFLICT (
                            PluginID
                        )
                        DO UPDATE SET PluginName = excluded.PluginName,
                        Description = excluded.Description,
                        Achievements = excluded.Achievements,
                        DTStamp = excluded.DTStamp,
                        Version = excluded.Version,
                        State = excluded.State;-- Insert INTO ProfilePlugins table
    INSERT INTO ProfilePlugins (
                                   ProfileID,
                                   PluginID
                               )
                               VALUES (
                                   NEW.ProfileID,
                                   NEW.PluginID
                               )
                               ON CONFLICT (
                                   ProfileID,
                                   PluginID
                               )
                               DO NOTHING;-- Insert INTO GroupSetPlugins table
    INSERT INTO GroupSetPlugins (
                                    GroupSetID,
                                    GroupID,
                                    PluginID,
                                    Ordinal
                                )
                                VALUES (
                                    NEW.GroupSetID,
                                    NEW.GroupID,
                                    NEW.PluginID,
                                    NEW.GroupOrdinal
                                )
                                ON CONFLICT (
                                    GroupSetID,
                                    GroupID,
                                    PluginID
                                )
                                DO UPDATE SET Ordinal = excluded.Ordinal;-- Insert INTO ExternalIDs table
    INSERT INTO ExternalIDs (
                                PluginID,
                                BethesdaID,
                                NexusID
                            )
                            VALUES (
                                NEW.PluginID,
                                NEW.BethesdaID,
                                NEW.NexusID
                            )
                            ON CONFLICT (
                                PluginID
                            )
                            DO UPDATE SET BethesdaID = excluded.BethesdaID,
                            NexusID = excluded.NexusID;
END;


-- Trigger: trgInsteadOfInsert_vwModGroups
DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwModGroups;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfInsert_vwModGroups
                  INSTEAD OF INSERT
                          ON vwModGroups
                    FOR EACH ROW
BEGIN-- Insert INTO ModGroups table
    INSERT INTO ModGroups (
                              GroupID,
                              Ordinal,
                              GroupName,
                              Description,
                              ParentID
                          )
                          VALUES (
                              NEW.GroupID,
                              NEW.Ordinal,
                              NEW.GroupName,
                              NEW.GroupDescription,
                              NEW.ParentID
                          )
                          ON CONFLICT (
                              GroupID
                          )
                          DO UPDATE SET Ordinal = COALESCE(excluded.Ordinal, ModGroups.Ordinal),
                          GroupName = COALESCE(excluded.GroupName, ModGroups.GroupName),
                          Description = COALESCE(excluded.Description, ModGroups.Description),
                          ParentID = COALESCE(excluded.ParentID, ModGroups.ParentID);-- Insert INTO GroupSetPlugins table
    INSERT INTO GroupSetPlugins (
                                    GroupSetID,
                                    GroupID,
                                    PluginID,
                                    Ordinal
                                )
                                VALUES (
                                    NEW.GroupSetID,
                                    NEW.GroupID,
                                    NEW.PluginID,
                                    NEW.GroupOrdinal
                                )
                                ON CONFLICT (
                                    GroupID,
                                    PluginID
                                )
                                DO UPDATE SET GroupSetID = COALESCE(excluded.GroupSetID, GroupSetPlugins.GroupSetID),
                                Ordinal = COALESCE(excluded.Ordinal, GroupSetPlugins.Ordinal);-- Insert INTO Plugins table
    INSERT INTO Plugins (
                            PluginID,
                            PluginName,
                            Description,
                            Achievements,
                            DTStamp,
                            Version,
                            State
                        )
                        VALUES (
                            NEW.PluginID,
                            NEW.PluginName,
                            NEW.PluginDescription,
                            NEW.Achievements,
                            NEW.TimeStamp,
                            NEW.Version,
                            NEW.State
                        )
                        ON CONFLICT (
                            PluginID
                        )
                        DO UPDATE SET PluginName = COALESCE(excluded.PluginName, Plugins.PluginName),
                        Description = COALESCE(excluded.Description, Plugins.Description),
                        Achievements = COALESCE(excluded.Achievements, Plugins.Achievements),
                        DTStamp = COALESCE(excluded.DTStamp, Plugins.DTStamp),
                        Version = COALESCE(excluded.Version, Plugins.Version),
                        State = COALESCE(excluded.State, Plugins.State);-- Insert INTO ExternalIDs table
    INSERT INTO ExternalIDs (
                                PluginID,
                                BethesdaID,
                                NexusID
                            )
                            VALUES (
                                NEW.PluginID,
                                NEW.BethesdaID,
                                NEW.NexusID
                            )
                            ON CONFLICT (
                                PluginID
                            )
                            DO UPDATE SET BethesdaID = COALESCE(excluded.BethesdaID, ExternalIDs.BethesdaID),
                            NexusID = COALESCE(excluded.NexusID, ExternalIDs.NexusID);
END;


-- Trigger: trgInsteadOfInsert_vwPluginFiles
DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwPluginFiles;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfInsert_vwPluginFiles
                  INSTEAD OF INSERT
                          ON vwPluginFiles
                    FOR EACH ROW
BEGIN-- Insert INTO Plugins table
    INSERT INTO Plugins (
                            PluginID,
                            PluginName
                        )
                        VALUES (
                            NEW.PluginID,
                            NEW.PluginName
                        )
                        ON CONFLICT (
                            PluginID
                        )
                        DO UPDATE SET PluginName = COALESCE(excluded.PluginName, Plugins.PluginName);-- Insert INTO FileInfo table
    INSERT INTO FileInfo (
                             FileID,
                             PluginID,
                             Filename,
                             RelativePath,
                             DTStamp,
                             HASH,
                             Flags
                         )
                         VALUES (
                             NEW.FileID,
                             NEW.PluginID,
                             NEW.Filename,
                             NEW.RelativePath,
                             NEW.DTStamp,
                             NEW.HASH,
                             NEW.Flags
                         )
                         ON CONFLICT (
                             FileID
                         )
                         DO UPDATE SET PluginID = COALESCE(excluded.PluginID, FileInfo.PluginID),
                         Filename = COALESCE(excluded.Filename, FileInfo.Filename),
                         RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath),
                         DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp),
                         HASH = COALESCE(excluded.HASH, FileInfo.HASH),
                         Flags = COALESCE(excluded.Flags, FileInfo.Flags);
END;


-- Trigger: trgInsteadOfInsert_vwPlugins
DROP TRIGGER IF EXISTS trgInsteadOfInsert_vwPlugins;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfInsert_vwPlugins
                  INSTEAD OF INSERT
                          ON vwPlugins
                    FOR EACH ROW
BEGIN-- Insert INTO Plugins table
    INSERT INTO Plugins (
                            PluginID,
                            PluginName,
                            Description,
                            Achievements,
                            DTStamp,
                            Version,
                            BethesdaID,
                            NexusID,
                            GroupID,
                            GroupOrdinal,
                            GroupSetID
                        )
                        VALUES (
                            NEW.PluginID,
                            NEW.PluginName,
                            NEW.Description,
                            NEW.Achievements,
                            NEW.DTStamp,
                            NEW.Version,
                            NEW.BethesdaID,
                            NEW.NexusID,
                            NEW.GroupID,
                            NEW.GroupOrdinal,
                            NEW.GroupSetID
                        )
                        ON CONFLICT (
                            PluginID
                        )
                        DO UPDATE SET PluginName = COALESCE(excluded.PluginName, Plugins.PluginName),
                        Description = COALESCE(excluded.Description, Plugins.Description),
                        Achievements = COALESCE(excluded.Achievements, Plugins.Achievements),
                        DTStamp = COALESCE(excluded.DTStamp, Plugins.DTStamp),
                        Version = COALESCE(excluded.Version, Plugins.Version),
                        BethesdaID = COALESCE(excluded.BethesdaID, Plugins.BethesdaID),
                        NexusID = COALESCE(excluded.NexusID, Plugins.NexusID),
                        GroupID = COALESCE(excluded.GroupID, Plugins.GroupID),
                        GroupOrdinal = COALESCE(excluded.GroupOrdinal, Plugins.GroupOrdinal),
                        GroupSetID = COALESCE(excluded.GroupSetID, Plugins.GroupSetID);
END;


-- Trigger: trgInsteadOfUpdate_vwLoadOuts
DROP TRIGGER IF EXISTS trgInsteadOfUpdate_vwLoadOuts;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfUpdate_vwLoadOuts
                  INSTEAD OF UPDATE
                          ON vwLoadOuts
                    FOR EACH ROW
BEGIN-- Update LoadOutProfiles table
    UPDATE LoadOutProfiles
       SET ProfileName = NEW.ProfileName,
           GroupSetID = NEW.GroupSetID
     WHERE ProfileID = OLD.ProfileID;-- Update Plugins table
    UPDATE Plugins
       SET PluginName = NEW.PluginName,
           Description = NEW.Description,
           Achievements = NEW.Achievements,
           DTStamp = NEW.TimeStamp,
           Version = NEW.Version,
           State = NEW.State
     WHERE PluginID = OLD.PluginID;-- Update ProfilePlugins table
    UPDATE ProfilePlugins
       SET PluginID = NEW.PluginID
     WHERE ProfileID = OLD.ProfileID AND 
           PluginID = OLD.PluginID;-- Update GroupSetPlugins table
    UPDATE GroupSetPlugins
       SET GroupID = NEW.GroupID,
           Ordinal = NEW.GroupOrdinal
     WHERE GroupSetID = OLD.GroupSetID AND 
           PluginID = OLD.PluginID;-- Update ExternalIDs table
    UPDATE ExternalIDs
       SET BethesdaID = NEW.BethesdaID,
           NexusID = NEW.NexusID
     WHERE PluginID = OLD.PluginID;
END;


-- Trigger: trgInsteadOfUpdate_vwModGroups
DROP TRIGGER IF EXISTS trgInsteadOfUpdate_vwModGroups;
CREATE TRIGGER IF NOT EXISTS trgInsteadOfUpdate_vwModGroups
                  INSTEAD OF UPDATE
                          ON vwModGroups
                    FOR EACH ROW
BEGIN-- Update ModGroups table
    UPDATE ModGroups
       SET Ordinal = NEW.Ordinal,
           GroupName = NEW.GroupName,
           Description = NEW.GroupDescription,
           ParentID = NEW.ParentID
     WHERE GroupID = OLD.GroupID;-- Update GroupSetPlugins table
    UPDATE GroupSetPlugins
       SET GroupSetID = NEW.GroupSetID,
           Ordinal = NEW.GroupOrdinal
     WHERE GroupID = OLD.GroupID AND 
           PluginID = OLD.PluginID;-- Update Plugins table
    UPDATE Plugins
       SET PluginName = NEW.PluginName,
           Description = NEW.PluginDescription,
           Achievements = NEW.Achievements,
           DTStamp = NEW.TimeStamp,
           Version = NEW.Version,
           State = NEW.State
     WHERE PluginID = OLD.PluginID;-- Update ExternalIDs table
    UPDATE ExternalIDs
       SET BethesdaID = NEW.BethesdaID,
           NexusID = NEW.NexusID
     WHERE PluginID = OLD.PluginID;
END;


-- View: vwLoadOuts
DROP VIEW IF EXISTS vwLoadOuts;
CREATE VIEW IF NOT EXISTS vwLoadOuts AS
    SELECT l.ProfileID,
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
           gsp.GroupID,
           gsp.Ordinal AS GroupOrdinal,
           l.GroupSetID,
           CASE WHEN EXISTS (
                   SELECT 1
                     FROM ProfilePlugins pp
                    WHERE pp.ProfileID = l.ProfileID AND 
                          pp.PluginID = p.PluginID
               )
           THEN 1 ELSE 0 END AS IsEnabled
      FROM LoadOutProfiles l
           LEFT JOIN
           ProfilePlugins pp ON l.ProfileID = pp.ProfileID
           LEFT JOIN
           Plugins p ON pp.PluginID = p.PluginID
           LEFT JOIN
           GroupSetPlugins gsp ON p.PluginID = gsp.PluginID AND 
                                  l.GroupSetID = gsp.GroupSetID
           LEFT JOIN
           ModGroups g ON gsp.GroupID = g.GroupID
           LEFT JOIN
           ExternalIDs e ON p.PluginID = e.PluginID
     ORDER BY l.ProfileID,
              gsp.GroupID,
              gsp.Ordinal;


-- View: vwModGroups
DROP VIEW IF EXISTS vwModGroups;
CREATE VIEW IF NOT EXISTS vwModGroups AS
    SELECT g.GroupID,
           gsg.Ordinal,
           g.GroupName,
           g.Description AS GroupDescription,
           gsg.ParentID,
           gsg.GroupSetID,
           p.PluginID,
           p.PluginName,
           p.Description AS PluginDescription,
           p.Achievements,
           p.DTStamp AS TimeStamp,
           p.Version,
           p.State,
           e.BethesdaID,
           e.NexusID,
           gsp.Ordinal AS GroupOrdinal
      FROM ModGroups g
           LEFT JOIN
           GroupSetGroups gsg ON g.GroupID = gsg.GroupID
           LEFT JOIN
           GroupSetPlugins gsp ON gsg.GroupID = gsp.GroupID AND 
                                  gsg.GroupSetID = gsp.GroupSetID
           LEFT JOIN
           Plugins p ON gsp.PluginID = p.PluginID
           LEFT JOIN
           ExternalIDs e ON p.PluginID = e.PluginID
     ORDER BY gsg.GroupSetID,-- Group by GroupSetID
              gsg.ParentID,-- Sort by ParentID
              gsg.Ordinal,-- Sort by Ordinal within ParentID
              gsp.Ordinal;


-- View: vwPluginFiles
DROP VIEW IF EXISTS vwPluginFiles;
CREATE VIEW IF NOT EXISTS vwPluginFiles AS
    SELECT fi.FileID,
           p.PluginID,
           p.PluginName,
           fi.Filename,
           fi.RelativePath,
           fi.DTStamp,
           fi.HASH,
           fi.Flags
      FROM Plugins p
           JOIN
           FileInfo fi ON p.PluginID = fi.PluginID;


-- View: vwPluginGrpUnion
DROP VIEW IF EXISTS vwPluginGrpUnion;
CREATE VIEW IF NOT EXISTS vwPluginGrpUnion AS
    SELECT COALESCE(p.PluginID, NULL) AS PluginID,
           COALESCE(p.PluginName, NULL) AS PluginName,
           COALESCE(p.Description, NULL) AS Description,
           COALESCE(p.Achievements, NULL) AS Achievements,
           COALESCE(p.DTStamp, NULL) AS DTStamp,
           COALESCE(p.Version, NULL) AS Version,
           COALESCE(p.State, NULL) AS State,
           gsg.GroupID AS GroupID,
           g.GroupName AS GroupName,
           g.Description AS GroupDescription,
           gsg.ParentID,
           gsg.Ordinal AS GroupOrdinal,
           COALESCE(pp.ProfileID, NULL) AS ProfileID,
           COALESCE(e.BethesdaID, NULL) AS BethesdaID,
           COALESCE(e.NexusID, NULL) AS NexusID,
           gsg.GroupSetID,
           gs.GroupSetName,
           CASE WHEN pp.ProfileID IS NOT NULL THEN 1 ELSE 0 END AS IsEnabled-- Check if Plugin is enabled for the profile
      FROM GroupSets gs
           LEFT JOIN
           GroupSetGroups gsg ON gs.GroupSetID = gsg.GroupSetID
           LEFT JOIN
           ModGroups g ON gsg.GroupID = g.GroupID
           LEFT JOIN
           GroupSetPlugins gsp ON gsg.GroupID = gsp.GroupID AND 
                                  gsg.GroupSetID = gsp.GroupSetID
           LEFT JOIN
           Plugins p ON gsp.PluginID = p.PluginID
           LEFT JOIN
           ProfilePlugins pp ON p.PluginID = pp.PluginID AND 
                                pp.ProfileID = 1-- Example ProfileID filter
           LEFT JOIN
           LoadOutProfiles l ON gsg.GroupSetID = l.GroupSetID
           LEFT JOIN
           ExternalIDs e ON p.PluginID = e.PluginID
     ORDER BY gs.GroupSetID,-- Group by GroupSetID
              l.ProfileID,
              gsg.ParentID,
              gsg.Ordinal,-- Order by Group Ordinal
              p.PluginID/* Order by Plugin ID */;


-- View: vwPlugins
DROP VIEW IF EXISTS vwPlugins;
CREATE VIEW IF NOT EXISTS vwPlugins AS-- Script Date: 9/21/2024 1:49 AM  - ErikEJ.SqlCeScripting version 3.5.2.95
    SELECT p.PluginID,
           p.PluginName,
           p.Description,
           p.Achievements,
           p.DTStamp,
           p.Version,
           p.State,
           gsp.GroupSetID,
           gsp.GroupID,
           gsp.Ordinal AS GroupOrdinal,
           g.GroupName,
           e.BethesdaID,
           e.NexusID
      FROM Plugins p
           LEFT JOIN
           GroupSetPlugins gsp ON p.PluginID = gsp.PluginID
           LEFT JOIN
           ModGroups g ON gsp.GroupID = g.GroupID
           LEFT JOIN
           ExternalIDs e ON p.PluginID = e.PluginID;


COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
