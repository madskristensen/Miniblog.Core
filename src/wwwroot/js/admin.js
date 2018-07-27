(function () {

    var edit = document.getElementById("edit");

    if (edit) {
        // Setup editor
        var editPost = document.getElementById("Content");

        tinymce.init({
            selector: '#Content',
            autoresize_min_height: 200,
            plugins: 'autosave preview searchreplace visualchars image link media fullscreen code table hr pagebreak autoresize nonbreaking anchor insertdatetime advlist lists textcolor wordcount imagetools colorpicker',
            menubar: "edit view format insert table",
            toolbar1: 'formatselect | bold italic blockquote forecolor backcolor | imageupload link | alignleft aligncenter alignright  | numlist bullist outdent indent | fullscreen',
            selection_toolbar: 'bold italic | quicklink h2 h3 blockquote',
            autoresize_bottom_margin: 0,
            paste_data_images: true,
            image_advtab: true,
            file_picker_types: 'image',
            relative_urls: false,
            convert_urls: false,
            branding: false,

            setup: function (editor) {
                editor.addButton('imageupload', {
                    icon: "image",
                    onclick: function () {
                        var fileInput = document.createElement("input");
                        fileInput.type = "file";
                        fileInput.multiple = true;
                        fileInput.accept = "image/*";
                        fileInput.addEventListener("change", handleFileSelect, false);
                        fileInput.click();
                    }
                });

            }
        });

        // Delete post
        var deleteButton = edit.querySelector(".delete");
        if (deleteButton) {
            deleteButton.addEventListener("click", function (e) {
                if (!confirm("Are you sure you want to delete the post?")) {
                    e.preventDefault();
                }
            });
        }

        // File upload
        function handleFileSelect(event) {
            if (window.File && window.FileList && window.FileReader) {

                var files = event.target.files;

                for (var i = 0; i < files.length; i++) {
                    var file = files[i];

                    // Only image uploads supported
                    if (!file.type.match('image'))
                        continue;

                    var reader = new FileReader();
                    reader.addEventListener("load", function (event) {
                        var image = new Image();
                        image.alt = file.name;
                        image.onload = function (e) {
                            image.setAttribute("data-filename", file.name);
                            image.setAttribute("width", image.width);
                            image.setAttribute("height", image.height);
                            tinymce.activeEditor.execCommand('mceInsertContent', false, image.outerHTML);
                        };
                        image.src = this.result;

                    });

                    reader.readAsDataURL(file);
                }

                document.body.removeChild(event.target);
            }
            else {
                console.log("Your browser does not support File API");
            }
        }
    }

    // Delete comments
    var deleteLinks = document.querySelectorAll("#comments a.delete");
    if (deleteLinks) {
        for (var i = 0; i < deleteLinks.length; i++) {
            var link = deleteLinks[i];

            link.addEventListener("click", function (e) {
                if (!confirm("Are you sure you want to delete the comment?")) {
                    e.preventDefault();
                }
            });
        }
    }

    // autocomplete for tags
    // Get the <datalist> and <input> elements.
    var dataList = document.getElementById('json-taglist');
    var input = document.getElementById('categories');

    if (dataList && input) {

        // Create a new XMLHttpRequest.
        var request = new XMLHttpRequest();

        // Handle state changes for the request.
        request.onreadystatechange = function (response) {
            if (request.readyState === 4) {
                if (request.status === 200) {
                    // Parse the JSON
                    var jsonOptions = JSON.parse(request.responseText);

                    // Loop over the JSON array.
                    jsonOptions.forEach(function (item) {
                        // Create a new <option> element.
                        var option = document.createElement('option');
                        // Set the value using the item in the JSON array.
                        option.value = item;
                        // Add the <option> element to the <datalist>.
                        dataList.appendChild(option);
                    });

                    // Update the placeholder text.
                    input.placeholder = "e.g. taglist";
                } else {
                    // An error occured :(
                    input.placeholder = "Couldn't load taglist options :(";
                }
            }
        };

        // Update the placeholder text.
        input.placeholder = "Loading options...";

        // Set up and make the request.
        request.open('GET', '/blog/categories', true);
        request.send();
    }

})();