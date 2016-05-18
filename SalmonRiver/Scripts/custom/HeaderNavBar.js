$('.header-site').affix({
    offset: {
        top: function () {
            return (this.bottom = $('#body').outerHeight(true));
        }
    }
});