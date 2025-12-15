using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urkey.Core.Models
{
    public class User
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string HashPassword { get; set; } = string.Empty;
        public string Salt  { get; set; } = string.Empty;
        public enum SubscriptionType
        {
            Free,
            Guest,
            Paid,
        }
        public SubscriptionType Subscription = SubscriptionType.Guest;
        public DateOnly FirstOpenDate { get; set; }
        public DateOnly? StartSubscriptionDate  { get; set; }
        public DateOnly? EndSubscriptionDate  { get; set; }
    }
}
