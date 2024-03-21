
export function OpenCloseElement(_ID, _itemCount) {
    var eleH = document.getElementById(_ID + "h");
    console.log("List height ID: " + _ID + "h\n" + "ClientHeight: " + eleH.clientHeight);
    var eleP = document.getElementById(_ID + "p");
    var Time = 1;
    var height = eleP.clientHeight;
    Time = height * .0009;

    eleH.style.transitionDuration = Time + "s";
    eleP.style.transitionDuration = Time + "s";
    eleH.style.transitionTimingFunction = "linear";
    eleP.style.transitionTimingFunction = "linear";

    if (_ID == "2") {
        console.log("Adjusting...")
        var eleH2 = document.getElementById("3h");
        var eleP2 = document.getElementById("3p");
        eleH2.style.transitionDuration = Time + "s";
        eleP2.style.transitionDuration = Time + "s";
        eleH2.style.transitionTimingFunction = "linear";
        eleP2.style.transitionTimingFunction = "linear";


    }

    if (eleH.style.height == "0px") {
        eleH.style.height = height + "px"; //Don't forget the extra 36px for header.
        eleP.style.bottom = 0 + "px";
        console.log("Opening Element, setting height to: " + height);
    }
    else {
        console.log("Closing Element")
        //autoAdjustHeight(_ID);
        eleH.style.height = 0 + "px";
        eleP.style.bottom = height + "px"//temp + "px";//((_itemCount * 35) + 36) + "px";
    }
    console.log("client Height: " + height);

}
export function OpenInfo(_ID) {
    var ele = document.getElementById(_ID);
    if (ele.style.display == "block") {
        ele.style.display = "none";
    }
    else {
        ele.style.display = "block";
    }
}
export function SetElementOpacity(_ID, Opacity) {
    var ele = document.getElementById(_ID);
    ele.style.opacity = Opacity;
    console.log("setting opacity on element.")
}
export function SetCheckValue(_ID, value) {
    var ele = document.getElementById(_ID);
    var V = (value === "true");
    ele.checked = V;
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

    var ele = document.getElementById(ID, CurrentValue);
    var EnteredValue = ele.value;
    //console.log("Entered value: " + EnteredValue + "\nCurrent Priority is: " + CurrentValue);
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
export function expandTaskList(_ID, _ListID) {

    var eleH = document.getElementById(_ListID + "h");
    var eleP = document.getElementById(_ListID + "p");
    var MaxA = document.getElementById(_ID + "MaxA")

    var adjusted = eleH.clientHeight;
    //console.log("TEMP: " + temp)

    var HDiv = document.getElementById(_ID + "H");
    var PDiv = document.getElementById(_ID + "P");
    var Adiv = document.getElementById(_ID + "arrow");

    var BL = document.getElementById(_ID + "BL")
    var TL = document.getElementById(_ID + "TL")

    //var ListHeight = ListH.style.heigh;

    eleH.style.transitionDuration = ".4s"
    eleP.style.transitionDuration = ".4s"
    HDiv.style.transitionDuration = ".4s"
    PDiv.style.transitionDuration = ".4s"
    Adiv.style.transitionDuration = ".4s"
    BL.style.transitionDuration = ".4s"
    TL.style.transitionDuration = ".4s"

    eleH.style.transitionTimingFunction = "linear"
    eleP.style.transitionTimingFunction = "linear"
    HDiv.style.transitionTimingFunction = "linear"
    PDiv.style.transitionTimingFunction = "linear"
    Adiv.style.transitionTimingFunction = "linear"
    BL.style.transitionTimingFunction = "linear"
    TL.style.transitionTimingFunction = "linear"

    if (HDiv.style.height == "0px") {
        HDiv.style.height = "272px"
        adjusted += 272;
        eleH.style.height = adjusted + "px"
        PDiv.style.bottom = "0px"
        Adiv.style.rotate = "90deg"
    }
    else {
        HDiv.style.height = "0px"
        adjusted -= HDiv.clientHeight;
        eleH.style.height = adjusted + "px"
        PDiv.style.bottom = "272px"
        Adiv.style.rotate = "0deg"
        MaxA.style.rotate = "0deg"
        BL.style.height = 182 + "px";
        BL.style.maxHeight = 182 + "px";
        TL.style.maxHeight = 182 + "px";
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
    console.log("Arrow: " + Arrow)
    //var Adiv = document.getElementById(_ID + "arrow");
    console.log("Task List height: " + height);
    if (JobDiv.style.height == "272px") {
        console.log("Maximize!")
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
export function CloseAllJobs(JobIDList, _ListID) {
    for (var i = 0; i < JobIDList; i++) {
        var HDiv = document.getElementById(JobIDList[i] + "H");
        if (HDiv.style.height == "0px") {
            expandTaskList(JobIDList[i], _TaskListID);
        }
    }
}