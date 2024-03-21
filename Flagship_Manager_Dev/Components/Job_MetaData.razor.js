export function getScreenHeight() {
    return window.innerHeight || document.documentElement.clientHeight;
}
export function copyToClipboard(text) {
    // Create a temporary textarea element to hold the text
    const textArea = document.createElement('textarea');
    textArea.value = text;

    // Make the textarea invisible
    textArea.style.position = 'fixed';
    textArea.style.top = '0';
    textArea.style.left = '0';
    textArea.style.opacity = 0;

    // Append the textarea to the DOM
    document.body.appendChild(textArea);

    // Select the text within the textarea
    textArea.select();

    try {
        // Copy the selected text to the clipboard
        document.execCommand('copy');
    } catch (err) {
        console.error('Unable to copy to clipboard:', err);
    }

    // Remove the temporary textarea
    document.body.removeChild(textArea);
}

export function removeFocus(ID) {
    console.log("Called Remove focus");
    console.log(ID)
    ele = document.getElementById(ID);
    console.log(ele)
    ele.blur();
}
export function WriteTest() {
    console.log("Hello world")
}
export function uncheckAllCheckboxes() {
    var checkboxes = document.querySelectorAll("input[type='checkbox']");
    for (var i = 0; i < checkboxes.length; i++) {
        checkboxes[i].checked = false;
    }
}