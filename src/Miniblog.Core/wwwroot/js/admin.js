(function () {

    var simplemde;

    // Setup markdown editor
    var editPost = document.querySelector("#Content");

    if (editPost) {
        simplemde = new SimpleMDE({
            element: editPost,
            showIcons: ["clean-block", "table", "code", "strikethrough"],
            spellChecker: false
        });
    }

    // Delete comments
    var deleteLinks = document.querySelectorAll("a.delete");

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

    // Delete post
    var deleteButton = document.querySelector(".delete");

    if (deleteButton) {
        deleteButton.addEventListener("click", function (e) {
            if (!confirm("Are you sure you want to delete the post?")) {
                e.preventDefault();
            }
        });
    }

    // File upload
    var fileUpload = document.getElementById("files");
    var postId = document.querySelector("input#ID");

    if (simplemde && fileUpload) {
        fileUpload.addEventListener("change", function (e) {
            for (var i = 0; i < fileUpload.files.length; i++) {
                var file = fileUpload.files[i];
                var name = file.name.substr(0, file.name.lastIndexOf("."));
                var ext = file.name.substr(name.length);
                simplemde.value(simplemde.value() + "\n\n![" + name + "](/files/" + name + "_" + postId.value + ext + ")");
            }
        });
    }

})();