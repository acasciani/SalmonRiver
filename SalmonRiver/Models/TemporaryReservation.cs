using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SalmonRiver.Models
{
    public class BookNowViewModel
    {
        [Required(AllowEmptyStrings=false, ErrorMessage="Please enter a start date.")]
        public DateTime Start { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter a start date.")]
        public DateTime End { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter a start date.")]
        public int Guests { get; set; }
    }

    public class TemporaryReservationViewModel
    {
        public List<Hold> Holds { get; set; }
        public int GuestCount { get; set; }
        public DateTime Expires { get; set; }
    }
}