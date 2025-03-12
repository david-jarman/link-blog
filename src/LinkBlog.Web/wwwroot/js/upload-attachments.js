(function () {
    document.addEventListener("trix-attachment-add", function (event) {
        if (event.attachment.file) {
            uploadFileAttachment(event.attachment)
        }
    })

    document.addEventListener("trix-file-accept", function(event) {
        const acceptedTypes = ["image/jpeg", "image/png", "image/gif"];
        if (!acceptedTypes.includes(event.file.type)) {
          event.preventDefault();
          alert("Only image files are allowed!");
        }
      });

    function uploadFileAttachment(attachment) {
        uploadFile(attachment, setProgress, setAttributes)

        function setProgress(progress) {
            attachment.setUploadProgress(progress)
        }

        function setAttributes(attributes) {
            attachment.setAttributes(attributes)
        }
    }

    function uploadFile(attachment, progressCallback, successCallback) {
        var file = attachment.file
        var formData = createFormData(file)
        var xhr = new XMLHttpRequest()

        xhr.open("POST", "/api/upload", true)

        xhr.upload.addEventListener("progress", function (event) {
            var progress = event.loaded / event.total * 100
            progressCallback(progress)
        })

        xhr.addEventListener("load", function (event) {
            if (xhr.status == 201) {
                // Get url from the Location header
                var location = xhr.getResponseHeader("Location")

                var attributes = {
                    url: location,
                    href: location
                }
                successCallback(attributes)
            }
            else {
                removeAttachment(attachment, xhr.status);
            }
        })

        xhr.onerror = function() {
            removeAttachment(attachment, xhr.status);
        };

        xhr.send(formData)
    }

    function createFormData(file) {
        var data = new FormData()
        data.append("key", file.name)
        data.append("Content-Type", file.type)
        data.append("file", file)
        return data
    }

    function removeAttachment(attachment, status) {
        var element = document.querySelector("trix-editor")
        element.editorController.removeAttachment(attachment);

        alert("Upload failed with status: " + status);
    }
})();