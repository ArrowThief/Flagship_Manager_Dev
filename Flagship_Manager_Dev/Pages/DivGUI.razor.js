export function OpenCloseLists(_ID, _CloseList) {
    //console.log("OpenCloseLists")
    var eleH = document.getElementById(_ID + "h");
    var JL = document.getElementById("jl" + _ID)

    var eleP = document.getElementById(_ID + "p");
    var height = JL.clientHeight;
    var Time = height * .0005;
    if (Time < .1) Time = .1;
    else if (Time > 1.5) Time = 1.5
    var AdjustedTime = Math.round(Time * 1000);
    
    eleP.style.transitionDuration ="0s";
    eleP.style.transitionTimingFunction = "linear";
    eleH.style.transitionDuration = Time + "s";
    eleH.style.transitionTimingFunction = "linear";
    
    if (_ID == "2") {
        var eleH2 = document.getElementById("3h");
        var eleP2 = document.getElementById("3p");
        eleH2.style.transitionDuration = Time + "s";
        eleH2.style.transitionTimingFunction = "linear";
        eleP2.style.transitionDuration = Time + "s";
        eleP2.style.transitionTimingFunction = "linear";
    }
    if (eleH.style.height == "0px") 
    {
        //Opening

        eleP.style.bottom = height + 36 + "px";
        eleP.style.transitionDuration = Time + "s";

        setTimeout(function () {
            eleH.style.height = height + 36 + "px";
            eleP.style.bottom = 0 + "px";
        }, 25); 
        return "true," + AdjustedTime; 
    }
    else
    {
        //Closing

        eleP.style.transitionDuration = Time + "s";
        eleH.style.height = 0 + "px";
        eleP.style.bottom = height + 36 + "px";
        
        if (_ID != 1) {
            //console.log("Closing all jobs in " + _ID + "h")
            setTimeout(function () {
                CloseAllJobs(_ID, _CloseList, true)
                eleP.style.bottom = JL.clientHeight + "px"
            }, AdjustedTime); 
        }

        return "false," + AdjustedTime; 
    }
}
export function OpenInfo(_ID) {
    var ele = document.getElementById(_ID);
    if (ele.style.display == "block")
    {
        ele.style.display = "none";
    }
    else
    {
        ele.style.display = "block";
    }
}
export function SetElementOpacity(_ID, Opacity) {
    var ele = document.getElementById(_ID);
    ele.style.opacity = Opacity;
    console.log("setting opacity on element.")
}
export function EnableDisableElement(_ID, _value) {
    var ele = document.getElementById(_ID);
    ele.disabled = _value;
    console.log("setting checkbox value at: " + _value)
}
export function SetCheckValue(_ID, value) {
    try {
        var ele = document.getElementById(_ID);
        var V = (value === "true");
        ele.checked = V;
        console.log("Changed checkbox value at: " + _ID + " to: " + V);
    } catch {
        console.log("Couldn't change Checkbox value at: " + _ID)
    }
    }
