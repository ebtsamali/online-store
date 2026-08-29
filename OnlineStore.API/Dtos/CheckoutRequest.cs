namespace OnlineStore.API.Dtos;

public record CheckoutRequest(
    string ShippingName,
    string ShippingRegion,
    string ShippingCity,
    string ShippingPhone);
