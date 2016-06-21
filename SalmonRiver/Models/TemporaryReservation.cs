using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
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
        private List<Hold> _Holds = null;
        public List<Hold> Holds
        {
            get
            {
                return _Holds;
            }
            set
            {
                _Holds = value;
                UpdateTotalCost();
            }
        }

        private int _GuestCount = 0;
        public int GuestCount
        {
            get
            {
                return _GuestCount;
            }
            set
            {
                _GuestCount = value;
                UpdateTotalCost();
            }
        }

        /// <summary>
        /// Returns the expiration of the Hold date that expires soonest.
        /// </summary>
        public DateTime Expires
        {
            get
            {
                return Holds.Min(i => i.Expiration);
            }
        }

        public decimal TotalCost { get; private set; }

        public decimal SecurityDeposit { get; private set; }


        #region contact info
        [Required(AllowEmptyStrings=false, ErrorMessage="Please enter your first and last name.")]
        [StringLength(100, MinimumLength=5, ErrorMessage="Your first and last name must be at least 5 characters long and no more than 100 characters.")]
        public string FullName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter your email address.")]
        [StringLength(200, MinimumLength=4, ErrorMessage="Your email address must be no more than 200 characters long.")]
        [EmailAddress(ErrorMessage="Please enter a valid email address.")]
        public string EmailAddress { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter your ten digit phone number.")]
        [StringLength(20, MinimumLength=10,ErrorMessage="Your phone number must be between 10 and 20 characters long.")]
        [Phone(ErrorMessage="Please enter a valid phone number.")]
        public string PhoneNumber { get; set; }

        [Required]
        public string CardNonce { get; set; }
        #endregion

        public void ExtendExpiration(int minuteExtension = 5)
        {
            // extends the expiration by 5 more minutes
            // get clean copies
            using (SalmonRiverEntities db = new SalmonRiverEntities())
            {
                List<int> ids = Holds.Select(i => i.HoldID).Distinct().ToList();
                List<Hold> freshHolds = db.Holds.Where(i => ids.Contains(i.HoldID)).ToList();

                DateTime newExpiration = DateTime.UtcNow.AddMinutes(minuteExtension);

                freshHolds.ForEach(i =>
                {
                    if (i.Expiration < newExpiration)
                    {
                        i.Expiration = newExpiration;
                    }
                });

                db.SaveChanges();
            }
        }

        public void UpdateTotalCost()
        {
            if (GuestCount == 0 || Holds == null)
            {
                TotalCost = 0;
                SecurityDeposit = 0;
                return;
            }

            using (SalmonRiverEntities db = new SalmonRiverEntities())
            {
                // First Get the default pricing model.
                PricingModel defaultPrice = db.PricingModels.Where(i => i.GuestCount == GuestCount && !i.StayDate.HasValue).OrderByDescending(i => i.PricingModelID).First();

                // Now get all pricing models for the dates selected.
                List<DateTime> selectedDates = Holds.Select(i => i.Date.Date1).Distinct().ToList();
                Dictionary<DateTime, PricingModel> allPricingForDates = db.PricingModels.Where(i => i.GuestCount == GuestCount && i.StayDate.HasValue && selectedDates.Contains(i.StayDate.Value)).ToDictionary(i => i.StayDate.Value);

                decimal totalCost = 0;
                decimal securityDeposit = 0;
                foreach (DateTime selectedDate in selectedDates)
                {
                    if (allPricingForDates.ContainsKey(selectedDate))
                    {
                        totalCost += allPricingForDates[selectedDate].StayCost;

                        if (allPricingForDates[selectedDate].SecurityDeposit > securityDeposit)
                        {
                            securityDeposit = allPricingForDates[selectedDate].SecurityDeposit;
                        }
                    }
                    else
                    {
                        totalCost += defaultPrice.StayCost;

                        if (defaultPrice.SecurityDeposit > securityDeposit)
                        {
                            securityDeposit = defaultPrice.SecurityDeposit;
                        }
                    }
                }

                TotalCost = totalCost;
                SecurityDeposit = securityDeposit;
            }
        }
    }
}