export function ChangeBarColor(_ID, value) {
    var V = (value === "true");
    console.log(_ID);
    var eleB = document.getElementById(_ID)
    if (V) {
        eleB.style.background = "#4d4d4d"
    } else {
        eleB.style.background = "#424242";
    }
}
export function autoAdjustHeight(_ID) {
    var eleH = document.getElementById(_ID + "h");
    var eleP = document.getElementById(_ID + "p");
    var temp = eleP.scrollHeight;
    eleH.style.height = temp + "px"
}
export function removeFocus(ID) {
    //console.log("Called Remove focus");
    try {
        var ele = document.getElementById(ID);
    } catch {
        console.log("ERROR: Failed to find element from passed ID: " + ID)
    }
    ele.blur();
}
export function CheckValue(ID, CurrentValue) {
    //console.log("ID: "+ID + "\nCurrent Value: " + CurrentValue)
    var ele = document.getElementById(ID + "Pri");
    var EnteredValue = ele.value;
    if (EnteredValue > 0 && EnteredValue < 101) {
        if (EnteredValue == 69) {
            console.log("Nice");
        }
        else {
            console.log("Entered value is within acceptable range.")
        }
        return Number(EnteredValue);
    }
    else {
        if (EnteredValue == 420) {
            console.log("I see what you did there. Cute, but the max value is still 100.\nReturning priority value to: " + CurrentValue);
        } else {
            console.log("Entered value is NOT within acceptable range.\nReturning priority value to: " + CurrentValue)
        }
        ele.value = CurrentValue;
        return CurrentValue;
    }
}
export function SelectPriorityValue(_ID) {
    var ele = document.getElementById(_ID);
    ele.select();
}
export function getTotalSize(_ID, _TaskListID) {
    console.log(_TaskListID)
    console.log(_ID);
    var listEle = document.getElementById(_TaskListID);

    var listHeight = listEle.style.maxHeight;
    listHeight = parseInt(listHeight.substring(0, listHeight.length - 2));

    var ele = document.getElementById(_ID + "task");

    var innerEle = document.getElementById(_ID + "TD")
    var eleSize = innerEle.offsetHeight;
    var NewListHeight = 0;

    if (ele.style.maxHeight == "0px") {
        ele.style.maxHeight = eleSize + "px";
        NewListHeight = eleSize + listHeight;

    } else {
        ele.style.maxHeight = "0px";
        NewListHeight = listHeight - eleSize;
    }

    listEle.style.maxHeight = NewListHeight + "px"
    console.log(eleSize)
    return eleSize;

}
export function expandTaskList(_ID, _ListID, _ignoreList) {

    var ListH = document.getElementById(_ListID + "h");
    var ListP = document.getElementById(_ListID + "p");
    var TaskMaxArrow = document.getElementById(_ID + "MaxA")

    var adjusted = ListH.clientHeight;

    var TaskH = document.getElementById(_ID + "H");
    var TaskP = document.getElementById(_ID + "P");
    var TaskArrow = document.getElementById(_ID + "arrow");

    var BL = document.getElementById(_ID + "BL")
    var TL = document.getElementById(_ID + "TL")

    if (!_ignoreList) {
        ListH.style.transitionDuration = ".4s"
        ListP.style.transitionDuration = ".4s"
        TaskH.style.transitionDuration = ".4s"
        TaskP.style.transitionDuration = ".4s"
    } else {
        ListH.style.transitionDuration = "0s"
        ListP.style.transitionDuration = "0s"
        TaskH.style.transitionDuration = "0s"
        TaskP.style.transitionDuration = "0s"
    }
    
    TaskArrow.style.transitionDuration = ".4s"
    BL.style.transitionDuration = ".4s"
    TL.style.transitionDuration = ".4s"

    if (_ListID == "2") {
        var eleH2 = document.getElementById("3h");
        var eleP2 = document.getElementById("3p");
        eleH2.style.transitionDuration = ".4s";
        eleH2.style.transitionTimingFunction = "linear";
        eleP2.style.transitionDuration = ".4s";
        eleP2.style.transitionTimingFunction = "linear";
    }

    ListH.style.transitionTimingFunction = "linear"
    ListP.style.transitionTimingFunction = "linear"
    TaskH.style.transitionTimingFunction = "linear"
    TaskP.style.transitionTimingFunction = "linear"
    TaskArrow.style.transitionTimingFunction = "linear"
    BL.style.transitionTimingFunction = "linear"
    TL.style.transitionTimingFunction = "linear"

    if (TaskH.style.height == "0px")
    {//Opening
        TaskH.style.display = "block";
        setTimeout(function () {
            TaskH.style.height = "272px"
            adjusted += 272;
            ListH.style.height = adjusted + "px"
            TaskP.style.bottom = "0px"
            TaskArrow.style.rotate = "90deg"
        }, 100); 
    }
    else
    {//Closing
        TaskH.style.height = "0px"
        adjusted -= TaskH.clientHeight;
        if (!_ignoreList)ListH.style.height = adjusted + "px"
        TaskP.style.bottom = "272px"
        TaskArrow.style.rotate = "0deg"
        TaskMaxArrow.style.rotate = "0deg"
        BL.style.height = 182 + "px";
        BL.style.maxHeight = 182 + "px";
        TL.style.maxHeight = 182 + "px";
        setTimeout(function () {
            TaskH.style.display = "hidden";
        }, 450); 
    }
}
export function maximizeTaskList(_ID, _ListID) {
    var ListDiv = document.getElementById(_ListID + "h");
    //var eleP = document.getElementById(_ListID + "p");

    var JobDiv = document.getElementById(_ID + "H");
    //var ListD = document.getElementById(_ID + "TL")

    var MaxA = document.getElementById(_ID + "MaxA")
    var BL = document.getElementById(_ID + "BL")
    var TL = document.getElementById(_ID + "TL")

    //JobDiv.style.transition = ".4s"

    var height = TL.scrollHeight;
    var blHeight = height;
    var adjustedHeight = ListDiv.clientHeight;

    var Arrow = "";
    //console.log("Arrow: " + Arrow)
    //var Adiv = document.getElementById(_ID + "arrow");
    console.log("Task List height: " + height);
    if (TL.clientHeight >= 182) {
        if (JobDiv.style.height == "272px") {
            console.log("Maximize!")
            console.log("Task List height: " + TL.clientHeight)
            adjustedHeight += height - 182;
            //overFlow = "hidden"
            Arrow += "180deg"
            blHeight += 36;
        }
        else {
            console.log("Contracting!");

            adjustedHeight -= height;
            adjustedHeight += 182;
            height = 182;
            Arrow += "360deg"
            blHeight = height;
        }
        console.log("BL Height: " + blHeight)

        //set transition time;
        TL.style.transitionTimingFunction = "linear";
        JobDiv.style.transitionTimingFunction = "linear";
        BL.style.transitionTimingFunction = "linear";
        ListDiv.style.transitionTimingFunction = "linear";
        TL.style.transitionDuration = "1s"
        JobDiv.style.transitionDuration = "1s"
        BL.style.transitionDuration = "1s"
        ListDiv.style.transitionDuration = "1s"

        //Set Job List height;
        ListDiv.style.height = adjustedHeight + "px"
        //Set Job height;
        JobDiv.style.height = height + 90 + "px"
        //Set maximize arrow up or down.
        MaxA.style.rotate = Arrow;
        //Set Black list and Task list height and overflow.
        BL.style.height = blHeight + "px";
        BL.style.maxHeight = blHeight + "px";
        TL.style.maxHeight = height + "px";
    }

}
export function CloseAllJobs(_ListID, _List, _ignoreList) {
    if (_ListID == "1") return;
    //console.log("Closing All");
    
    for (var i = 0; i < _List.length; i++) {
        console.log("Checking: " + _List[i]+"H")
        var HDiv = document.getElementById(_List[i] + "H");
        if (HDiv.style.height != "0px") {
            expandTaskList(_List[i], _ListID, _ignoreList);
        }
    }
    
}

