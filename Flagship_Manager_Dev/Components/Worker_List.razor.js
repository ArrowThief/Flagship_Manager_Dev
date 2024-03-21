export function ShowExtendedName(ID) {
    var labelEle = document.getElementById(ID + "l");
    if (labelEle.offsetWidth > 130)
    {
        
        //console.log("Showing extended Name.")
        //Get Name element and status element
        var eleN = document.getElementById(ID);

        //Get lablel width and add 20px for comfort. 
        var newWidth = labelEle.offsetWidth + 20;
        console.log("new width: " + newWidth)
        //Apply new width and make status disapear for ease of reading.
        eleN.style.width = newWidth + "px";
        if (labelEle.offsetWidth > 150) {
            var eleS = document.getElementById(ID + "s");
            eleS.style.opacity = 0;
        }
    }
}
export function HideExtendedName(ID)
{
    //Get name and status elements.
    var eleN = document.getElementById(ID);
    var eleS = document.getElementById(ID + "s");

    //Return elements to default state.
    eleS.style.opacity = 100;
    eleN.style.width = "140px";
}

