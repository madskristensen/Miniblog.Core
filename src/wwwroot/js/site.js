window.addEventListener("load", function () {

    // Lazy load images
    var timer;
    window.addEventListener("scroll", lazyload);
    window.addEventListener("resize", lazyload);

    function lazyload() {
        if (timer) {
            return;
        }

        timer = setTimeout(function () {
            timer = null;
            var images = document.body.querySelectorAll("img[data-src]");

            if (images.length === 0) {
                window.removeEventListener("scroll", lazyload);
                window.removeEventListener("resize", lazyload);
                return;
            }

            var viewHeight = Math.max(document.documentElement.clientHeight, window.innerHeight);

            for (var i = 0; i < images.length; i++) {
                var img = images[i];
                var rect = img.getBoundingClientRect();

                if (!(rect.bottom < 0 || rect.top - 100 - viewHeight >= 0)) {
                    img.src = img.getAttribute("data-src");
                    img.removeAttribute("data-src");
                }
            }
        }, 150);
    }

    lazyload();

    // Expand comment form
    var content = document.querySelector("#comments textarea");

    if (content) {
        content.addEventListener("focus", function () {
            document.querySelector(".details").className += " show";
        }, false);
    }
});