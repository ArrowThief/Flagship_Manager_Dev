from msilib.schema import Directory
import bpy
import os
import json
from datetime import datetime
from bpy.app.handlers import persistent
from bpy.props import IntProperty
import math
import random
import textwrap
from pathlib import Path
import time


bl_info = {
    "name": "Flagship Client 0.6",
    "description": "Allows submiting shots to the Flagship Manager",
    "author": "Nick Siebold",
    "version": (0, 6),
    "blender": (4, 0, 0),
    "location": "Render Tab",
    "category": "Render",
}

startup = True
ctlPath = "M:\\Render Watch folders\\RenderControl\\RenderCMD\\"
force = False
tempProjectPath ="L:\Libraries\RenderTemp"
tempOutputPath ="L:\Libraries\RenderTemp\\Temp Output\\"
timeout = 30
priority = 50
split = 1
@persistent
def ImportSettings(self, context):
    print("Running Import")
    SettingsPath = str(Path.home() / "Documents") +"/FlagShip_Settings.txt"
    if(os.path.exists(SettingsPath)):
        with open(SettingsPath, 'r') as file:
            Settings= file.read() 
    json_Settings = json.loads(Settings)

    global ctlPath
    global tempOutputPath
    global tempProjectPath
    global startup

    tempProjectPath = json_Settings['TempProjectPath']
    tempOutputPath = json_Settings['TempOutputPath']
    ctlPath = json_Settings['ctlFolder']

    print("Import Flagship Temp Projects Path: " + tempProjectPath)
    print("Import Flagship Temp Output path: " + tempOutputPath)
    print("Import Flagship ctlFolder: " + ctlPath)
    startup = False


#----------------------------------------------------------------------------------------------------------------------------------
        
class FlagshipUIproperties(bpy.types.PropertyGroup):
    bl_idname = "ui.props"
    bl_label = "ui_props"

    global ctlPath
    global tempOutputPath
    global tempProjectPath
    global timeout
    global split
    global priority
    global startup

    
    ctlPath_Prop: bpy.props.StringProperty(name="Database Folder")
    tempProjectPath_Prop: bpy.props.StringProperty(name="tempProject",default =tempProjectPath)
    priority_Prop: bpy.props.IntProperty(name="priority", soft_min=0, soft_max=100, default =priority)
    split_Prop: bpy.props.IntProperty(name="frames per task", soft_min=1, soft_max=50, default = split)
    timeout_Prop: bpy.props.IntProperty(name="Timeout", soft_min=1, soft_max=120, default = timeout)

#----------------------------------------------------------------------------------------------------------------------------------

class Continue(bpy.types.Operator):
    bl_idname = "opp.continue"
    bl_label = "Function 1"
    
    
    def execute(self, context):
        global force
        # Implement your first function here
        self.report({'INFO'}, "Submiting with errors")
        force = True
        if force: 
            print("Force is true!=====================")
        bpy.ops.render.submitrender()
        return {'FINISHED'}
 
class MessageBox(bpy.types.Operator):
    bl_idname = "message.messagebox"
    bl_label = "Flagship ERROR"
    
    
    errorString = ""
    
    def draw(self, context):
        global force
        layout = self.layout

        wrapp = textwrap.TextWrapper(width=60)
        wList = wrapp.wrap(text=self.errorString)

        for text in wList:
            labelRow = layout.row(align=True)
            labelRow.label(text=text)
        
        
        row = layout.row(align=True)
        row.scale_y = 1.0
        row.operator("opp.continue", text = "Submit anyway",)
         
    def execute(self, context):
        print("Running execute") 
        return {'FINISHED'}
 
    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self, width = 300)
    
    def close_panel(event):
        print("close_Panel ran")
        x, y = event.mouse_x, event.mouse_y
        bpy.context.window.cursor_warp(10, 10)

        move_back = lambda: bpy.context.window.cursor_warp(x, y)
        bpy.app.timers.register(move_back, first_interval=0.001)

#----------------------------------------------------------------------------------------------------------------------------------

