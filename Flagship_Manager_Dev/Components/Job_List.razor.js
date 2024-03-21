
export function Update_UI(_ID, _ListID) {
    var eleP = document.getElementById(_ID + "p");
    var eleH = document.getElementById(_ID + "h");

    var height = eleP.scrollHeight;
    eleH.style.height = height + "px";
    console.log("Scroll Height: " + eleP.scrollHeight);
}