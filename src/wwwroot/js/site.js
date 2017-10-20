// Lazy load images/iframes
window.addEventListener("DOMContentLoaded", function () {

    var timer;
    window.addEventListener("scroll", lazyload);
    window.addEventListener("resize", lazyload);

    function lazyload(delay) {
        if (timer) {
            return;
        }

        timer = setTimeout(function () {
            timer = null;
            var images = document.body.querySelectorAll("[data-src]");

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
                    img.onload = function (e) {
                        e.target.className = "loaded";
                    };

                    img.className = "notloaded";
                    img.src = img.getAttribute("data-src");
                    img.removeAttribute("data-src");
                }
            }
        }, delay || 150);
    }

    lazyload(0);
});