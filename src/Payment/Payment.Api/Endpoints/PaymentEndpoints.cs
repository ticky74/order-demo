// src/Payment/Payment.Api/Endpoints/PaymentEndpoints.cs
namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "payment-api", status = "running" }));
        return app;
    }
}
