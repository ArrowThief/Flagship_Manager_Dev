#include "L:/Libraries/Plugins/AfterEffects/SharedScripts/json2.js";
//Global Var
{
    var documentsFolder = File(Folder.userData).fsName;
    documentsFolder = new Folder(documentsFolder).parent;
    documentsFolder = new Folder(documentsFolder).parent;
    var defaultSettingsFilePath = documentsFolder.fsName + "\\Documents\\FlagShip_Settings.txt";
    var defaultWorkOrderPath = "M:\\Render Watch folders\\RenderControl\\RenderCMD\\";
    var defaultProjectPath = "L:\\Libraries\\RenderTemp\\";

    var GPU = true;
    var OW = true;
    var Priority = 50;
    var split = 0;
    var showWindow = false;
    var fileName = Math.round(generateRandomNumber() * 1000000).toString() + ".aep";
    var tempProjectPathString = "";
    var WorkorderPath = "";
    var tempOutput = "";
}
//Settings File management.

function LoadSettingsFile() {

    //Loads Settings file from user documents folder.

    var settingsFile = new File(defaultSettingsFilePath);

    if (settingsFile.exists) {

        //If settings file exists then settings will be loaded.

        settingsFile.encoding = 'UTF8';
        settingsFile.read()
        settingsFile.open("r");

        var string = settingsFile.read();
        var json = JSON.parse(string);
        WorkorderPath = json.ctlFolder;
        tempProjectPathString = json.TempProjectPath;
        tempOutput = json.TempOutputPath;

    } else {

        //If settings file doesn't exist, default values will be used.

        tempProjectPathString = defaultProjectPath;
        WorkorderPath = defaultWorkOrderPath;
    }

}

function saveSettingsFile() {

    //Creates a settings file in user documents dir.
    //This file can be used by all Flagship applications.

    var SettingsObject =
    {
        "ctlFolder": WorkorderPath,
        "TempProjectPath": tempProjectPathString,
        "TempOutputPath": tempOutput
    }
    var exportFile = new File(encodeURI(defaultSettingsFilePath));
    if (canWriteFiles(exportFile)) {
        try {
            writeFile(exportFile, SettingsObject);
        }
        catch (err) {
            alert("Failed to write file after passing \"CanWriteFiles\" Check \n" + err)
        }
    }
    else alert("Can't write file")
}

LoadSettingsFile()
{
    //Settings window

    var settingsWindow = new Window("palette", "Settings", undefined);

    var PathGroup = settingsWindow.add("group")
    PathGroup.orentation = " row";
    var WO_Entry = PathGroup.add("edittext", undefined, WorkorderPath);
    var TP_Entry = PathGroup.add("edittext", undefined, tempProjectPathString);


    var buttonsGroup = settingsWindow.add("group")
    var closeButton = buttonsGroup.add("button", undefined, "Save");
    var defaultButton = buttonsGroup.add("button", undefined, "Defaults");

    //Text window settings
    var noChange = true;
    var noSplitChange = true;
}


//Submit Functions