class SubmitRender(bpy.types.Operator):
    bl_idname = "render.submitrender"
    bl_label = "submit_render"
    
    def GetFirstActiveOutputNode():
   
        # GetsOutput path with hashes
    
        for node in bpy.context.scene.node_tree.nodes:
            if node.bl_idname == 'CompositorNodeOutputFile':
                if node.mute == False:
                    for input in node.inputs:
                        if input.is_linked:
                            return node  
    
    
    def FindOutputPath(_basePath, extension):
        try:
            fullFilePath = os.path.dirname(_basePath) + "\\"
            FileName = os.path.basename(_basePath)
        except:
            Print('test')
        #Add numbers if ther aren't any.
        if(FileName == ""):
            #No name at all
            FileName = "####" 
        #if ther is a name but no numbers
        elif(FileName[-1] != "#"):
            #Has a name but is missing hashes at end of name
            if(FileName[-1] != "_"):
                #missing underscore and hashes
                FileName += "_####" 
            else: 
                #not missing underscore but still missing hashes
                FileName += "####"
        fullFilePath += FileName + extension 
        return fullFilePath   
    # Gets extension from node output
    
    def GetNodeOutputExtension(_format):

        #Returns correct file extention for format.
        
        if(_format == "BPM"):return ".bpm"    
        elif(_format == "IRIS"): return ".iris"
        elif(_format == "PNG"): return ".png"
        elif(_format == "JPEG"): return ".jpg"
        elif(_format == "JPEG2000"): return ".jpeg"
        elif(_format == "TARGA"): return ".tga"
        elif(_format == "TARGA_RAW"): return ".tga"
        elif(_format == "CINEON"): return ".cin"
        elif(_format == "DPX"): return ".dpx"
        elif(_format == "OPEN_EXR_MULTILAYER"): return ".exr"
        elif(_format == "OPEN_EXR"): return ".exr"
        elif(_format == "DPX"): return ".dpx"   
        elif(_format == "HDR"): return ".hdr"   
        elif(_format == "TIFF"): return ".tif"   
        elif(_format == "WEBP"): return ".webp"
        return
    
    def FindAdjustedSplit(Frames, FrameStep):
        
        #Returns auto adjusted split number
        
        Adjusted = 0;
        Count = 0;
        while(Adjusted <= Frames):
            Count+1;
            Adjusted += FrameStep;
        
        return Count; 

    def execute(self, context):
        
        #Builds JSON object from project settings.
        #Checks if filepath is in use, itterates if true.
        #Saves project to CtlFolder.


        global ctlPath
        global force
        global tempProjectPath 
        global tempOutputPath
        global timeout
        global priority
        global split

        print("Flagship Temp Projects Path: " + tempProjectPath)
        print("Flagship Temp Output path: " + tempOutputPath)
        print("Flagship ctlFolder: " + ctlPath)

        if force:
            print("force Value: True")
        else:
            print("force Value: False")
        error = False
        errorMessage = "Output error! Please check your file output path"
        
        ogFilePath = bpy.data.filepath
        workorderFilePath = ctlPath
        if(workorderFilePath == ""):workorderFilePath = "M:\\Render Watch folders\\RenderControl\\RenderCMD\\"
        currentScene = bpy.context.scene
        tempProjectDir = tempProjectPath
        while(True):
            GenFileName = str(random.randrange(10000, 99999))
            if not os.path.exists(tempProjectDir + GenFileName):
                projectFilepath = tempProjectDir +"\\"+ GenFileName + '.blend'
                break
        
        file = os.path.basename(projectFilepath)
        if(ogFilePath == ""):
            ogFilePath= "Unsaved Project"
        
        tmpOutputPath = tempOutputPath + file.replace('.blend', '') + "\\" + file.replace('.blend', '')
        
        fileName = os.path.splitext(file)[0]
        renderApp = "blender"
        Step = currentScene.frame_step
        currentScene.render.use_overwrite = True
        overwrite = currentScene.render.use_overwrite
        
        #set tmp output to the one in the GUI
        currentScene.render.filepath = tmpOutputPath
        
        startFrame = currentScene.frame_start
        frameRange = (currentScene.frame_end - startFrame)
        frameRange += 1
        priority = priority
        split = math.ceil(frameRange/split)
        
        if(Step > 1):
            Adjusted = 0
            Count = 0
            while(Adjusted <= frameRange):
                Count+=1
                Adjusted += Step
            split = math.ceil(Count / split)
            
        maxRenderTime = timeout

        #check if nodes are being used for output
        
        if(currentScene.use_nodes):
            
            #Using Nodes
            
            nodes = currentScene.node_tree.nodes
            ActiveOutput = GetFirstActiveOutputNode();
            print(ActiveOutput)
            if ActiveOutput == None: 
                self.report({'ERROR'}, message ="Output is set to use Nodes, but no node based output is connected.")
                return {"FINISHED"};
            outputType = ActiveOutput.format.file_format
            print("using Nodes")
            if(outputType == "OPEN_EXR_MULTILAYER"):
                
                #Get name from base path 
                
                outputType = "MULTILAYER_EXR"
                if(ActiveOutput.base_path[-1] == "\\"):
                    
                    if force:
                        ActiveOutput.base_path = ActiveOutput.base_path[:-1] + "_####.exr"
                    else: 
                        error = True
                elif(ActiveOutput.base_path[-1] == "/"):
                    print("Hit this")
                    if force:
                        ActiveOutput.base_path = ActiveOutput.base_path[:-1] + "_####.exr"
                    else: error = True
                elif(ActiveOutput.base_path[-1] == "_"):
                    
                    #Check for underscores in file output names
                    
                    if force: 
                        ActiveOutput.base_path += "####.exr"
                    else:
                        error = True
                elif(ActiveOutput.base_path[-1] == "#"):
                    if force: 
                        ActiveOutput.base_path += ".exr"
                    else: 
                        error = True
                elif(ActiveOutput.base_path[-3:] != "exr"):
                    if force:
                        ActiveOutput.base_path += "_####.exr"
                    else:
                        error = True
                if(error):
                    errorMessage = "Naming error in output path, please make sure path ends in a file not a folder" 
                filePath = ActiveOutput.base_path
                outputFilePath = filePath 
                
            else: 

                #Not using nodes.
                #Get name from file_slots[0].path
                
                for name in ActiveOutput.file_slots:
                    if(name.path[-1] != "_" and name.path[-1] != "#"):
                        
                        #Check for underscores in file output names
                        
                        name.path += "_"
                filePath = ActiveOutput.base_path
                indexCount = 0
                fileName = "null"
                
                for input in ActiveOutput.inputs:
                    if input.is_linked:
                        fileName = ActiveOutput.file_slots[indexCount].path
                        if(fileName == "Image"):
                           if not force: 
                                error = True
                                errorMessage = "Output filename is set to Image, this could indicate that you haven't setup your outputs" 
                        elif(fileName == "Image_"):
                           if not force: 
                                error = True
                                errorMessage = "Output filename is set to Image, this could indicate that you haven't setup your outputs" 
                        break
                    else: 
                        indexCount+=1
                        print("Not this one")
                
                if fileName == "null":
                    print("ERROR, cannot find a usable output name")
                    return
                
                if not os.path.exists(ActiveOutput.base_path):
                    if not force: 
                        error = True
                        errorMessage = "Output path dosen't exist"
                
                if(ActiveOutput.base_path[-1] != "\\"):
                    if not force:
                        error = True
                        errorMessage = "Output Path Should be a folder, but it's currently a file"
                
                renderExtension = SubmitRender.GetNodeOutputExtension(outputType)
                outputFilePath = SubmitRender.FindOutputPath(filePath + fileName, renderExtension)
            
            print("This is the FP: " + outputFilePath)
            
        else:
            #Using Output from Render panel
            print("Not using nodes")
            outputType = currentScene.render.image_settings.file_format
            tempPath = currentScene.render.filepath
            if(tempPath[-1] != "_" and tempPath[-1] != "#"):
                currentScene.render.filepath += "_"
            outputFilePath = SubmitRender.FindOutputPath(currentScene.render.filepath, currentScene.render.file_extension)

        #Find if GPU is required for this render.

        if (currentScene.cycles.device == "CPU"):
            GPU = False
        else: GPU = True

        #Find if this render is to a video file.

        if(outputType == "AVI_JPEG"):
            vid = True
        elif (outputType == "AVI_RAW"):
            vid = True
        elif (outputType == "FFMPEG"):
            vid = True
        else :vid = False
                
        #Naming check: 
        
        if not force and error:
            MessageBox.errorString = errorMessage
            bpy.ops.message.messagebox('INVOKE_DEFAULT')
            return {"FINISHED"}
        
        
        print("SAVING FILE PATH: " + projectFilepath)
        bpy.ops.wm.save_as_mainfile(copy=True,filepath = projectFilepath)
        
        #buld Json object
        
        outputWorkOrder = {
            "Project": projectFilepath,
            "WorkingProject": ogFilePath,  
            "Name": bpy.path.basename(bpy.data.filepath).rsplit(".",1)[0],
            "extinfo": "",
            "outputType": outputType,
            "Filepath": outputFilePath,
            "StartFrame": startFrame,
            "FrameRange": frameRange,
            "FrameStep": Step,
            "GPU": GPU,
            "OW": overwrite,
            "vid": vid,
            "RenderApp": renderApp,
            "Priority": priority,
            "split": split,
            "MaxRenderTime": maxRenderTime,
            }
        writeOutput =  "[" +str(outputWorkOrder).replace("'", "\"") + "]"
        writeOutput = writeOutput.replace("True", "true")
        writeOutput = writeOutput.replace("False","false")
        FinalOutput = workorderFilePath + fileName + ".txt"
        
        outputFile = open(FinalOutput, "w") 
        outputFile.write( writeOutput)
        outputFile.close()
        
        if(force):
            force = False
        print("Finished")
        return {'FINISHED'}

