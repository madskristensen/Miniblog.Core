(function () {

    // Setup markdown editor
    var editPost = document.querySelector("#Content");

    if (editPost) {
        var simplemde = new SimpleMDE({
            element: editPost,
            showIcons: [ "clean-block", "table", "code", "strikethrough" ]
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

})();