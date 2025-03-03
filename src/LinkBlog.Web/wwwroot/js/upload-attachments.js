(function () {
    addEventListener("trix-attachment-add", function (event) {
        if (event.attachment.file) {
            uploadFileAttachment(event.attachment)
        }
    })

    function uploadFileAttachment(attachment) {
        uploadFile(attachment.file, setProgress, setAttributes)

        function setProgress(progress) {
            attachment.setUploadProgress(progress)
        }

        function setAttributes(attributes) {
            attachment.setAttributes(attributes)
        }
    }

    function uploadFile(file, progressCallback, successCallback) {
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
        })

        xhr.send(formData)
    }

    function createFormData(file) {
        var data = new FormData()
        data.append("key", file.name)
        data.append("Content-Type", file.type)
        data.append("file", file)
        return data
    }
})();