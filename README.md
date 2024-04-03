![Flagship_Header_Logo-White](https://github.com/ArrowThief/Flagship_Manager_Dev/assets/33704630/930bc17d-20a3-407f-8269-bbb1f45c3e61)

# Flagship Render Manager

Flagship is a distrobuted render manager for After effects and Blender projects.

## Why?
If you have ever worked in VFX you know that the biggest time sink is rendering. Not only the time it takes to process the rendering, but setting up and managing renders and machine time is also very time-consuming.
Other render managers do exist, but they require a lot of setup for them to work with any given project. 
My goal with Flagship was to create a streamlined client > server > worker system. Where the client could click a single button to submit a render job to the server and then the server could split the job into smaller tasks and distribute them to the workers. 
In addition, I wanted it to be extremely user-friendly and allow render submissions from within the applications users are already familiar with.
To finish I needed a web-based UI that allowed for managing workers, and jobs alike that any local user could access and manage. 

## Prep.
To get started you will need
- three computers, It can technically all be done on a single computer, but it won't improve anything.
- A shared file storage location. Accessible from clients, workers, and the manager.

## Manger Setup.
1. Launch Flagship Manager on your designated manager computer.
2. It will open a web browser leading you to a setup page asking for three file paths.
     - Work order file path - This is where files from AfterEffects and Blender will be saved for the manager to load them.
     - Temp Projects Path - This is a location where a copy of every render project is saved. This prevents write issues if you continue to work within the same project.
     - Temp Render Path - This is used for Blender rendering. Blender always outputs to a folder, even when node-based output is set up. Those files need to go somewhere so this folder exists to hold them.
3. Once you have input your file paths click save and the web UI will load.
4. You're done. The manager is set up.

## Worker Setup. 
 -- Add later --

## AfterEffects Setup
1. Open After Effects and go to File> scripts> Install scriptUIpanel, then choose the Flagship_Render_UI_Panel_v3.1.js from the plugins folder.
2. Restart AE
3. A new window should now be visible with a few buttons, click the path settings button and input your WorkOrder path and Temp projects path from the Manager setup.
4. Once that is complete will be able to set up a render normally within After Effects, then click the submit render button in the flagship UI panel.
5. The webUI will update to show the new Job and any workers currently connected should start rendering it automatically.
   
## Blender Setup. 
1. Copy the Flagship_Client_v6.0.py file to your blender plugins folder.
2. Open Blender and go to preferences and plugins. Search for flagship and click the checkbox.
3. A new UI box will appear in your Render settings. Go there and input your Work order path, Temp Projects path, and Temp Render Path.
4. Set up a render as you usually would, but click the submit render in the flagship panel. 
