// Trix HTML Extensions
// This file adds support for custom HTML elements like iframes in Trix editor

(function() {
    // Update DOMPurify configuration.
    document.addEventListener("trix-before-initialize", function () {
        // Trix.config.dompurify.ADD_ATTR = [];
        Trix.config.dompurify.ADD_TAGS = ["iframe"];
      });

    // Wait for Trix to be fully loaded
    document.addEventListener("trix-initialize", function() {
        // Add a custom button to the Trix toolbar
        addIframeButton();
    });

    // Add iframe button to the toolbar
    function addIframeButton() {
        // Get all Trix editors on the page
        const elements = document.querySelectorAll("trix-editor");

        elements.forEach(element => {
            // Find the toolbar for this editor
            const toolbar = element.toolbarElement;
            if (!toolbar) return;

            // Find the button groups
            const buttonGroups = toolbar.querySelectorAll(".trix-button-group--text-tools");
            if (!buttonGroups.length) return;

            // Create iframe button (add to the last button group)
            const lastGroup = buttonGroups[buttonGroups.length - 1];

            // Create a new button
            const button = document.createElement("button");
            button.type = "button";
            button.classList.add("trix-button");
            button.setAttribute("data-trix-action", "x-insertHTML");
            button.setAttribute("title", "Insert HTML/iframe");
            button.setAttribute("tabindex", "-1");
            button.innerHTML = "&lt;/&gt;"; // HTML tag icon

            // Add the button to the toolbar
            lastGroup.appendChild(button);

            // Add event listener for the button
            button.addEventListener("click", function() {
                promptForHTML(element.editor);
            });
        });
    }

    // Prompt user for custom HTML
    function promptForHTML(editor) {
        const html = prompt("Enter HTML code (e.g., iframe embed code):", "");
        if (html) {
            insertHTML(editor, html);
        }
    }

    // Insert HTML at the current cursor position
    function insertHTML(editor, html) {
        // Insert the HTML
        var attachment = new Trix.Attachment({ content: html })
        editor.insertAttachment(attachment)
    }
})();