(function () {
    // Expand comment form
    var content = document.querySelector("#comments textarea");

    if (content) {
        content.addEventListener("focus", function () {
            document.querySelector(".details").className += " show";
        }, false);
    }
})();