function SubmitRender(workOrderFilePath, projectFilePath, GPU, OW, priority, split) {

    //Builds JSON object with project info,
    //Creates a temp copy of the project in temp projects folder
    //Attempts to save workorder to CtlFolder.

    var renderItems = app.project.renderQueue.numItems;
    var TotalRendersSubmitted = 0;
    var RenderObjects = [];
    var outputType = "Custom";
    var WorkingProjectPath = app.project.file.fsName;

    for (var i = 1; i <= renderItems; i++) {
        try {
            outputType = app.project.renderQueue.item(i).outputModule(1).name
        }
        catch (err) {
            outputType = "Custom";
        }
        if (app.project.renderQueue.item(i).render == true) {
            var frameRate = 1 / app.project.renderQueue.item(i).comp.frameDuration;
            var renderObject =
            {
                "Project": projectFilePath,
                "WorkingProject": WorkingProjectPath,
                "Name": app.project.renderQueue.item(i).comp.name,
                "extinfo": app.project.file.path,
                "outputType": outputType,
                "Filepath": app.project.renderQueue.item(i).outputModule(1).file.fsName,
                "StartFrame": app.project.renderQueue.item(i).comp.displayStartTime * frameRate + app.project.renderQueue.item(i).comp.workAreaStart * frameRate,
                "FrameRange": 1 / app.project.renderQueue.item(i).comp.frameDuration * app.project.renderQueue.item(i).comp.workAreaDuration,
                "GPU": GPU,
                "OW": OW,
                "vid": VIDorImage(app.project.renderQueue.item(i).outputModule(1).file.toString()),
                "RenderApp": "AE",
                "Priority": priority,
                "split": split,
                "MaxRenderTime": 30,
                "QueueIndex": i
            }
            RenderObjects.push(renderObject);
            app.project.renderQueue.item(i).render = false
            TotalRendersSubmitted = TotalRendersSubmitted + 1;
        }
    }

    if (TotalRendersSubmitted > 0) {
        app.project.save(File(projectFilePath));
        saveWorkOrder(workOrderFilePath, RenderObjects);
    }
    else alert("Nothing queued to render.");
}
function VIDorImage(Filepath) {

    //Returns true if output file is a single video file,
    //Returns false if output files are single frames 

    var patt = "[a-zA-Z+0-9]+$";
    var vid = ["mov", "mp4", "avi", "r3d", "mp3", "wav"];
    var fileType = Filepath.substring(Filepath.search(patt));
    for (i = 0; i <= vid.length; i++) {
        if (vid[i] == fileType) {
            return true;

        }
    }
    return false;
}
function saveWorkOrder(filepath, content) {

    //Checks for filename conflicts, increments filename if needed and writes workoder to output path.

    var num = 1;
    var fileName = "WorkOrder";
    var baseName = "WorkOrder";
    var scriptFolderPath = filepath + fileName;
    var runWhile = true;

    while (runWhile)
    {

        //Check for existing file and increse the number depending on how many work orders exist currently.

        var scriptFolderPath = filepath + fileName;
        var F = new File(scriptFolderPath + ".txt");
        if (F.exists) {
            fileName = baseName + num;
            num++;
        } else runWhile = false;
    }
    var exportFile = new File(encodeURI(scriptFolderPath + ".txt"));

    if (canWriteFiles(exportFile)) {
        try {
            writeFile(exportFile, content);
        }
        catch (err) {
            alert("Failed to write file after passing \"CanWriteFiles\" Check \n" + err)
        }
    }
    else alert("Can't write file")
}
function writeFile(fileObj, content, encoding)
{
    //Writes workerObject as a JSON string.

    content = (JSON.stringify(content));
    encoding = encoding || "utf-8";

    fileObj = (fileObj instanceof File) ? fileObj : new File(fileObj);

    var parentFolder = fileObj.parent;

    if (!parentFolder.exists && !parentFolder.create()) {
        alert("Cannot create file in path " + fileObj.fsName);
        throw new Error("Cannot create file in path " + fileObj.fsName);
    }

    fileObj.encoding = encoding;

    fileObj.open("w");

    fileObj.write(content);

    fileObj.close();

    return fileObj;
}

function canWriteFiles() {
    //Checks if output can write to location.

    if (isSecurityPrefSet()) return true;

    alert(script.name + " requires access to write files.\n" +

        "Go to the \"General\" panel of the application preferences and make sure " +

        "\"Allow Scripts to Write Files and Access Network\" is checked.");

    app.executeCommand(2359);//Open General Preferances

    return isSecurityPrefSet();
}
function isSecurityPrefSet()
{

    //Checks prefrences to make sure settings are correctly setup.

    if (app.preferences.getPrefAsLong(

        "Main Pref Section",

        "Pref_SCRIPTING_FILE_NETWORK_SECURITY"

    ) === 1) {
        return true;
    }
    else return false;

}

function checkPriorityNum(Num) {

    //Checks priority for valid value.

    var _num = parseInt(Num, 10);
    if (!isNaN(_num)) {

        if (_num > 100) return 100;
        else if (_num < 0) return 0;
        else return _num;
    }
    else return 50;
}

function checkSplitNum(Num) {
    //Checks split for valid value.

    try {
        var _num = parseInt(Num, 10);
        if (isNaN(_num)) {
            alert("Split is not a num: " + Num)
            return 0;
        } else {
            return _num;
        }
    }
    catch (err) {
        alert("error on split number, setting default value.")
        return 0;
    }

}

//End Submit Functions

