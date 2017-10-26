(function () {

    // Expand comment form
    var content = document.querySelector("#comments textarea");
    if (content) {
        content.addEventListener("focus", function () {
            document.querySelector(".details").className += " show";
        }, false);
    }

    // Lazy load images/iframes
    window.addEventListener("load", function () {

        var timer,
            images,
            viewHeight;

        function init() {
            images = document.body.querySelectorAll("[data-src]");
            viewHeight = Math.max(document.documentElement.clientHeight, window.innerHeight);

            lazyload(0);
        }

        function scroll() {
            lazyload(200);
        }

        function lazyload(delay) {
            if (timer) {
                return;
            }

            timer = setTimeout(function () {
                var changed = false;

                requestAnimationFrame(function () {
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
                            changed = true;
                        }
                    }

                    if (changed) {
                        filterImages();
                    }

                    timer = null;
                });

            }, delay);
        }

        function filterImages() {
            images = Array.prototype.filter.call(
                images,
                function (img) {
                    return img.hasAttribute('data-src');
                }
            );

            if (images.length === 0) {
                window.removeEventListener("scroll", scroll);
                window.removeEventListener("resize", init);
                return;
            }
        }

        // polyfill for older browsers
        window.requestAnimationFrame = (function () {
            return window.requestAnimationFrame ||
                window.webkitRequestAnimationFrame ||
                window.mozRequestAnimationFrame ||
                function (callback) {
                    window.setTimeout(callback, 1000 / 60);
                };
        })();


        window.addEventListener("scroll", scroll);
        window.addEventListener("resize", init);

        init();
    });

})();
