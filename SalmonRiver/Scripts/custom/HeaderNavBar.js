$('.header-site').affix({
    offset: {
        top: function () {
            return (this.bottom = $('#body').outerHeight(true));
        }
    }
});

$('#datetimepicker6').datetimepicker({ format: 'MM/DD/YYYY', enabledDates: ['05/10/2016'] });
$('#datetimepicker7').datetimepicker({
    useCurrent: false, //Important! See issue #1075
    format: 'MM/DD/YYYY',
    enabledDates: ['05/10/2016']
});
$("#datetimepicker6").on("dp.change", function (e) {
    $('#datetimepicker7').data("DateTimePicker").minDate(e.date);
});
$("#datetimepicker7").on("dp.change", function (e) {
    $('#datetimepicker6').data("DateTimePicker").maxDate(e.date);
});