//Main UI Panel
{
    function myScript(thisObj) {
        function myScript_buildUI(thisObj) {

            //UIPanel for Flagship

            var myPanel = (thisObj instanceof Panel) ? thisObj : new Window("palette", "Revisions", undefined, { resizeable: false, closeButton: true });

            //Submit Button
            var Row1 = myPanel.add("group", undefined, "Row1");
            var SubmitButton = Row1.add("button", undefined, "Submit Render Job")

            //Settings
            var setPanel = myPanel.add("TabbedPanel", undefined, "Settings");
            var GPUcheckbox = setPanel.add("checkbox", undefined, "GPU");
            GPUcheckbox.value = false
            var OWcheckbox = setPanel.add("checkbox", undefined, "Overwrite");
            OWcheckbox.value = true
            var PriorityWindow = setPanel.add("edittext", undefined, "Priority");
            var workerSplit = setPanel.add("edittext", undefined, "Worker Split");

            //Advanced settings
            var Row3 = myPanel.add("group", undefined, "Row3");
            var FileButton = Row3.add("button", undefined, "Path Settings")


            //Help tips
            {

                PriorityWindow.helpTip = "This will set the priority of the render with a max value of 100. 100 will render before 1.";
                workerSplit.helpTip = "This sets how many workers you would like the job to be renderred on. If left blank or 0 it will auto calculate a good number on the server.";
                GPUcheckbox.helpTip = "If checked this will only render on machines with GPUs.";
                OWcheckbox.helpTip = "If checked this will overwirte existing frames in the output folder.";
                SubmitButton.helpTip = "This will submit the job to the render server.";
                FileButton.helpTip = "This will allow you to set the output directory for the workorder. Only change if you know what you are doing.";
                //End help tips
            }

            //On click functions:
            {
                SubmitButton.onClick = function () {

                    if (GPUcheckbox.value == true) {
                        GPU = true;
                    }
                    else GPU = false;
                    if (OWcheckbox.value == true) {
                        OW = true;
                    }
                    else OW = false;
                    if (workerSplit.text == "Worker Split") {
                        split = 0;
                    }
                    else split = checkSplitNum(workerSplit.text);
                    if (PriorityWindow.text == "Priority") {
                        priority = 50;
                    }
                    else priority = checkPriorityNum(PriorityWindow.text);
                    fileName = Math.round(generateRandomNumber() * 1000000).toString() + ".aep";
                    SubmitRender(WorkorderPath, (tempProjectPathString + fileName), GPU, OW, priority, split);
                }

                FileButton.onClick = function () {
                    settingsWindow.show();
                }

                closeButton.onClick = function () {
                    WorkorderPath = WO_Entry.text;
                    tempProjectPathString = TP_Entry.text;
                    saveSettingsFile();
                    settingsWindow.hide();
                }
                defaultButton.onClick = function () {
                    WorkorderPath = defaultWorkOrderPath;
                    tempProjectPathString = defaultProjectPath;

                    //OrderPath.text = defaultWorkOrderPath;
                    //projectFilePath.text = defaultProjectPath;

                    WO_Entry.text = defaultWorkOrderPath;
                    TP_Entry.text = defaultProjectPath;

                    settingsWindow.hide();
                }
                //End On click functions
            }

            //Mouse over functions:
            {

                PriorityWindow.addEventListener("mouseover", function () {
                    if (PriorityWindow.text == "Priority") {
                        this.justify = "center";
                        this.text = "50";
                        //PriorityWindow. 
                    }
                })
                //Auto set the priority text field to priority or 50 depending on if anything hance chnaged.
                PriorityWindow.addEventListener("focus", function () {
                    noChange = false;
                })

                PriorityWindow.addEventListener("mouseout", function () {
                    if (this.text == "50" && noChange) {
                        this.justify = "center";
                        this.text = "Priority";
                    }
                })
                //Response to people wanting more than 100 levels of priority.
                PriorityWindow.addEventListener("changing", function () {
                    if (parseInt(PriorityWindow.text) == 69) alert("Nice.");
                    else if (parseInt(PriorityWindow.text) == 420) alert("Light it up! ...But its still maxes out at 100.")
                    else if (parseInt(PriorityWindow.text) > 100) {
                        this.justify = "center";
                        alert("Yeah, no. Sorry to rain on your ridiculous parade, but you only get from 0-100")
                        this.text = "100";
                        //PriorityWindow.    
                    }
                    else if (PriorityWindow.text == "50") {
                        this.text = "Priority";
                        noChange = true;
                    }
                })

                //Worker split text field auto change.
                workerSplit.addEventListener("focus", function () {
                    noSplitChange = false;
                })
                workerSplit.addEventListener("mouseover", function () {
                    if (workerSplit.text == "Worker Split") {
                        this.justify = "center";
                        this.text = "0";
                    }
                })
                workerSplit.addEventListener("mouseout", function () {
                    if (this.text == "0" && noSplitChange) {
                        this.justify = "center";
                        this.text = "Worker Split";
                    }
                })
                workerSplit.addEventListener("changing", function () {

                    //if (parseInt(this.text) >= 10) Alert("This is going to split between 10 computers, the frames will be devided equilly, you aren't making frames of 10 unless you only have 100 frames.");
                    if (workerSplit.text == "0") {
                        noSplitChange == true;
                        this.text = "Worker Split";
                    }
                })

                //End Mouse over functions
            }

            myPanel.layout.layout(true);

            return myPanel;
        }


        var myScriptPal = myScript_buildUI(thisObj);

        if (myScriptPal != null && myScriptPal instanceof Window) {
            myScriptPal.center();
            myScriptPal.show();
        }
    }

    myScript(this);
}
