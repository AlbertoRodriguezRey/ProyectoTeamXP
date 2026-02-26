// ========================================
// TEAMXP - site.js
// ========================================
$(function () {
    'use strict';

    // ---- Navbar scroll effect ----
    var $nav = $('.navbar');
    $(window).on('scroll.navbar', function () {
        $nav.toggleClass('scrolled', $(this).scrollTop() > 60);
    });

    // ---- Smooth scroll solo para anclas reales (no # vacío ni dropdowns) ----
    $(document).on('click', 'a[href^="#"]', function (e) {
        var href = this.getAttribute('href');
        if (!href || href === '#') return; // deja actuar a Bootstrap dropdowns
        var target = $(href);
        if (target.length) {
            e.preventDefault();
            $('html,body').animate({ scrollTop: target.offset().top - 70 }, 600);
        }
    });

    // ---- Auto-dismiss alerts after 5 s ----
    setTimeout(function () {
        $('.alert, .auth-msg').fadeOut(400, function () { $(this).remove(); });
    }, 5000);

    // ---- Scroll reveal ----
    function reveal() {
        $('.fade-in-scroll').each(function () {
            var rect = this.getBoundingClientRect();
            if (rect.top < window.innerHeight - 60) $(this).addClass('visible');
        });
    }
    $(window).on('scroll.reveal resize.reveal', reveal);
    reveal();

    // ---- Confirm delete ----
    $(document).on('click', '.btn-delete', function (e) {
        if (!confirm('¿Eliminar este elemento?')) e.preventDefault();
    });

    // ---- Prevent double submit ----
    $(document).on('submit', 'form', function () {
        var $btn = $(this).find('[type=submit]');
        if ($btn.data('submitted')) return false;
        $btn.data('submitted', true);
        setTimeout(function () { $btn.data('submitted', false); }, 3000);
    });

    // ---- Bootstrap tooltips & popovers ----
    document.querySelectorAll('[data-bs-toggle="tooltip"]')
        .forEach(function (el) { new bootstrap.Tooltip(el); });
    document.querySelectorAll('[data-bs-toggle="popover"]')
        .forEach(function (el) { new bootstrap.Popover(el); });

});
