(function () {

    var editPost = document.querySelector("#Post_Content");

    if (editPost) {
        var simplemde = new SimpleMDE({
            element: editPost,
            showIcons: [ "clean-block", "table", "code", "strikethrough" ]
        });
    }

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

})();