export function UpdateListHeights() {
    //Get list

    console.log("Running UpdateList Heights");
    var Wo = document.getElementById("1h");
    var Wojl = document.getElementById("jl1");
    var Ar = document.getElementById("2h");
    var Arjl = document.getElementById("jl2");
    var Ac = document.getElementById("3h");
    var Acjl = document.getElementById("jl3");
    setTimeout(function () {
        //console.log("Done waiting: " + Time)
        //var WoHeight = Wo.clientHeight;
        var WojlHeight = Wojl.clientHeight;
        //var ArHeight = Ar.clientHeight;
        var ArjlHeight = Arjl.clientHeight;
        //var AcHeight = Ac.clientHeight;
        var AcjlHeight = Acjl.clientHeight;
        //console.log("Ar Div height: " + ArHeight + "\nAr Job List height: " + ArjlHeight + "\nAc Div height: " + AcHeight + "\nAc Job List height: " + AcjlHeight);
        if (Ar.offsetHeight > 0) Ar.style.height = ArjlHeight + 36 + "px";
        if (Ac.offsetHeight > 0) Ac.style.height = AcjlHeight + 36 + "px";
        if (Wo.offsetHeight > 0) Wo.style.height = WojlHeight + 36 + "px";
    }, 100);  
}
export function getInfo() {
    //Get list 
    var Ar = document.getElementById("2h");
    var Arjl = document.getElementById("jl2");
    var Ac = document.getElementById("3h");
    var Acjl = document.getElementById("jl3");

    //Get list height
    var ArHeight = Ar.clientHeight;
    var ArjlHeight = Arjl.clientHeight;
    var AcHeight = Ac.clientHeight;
    var AcjlHeight = Acjl.clientHeight;

    //calculate adjustment
    console.log("Ar Div height: " + ArHeight + "\nAr Job List height: " + ArjlHeight + "\nAc Div height: " + AcHeight + "\nAc Job List height: " + AcjlHeight);
}
export function CheckHeight(wOpen, arOpen, acOpen) {

    if (wOpen) {
        var w = document.getElementById("1h");
        var wjl = document.getElementById("jl1");
        //console.log((w.clientHeight -32) + " vs " + wjl.clientHeight);
        if ((w.clientHeight) < wjl.clientHeight) {
            //console.log("Updating Archive List Height")
            UpdateListHeights()
        }
    }
    if (arOpen)
    {
        var Ar = document.getElementById("2h");
        var Arjl = document.getElementById("jl2");
        //console.log((Ar.clientHeight - 32) + " vs " + Arjl.clientHeight);
        if ((Ar.clientHeight - 32) < Arjl.clientHeight) {
            //console.log("Updating Archive List Height")
            UpdateListHeights()
        }
    }
    if (acOpen) {
        var Ac = document.getElementById("3h");
        var Acjl = document.getElementById("jl3");
        //console.log((Ac.clientHeight) + " vs " + Acjl.clientHeight);
        if ((Ac.clientHeight - 32) < Acjl.clientHeight) {
            //console.log("Updating Active List Height")
            UpdateListHeights()
        }
    }
}
export function SetHeight(id, height) {
    console.log(height);
    var ele = document.getElementById(id);
    ele.style.clipPath = height;
}
