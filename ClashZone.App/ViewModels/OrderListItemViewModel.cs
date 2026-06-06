using System;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// Pozycja listy zamówień w panelu administratora — łączy dane realizacji
    /// nagrody (ProductRedeem) z nazwą produktu i loginem zamawiającego.
    /// </summary>
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ClashCoins { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
