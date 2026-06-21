using System;

namespace ClashZone.App.ViewModels
{
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ClashCoins { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public string? ShippingFullName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingStreet { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingCountry { get; set; }

        public bool HasShippingAddress =>
            !string.IsNullOrWhiteSpace(ShippingFullName)
            || !string.IsNullOrWhiteSpace(ShippingStreet)
            || !string.IsNullOrWhiteSpace(ShippingCity);
    }
}
