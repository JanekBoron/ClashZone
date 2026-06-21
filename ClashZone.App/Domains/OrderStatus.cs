using System;
using System.Collections.Generic;
using System.Linq;

namespace ClashZone.App.Domains
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Pending, Processing, Completed, Cancelled
        };

        public static bool IsValid(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            All.Contains(status, StringComparer.OrdinalIgnoreCase);

        public static string Label(string? status) => status switch
        {
            Pending => "Oczekujące",
            Processing => "W realizacji",
            Completed => "Zrealizowane",
            Cancelled => "Anulowane",
            _ => status ?? string.Empty
        };

        public static string BadgeClass(string? status) => status switch
        {
            Pending => "bg-secondary",
            Processing => "bg-warning text-dark",
            Completed => "bg-success",
            Cancelled => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
