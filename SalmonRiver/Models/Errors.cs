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
        Reserve_BookNowSessionExpired = 5
    }
}