# **Dev Mod Manager**
![Dev Mod Manager](./img/ZeeOgre_256x256.ico)
---
## Description

This application is designed primarily around Starfield mod development.

Starfield Creation Kit does not yet have extensions nor or good lifecycle management. This tool is to help overcome that shortfall.

We leverage existing mod managers to move the various versions of your mod in and out of the game folder by way of the mod manager.  I will provide samples and explanations based on Vortex.

## Installation

## Acquisition 
The latest release should be acquired from [Github](https://github.com/ZeeOgre/LoadOrderManager/releases/latest/LoadOrderManager.msi)

Currently investigating releasing on WinGet as well, and possibly also on Nexus.

## Prerequisites
A Repository Folder must exist with at least one mod installed, and you must identify the Game Folder and the Mod Staging Folder.

```
REPOFOLDER/
├── * SOURCE/
│   └── PluginName/
├── TARGET/
│   └── PluginName/
├── # BACKUP/
│   └── PluginName/
│           ├── * SOURCE/
│           ├── # DEPLOYED/
│           ├── # NEXUS/


MODSTAGINGFOLDER


GAMEFOLDER
```
This folder marked with a * must be specified as the *SOURCE* folder in your configuration. In our example this is DEV.
Under it there must be at least one *PluginName* where you have your files that are in development. For Bethesda games this would be your esp, scripts, textures, etc.
When you compile it with Creation Kit (or whatever tool you use) it will be saved in the *GAMEFOLDER* folder. When you perform a *Gather* operation, the modified files that weren't originally moved will be copied from the *GAMEFOLDER* to the *SOURCE* folder.

	It's worth a little sidebar here to explain how we longeract with Vortex.  Vortex uses junction polongs to move your ModFiles in and out of the game folders.  
	What we're doing is creating a directory junction INTO the Mod Manager (It SHOULD be agnostic as to which one). 
	We then leverage the Mod Manager to perform the actual movement in and out of the game folder.
	So, you've move your mod in, run CK, and created a bunch of scripts and textures 
	- these won't necessarily come back, as the ModManager doesn't know anything about them.  
	This is where the Gather operation comes in.
	The Gather operation scans the GameFolder for files that are newer than the ones in the Source folder, and copies them back.  
	This is a one-way operation, and is designed to be run after you've compiled your mod in the CK.
	You'll typically "undeploy" the mod after doing this, and then deploy it back so the ModManager will know about the rest of it.

Folders marked with a # are those that get created automatically by the tool, and should not be modified. Backup is where we put system wide, and mod specific backups.
Deployed is where we put the mods that are currently in the game folder.  Nexus is where we put the mods that are packaged for deployment to Nexus.


The *Target* folder may be an longermediary testing/staging folder, or could be your release folder, you can have as many as you need, but you really need at least one more.

Your ModManager does a great job of helping you find these folders. Vortex will show you the game folder, game settings folder etc... when you've got the game selected:

![Vortex Game Settings](./img/vortex_mod_openmenu.png)

When you're in the overall settings, you can find the mod staging folder:
![Vortex Mod Staging Folder](./img/vortex_modstagingfolder.png)


## Configure the config.yaml
The `config.yaml` distributed with the application will need to be updated with your actual folder paths. The example is set up to support a **DEV**, **TEST** and **RELEASE** folder structure – you can name them whatever you want, but at a minimum your repository folder must contain a <source> folder, with your mods, each in their own folder.  The Mod Name will be derived from this directory name.

The stage names **BACKUP**, **NEXUS** and **DEPLOYED** are reserved, as they’re used longernally.


The file is well documented with comments for each line
```
repoFolder: <<Your folder where you’re going to store your mods>> ## This is the folder where you're storing your in-development mods'
useGit: true ## If you're using GitHub, set this to true, today this only enables the button in the main longerface, future versions may include git longegration to push/pull the repo'
gitHubRepo: <<GITHUB REPO>> ## If you're using GitHub, this is the URL to your repository if useGit is true then this is required'
useModManager: true ## If you're using a mod manager, set this to true'
modStagingFolder:<<MOD STAGING FOLDER>> #if you're using a mod manager, this is the folder where your mod manager is looking for mods, if useModManager is true then this is required'
gameFolder: <<GAMEFOLDER ROOT>> ## This is the folder where your game is installed if UseModManager is false, this is required. This functionality is not fully implemented yet, however the “Gather Files” function depends on this to find updated files.'
modManagerExecutable: S:\Games\Vortex\Vortex.exe ## If you're using a mod manager, this is the path to the executable for your mod manager, if useModManager is true then this is required'
modManagerParameters: --user-data S:\Games\VortexSteamData ## If you're using a mod manager, these are the launch parameters that may be required - for example if you want to use a custom profile in vortex'
	## Both of these settings can be found from your Vortex shortcut, if you’re not using a custom profile, it should just be the executable
ideExecutable: <<Preferred IDE Path>> ## This is the path to your IDE, this is requred'
	## I use VSCode, but you can specify whatever editor you prefer, since it already has GitSupport, and has good support for compiling Bethesda’s Papyrus, I use that.
modStages: ## These are the stages that your mod will go through, you can add or remove stages as you see fit'
- "*DEV" ## This is the designated SOURCE stage, where you're actively working on the mod, the star indicates that it is the source folder, and there can only be one'
	## This folder MUST exist in your Repository Folder – it will be scanned for existing mods
- TEST
- RELEASE
- "#NEXUS" ## these folders are archive folders, when a mod is packaged for deployment or deployed INTO the mod manager, these are primarily here for documentation, and should not be changed, if they are not included that’s OK, they are already present in the database.'
- "#DEPLOYED"
limitFiletypes: true ## You may want to include everything, or you may want to limit the filetypes moved and archived'
promoteIncludeFiletypes: ## These are the filetypes that will be included when promoting a mod from one stage to another'
- .esm
- .ba2
- .ini
- .txt
packageExcludeFiletypes: ## These are the filetypes that will be excluded when packaging a mod for deployment, we'll be creating folder backups, but will make sure to exclude these if present'
- .zip
- .7z
- .rar
archiveFormat: zip ##supported options are zip and 7z
timestampFormat: yyMMddHHmm ## format for timestamps in filenames - see https://docs.python.org/3/library/datetime.html#strftime-and-strptime-format-codes for options
myNameSpace: <<Your Script namespace ZO.LoadOrderManagermyResourcePrefix: <<Your resource prefix – I use ZO_>> ## When creating any other kinds of objects, use a consistent naming prefix so you can easily locate those resources, this will also help the tool retreive other "strays"'
showSaveMessage: false ## This will show a message box when a save is complete, this is useful if you're running the tool in the background and want to know when it's done'
showOverwriteMessage: false ## This will show a message box when a file is about to be overwritten, '
nexusAPIKey: <<Your Nexus APIKey – this is not necessary now, but is a placeholder if I build in any direct compatibility in the future.>> ## This is your Nexus API key, at the moment, features using this are not implemented'
```
---
# The longerface
---
## Main Window
![Main Window](./img/dmm_mainwindow.png)
## Buttons
---
### Settings
This will open the settings window, you should see this when you first launch the program if you didn't fill the config.yaml before first launch.

![Settings Window](./img/dmm_settingswindow.png)

If you close this window without clicking save, it will revert to the previous settings.

### Backup
This button will create a backup of any mods in the Source folder.  It will create a timestamped zip/7z file in the Backup folder with the contents of the mod at that polong in time.

![Backup Window](./img/dmm_backupresults.png)

I've experimented with backing up only the files that have changed, but it's not reliable, so I'm backing up the whole thing.  This is a good practice to get INTO, as it will allow you to roll back to a previous version if you need to.

### Launch Mod Manager
Whether you use Vortex, MO2 or whatever mod manager you use, this will launch it as long as you specified it correctly.  For Vortex in particular, if you need to make a custom game profile, you'll need to specify the launch parameters to get that to work.

![Vortex Windows Properties](./img/vortex_windowsproperties.png)

### Launch IDE
Starfield Creation Kit has a plugin for VSCode so that you can compile papyrus scripts in a much nicer environment than the CK.  This will launch your IDE, so you can work on your scripts. If you prefer Notepad, Notepad++, whatever you've specified, this will launch it for you.
### Open Github
Github has free solutions that anyone can use. You can mark your repositories private, and if you're using VSCode, or another modern IDE, it will longegrate with Github, having version controlled backups is always a good idea! If you've specified your Github repository in the config.yaml, this button will open it in your default browser.
### Open Game Folder
Does what it says on the tin, opens the game folder in Windows Explorer.
### Load Order
Because everyone does it a little different, I've built a Load Order manager of my own. This mixes the information from Plugins.txt and CreationCatalog.txt to provide a more robust view of what you've got installed.
It's still very much a work in progress, so at this polong the buttons don't even work.  I plan on having categories, and the ability to move entire categories up and down, in addition to actually writing the comments/categories INTO the Plugins.txt (even if it's only there for one go, until the game wipes them out.
)

![Load Order Window](./img/dmm_loadorder_window.png)
## Rows
---
### Mod Name
This is the name of the mod, as it appears in the Source folder.  This is the name of the folder that contains the mod files, when you click on it, it opens the SOURCE folder for the mod.
### Current Stage
If the mod is deployed, this will show you which stage is deployed. When you click on it you have the opportunity to change which (or none at all) stage of that mod is deployed to the Staging folder.

![Deployed Window](./img/dmm_deploywindow.png)

### Staged Folder
This is the folder where the mod is currently staged.  If you click on it, it will open the folder in Windows Explorer.	

### Gather
This button will scan the GameFolder for files that are newer than the ones in the Source folder, and copy them back.  This is a one-way operation, and is designed to be run after you've compiled your mod in the CK.  You'll typically "undeploy" the mod after doing this, and then deploy it back so the ModManager will know about the rest of it.

### Backup Folder
This button opens the backup folder for the mod.  This is where the backups are stored, and you can see the timestamped backups here.	

### Promote
This will open the promotion longerface for your mod. Choose your source and target stages, and click the Promote button.  This will move the mod from one stage to another, and will also create a backup of the mod in the target stage.
This will only move the "Allowed Filetypes" as specified in the config.yaml/Configuration screen.  If you have other files that need to be moved, you'll need to update these settings for them to get moved. If you've unchecked "Limit File Types" all the files in the folder will move over.

![Promote Window](./img/dmm_promote_window.png)
### Package
This will open the package window. Select the mod you want to package, and click the Package button.  This will create a backup of the mod in the regular backup folder for that stage, as well as place a "clean" un-timestamped zip/7zip here for you to quickly upload to Nexus.

![Package Window](./img/dmm_package_window.png)
### Bethesda ID
If you haven't defined, or don't have a Bethesda ID, you can enter it here.  This will allow you to quickly open the mod page on Bethesda.net.  This is not required, but it's a nice feature to have.
If it says "Bethesda" it means you haven't provided the Creations URL.
When you look at a creation on Bethesda.net, the URL will look like this:
![Bethesda Address Zohst](img/bethesda_address_zohst.png)

The GUID between "details" and the Name of the mod is the Bethesda ID. Keep everything up to the GUID, and paste it in to the Bethesda URL field.
![Dmm Bethesda Url](img/dmm_bethesda_url.png)

### Nexus ID
Just like the Bethesda ID, if you have a Nexus ID, you can enter it here.  This will allow you to quickly open the mod page on Nexus.  This is not required, but it's a nice feature to have.
If it says "Nexus" it means you haven't provided the Nexus ID.
When you look at a mod on Nexus, the URL will look like this:	

![Nexus Address Zohst](img/nexus_address_zohst.png)

The number at the end of the URL is the Nexus ID.  In this case, paste the whole URL INTO the Nexus URL field.

## Load Order Window
---
![Dmm Loadorder Window](img/dmm_loadorder_window.png)

This is a work in progress, but it will show you the load order of your mods, and allow you to change the order of them.  This is a feature that is not yet fully implemented.
Plans include:
LOOT import/export support
User Defined Categories
LoadOuts
Plugins.txt and Starfield.ccc support.
	I've done initial testing with Plugins.txt, and I can write the comment lines that will reflect your categories.  Unfortunately, Starfield at least strips them out every time you load the game. But, with our database here, you should be able to recreate that for sharing with others easily.
 
## The Database
The database is a simple SQLite database that stores the information about your mods.  It's stored in the same folder as the application, and is named `dmm.db`.  It's a simple database, and you can open it with any SQLite viewer, or even with the SQLite command line tool.  

[Dev Mod Manager Db](img/LoadOrderManager_db.dgml) this is the DGML file for the database, you can open it with Visual Studio, or any other DGML viewer.  It shows the relationships between the tables in the database.

## The Future
Enhanced Load Order Management
	- LOOT Import/Export
	- User Defined Categories
	- LoadOuts
	- Plugins.txt and Starfield.ccc support
    - NXM link handling
	- Replicating hardlink deployment/mod staging functionality
	- Automating Bethesda/Nexus id retrieval

Smarter backup management
	- Only backup files that have changed

Direct Github longegration
	- Push/Pull
	- Tagging
	- CI/CD release management

	




