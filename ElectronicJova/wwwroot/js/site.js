// Micro-interactions and animations
function animateCart() {
    const cartIcon = $('#cart-icon-nav');
    cartIcon.addClass('animate-bounce');
    setTimeout(() => cartIcon.removeClass('animate-bounce'), 500);
}

function animateHeart(element) {
    $(element).addClass('animate-pulse');
    setTimeout(() => $(element).removeClass('animate-pulse'), 500);
}

$(document).ready(function () {
    // Handle favorite heart animation
    $('#favoriteBtn, #favoriteBtnImage').click(function () {
        animateHeart(this);
    });

    // Check if item was just added to cart (via TempData or similar hook if needed)
    // For now, these can be called manually or via success responses
});
