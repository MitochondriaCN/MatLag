using MatLag.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MatrixDemoService>();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapPost("/api/ref", (RefRequest request, MatrixDemoService service) =>
{
    try
    {
        return Results.Ok(service.Calculate(request));
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (ArithmeticException exception)
    {
        return Results.UnprocessableEntity(new { error = exception.Message });
    }
});
app.Run();
