using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SalmonRiver
{
    public enum Errors
    {
        BookNow_InvalidStartOrEndDate = 1, 
        BookNow_SomeDatesUnavailable = 2, 
        BookNow_SelectedDatesOnHold = 3,
        BookNow_InvalidGuestSelection = 4,
        BookNow_OtherError = 5,
        Reserve_BookNowSessionExpired = 6,
        BookNow_EndDateExceedsMaxDate = 7,
        BookNow_FirstDayMustBeSaturday = 8,
        BookNow_LastDayMustBeFriday = 9,
        BookNow_LengthMustBeSevenDays = 10
    }
}