--
-- File generated with SQLiteStudio v3.4.4 on Sun Sep 22 13:46:12 2024
--
-- Text encoding used: System
--
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

-- Trigger: trgInsteadOfInsert_vwLoadOuts
CREATE TRIGGER trgInsteadOfInsert_vwLoadOuts
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
CREATE TRIGGER trgInsteadOfInsert_vwModGroups
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
CREATE TRIGGER trgInsteadOfInsert_vwPluginFiles
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
                             IsArchive
                         )
                         VALUES (
                             NEW.FileID,
                             NEW.PluginID,
                             NEW.Filename,
                             NEW.RelativePath,
                             NEW.DTStamp,
                             NEW.HASH,
                             NEW.IsArchive
                         )
                         ON CONFLICT (
                             FileID
                         )
                         DO UPDATE SET PluginID = COALESCE(excluded.PluginID, FileInfo.PluginID),
                         Filename = COALESCE(excluded.Filename, FileInfo.Filename),
                         RelativePath = COALESCE(excluded.RelativePath, FileInfo.RelativePath),
                         DTStamp = COALESCE(excluded.DTStamp, FileInfo.DTStamp),
                         HASH = COALESCE(excluded.HASH, FileInfo.HASH),
                         IsArchive = COALESCE(excluded.IsArchive, FileInfo.IsArchive);
END;


-- Trigger: trgInsteadOfInsert_vwPlugins
CREATE TRIGGER trgInsteadOfInsert_vwPlugins
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
CREATE TRIGGER trgInsteadOfUpdate_vwLoadOuts
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
CREATE TRIGGER trgInsteadOfUpdate_vwModGroups
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
CREATE VIEW vwLoadOuts AS
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
CREATE VIEW vwModGroups AS
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
CREATE VIEW vwPluginFiles AS
    SELECT fi.FileID,
           p.PluginID,
           p.PluginName,
           fi.Filename,
           fi.RelativePath,
           fi.DTStamp,
           fi.HASH,
           fi.IsArchive
      FROM Plugins p
           JOIN
           FileInfo fi ON p.PluginID = fi.PluginID;


-- View: vwPluginGrpUnion
CREATE VIEW vwPluginGrpUnion AS
    SELECT COALESCE(p.PluginID, NULL) AS PluginID,
           COALESCE(p.PluginName, NULL) AS PluginName,
           COALESCE(p.Description, NULL) AS Description,
           COALESCE(p.Achievements, NULL) AS Achievements,
           COALESCE(p.DTStamp, NULL) AS DTStamp,
           COALESCE(p.Version, NULL) AS Version,
           COALESCE(p.State, NULL) AS State,
           gsp.Ordinal AS GroupOrdinal,
           gsg.GroupID AS GroupID,
           g.GroupName AS GroupName,
           g.Description AS GroupDescription,
           gsg.ParentID,
           gsg.Ordinal AS Ordinal,
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
CREATE VIEW vwPlugins AS-- Script Date: 9/21/2024 1:49 AM  - ErikEJ.SqlCeScripting version 3.5.2.95
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