#----------------------------------------------------------------------------------------------------------------------------------

class FlagshipClientPanel(bpy.types.Panel):
    """Creates a Panel in the scene context of the properties editor"""
    bl_label = "Flagship Client v5"
    bl_idname = "FlagShipPanel"
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "render"
    
    

    def draw(self, context):
        global startup

        if startup:
            print("Startup?")
            startup = False
            
        Props = context.scene.worth_group_tools
        temp = Props.ctlPath_Prop

        layout = self.layout
        
        col1 = layout.column()

        #CtlPath text input
        col1.prop(Props, "ctlPath_Prop")

        #Temp projects path text input
        col1.prop(Props, "tempProjectPath_Prop")
        row1 = layout.row()

        #Priority int input
        row1.prop(Props, "priority_Prop")
        
        row2 = layout.row()

        #Split int input
        row2.prop(Props, "split_Prop")

        #Timeout int input
        row2.prop(Props, "timeout_Prop")
        
        # Big render button
        row = layout.row()
        row.scale_y = 2.0
        row.operator("render.submitrender", text = "Render on network")

#----------------------------------------------------------------------------------------------------------------------------------

def register():
    bpy.utils.register_class(FlagshipUIproperties)
    bpy.utils.register_class(FlagshipClientPanel)
    bpy.utils.register_class(SubmitRender)
    bpy.utils.register_class(Continue)
    bpy.utils.register_class(MessageBox)
    bpy.types.Scene.worth_group_tools = bpy.props.PointerProperty(type=FlagshipUIproperties)
    #bpy.utils.register_class(ImportSettings)
    #Import settings is under construction.

def unregister():
    bpy.utils.unregister_class(FlagshipUIproperties)
    bpy.utils.unregister_class(FlagshipClientPanel)
    bpy.utils.unregister_class(SubmitRender)
    bpy.utils.unregister_class(Continue)
    bpy.utils.unregister_class(MessageBox)
    #bpy.utils.unregister_class(ImportSettings)

if __name__ == "__main__":
    register()

