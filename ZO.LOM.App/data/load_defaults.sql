--Insert into GroupSets
INSERT INTO [GroupSets] ([GroupSetID],[GroupSetName],[GroupSetFlags]) VALUES
(1,BASELINE_GS,9),
(2,SINGLETON_GS,12);

INSERT INTO [LoadOutProfiles] ([ProfileID],[ProfileName],[GroupSetID])
    VALUES (1, 'Baseline' ,1),
	VALUES (2, '(Default)' ,1);

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
(2, -998, 9, 1);


-- Insert into ExternalIDs
INSERT INTO [ExternalIDs] ([ExternalID], [PluginID], [BethesdaID], [NexusID]) VALUES 
(1, 9, NULL, '10189');
