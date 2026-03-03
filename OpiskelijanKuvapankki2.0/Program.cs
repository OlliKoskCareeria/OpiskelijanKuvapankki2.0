
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpiskelijanKuvaPankki.Services;
using OpiskelijanKuvapankki2_0.Filters;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);



Log.Logger = new LoggerConfiguration() //Määritellään lokikirjoituksen asetukset Vain ILoggerin tuottamat information ja error viestit lokiin tässä vaiheessa
    .MinimumLevel.Information() 
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) 
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning) 
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OpiskelijanKuvapankki")
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .Enrich.WithProperty("Application", "OpiskelijanKuvapankki")
//    .WriteTo.Console()
//    .WriteTo.File(
//        path: "Logs/app-.log",
//        rollingInterval: RollingInterval.Day,
//        retainedFileCountLimit: 14,
//        restrictedToMinimumLevel: LogEventLevel.Information)
//    .CreateLogger();

//builder.Host.UseSerilog(Log.Logger);

//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddDebug();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<EnumSchemaFilter>();
    c.OperationFilter<RecaptchaHeaderParameter>();

    //c.AddSecurityDefinition("RecaptchaToken", new OpenApiSecurityScheme
    //{
    //    Name = "X-Recaptcha-Token",
    //    Type = SecuritySchemeType.ApiKey,
    //    In = ParameterLocation.Header,
    //    Description = "Google reCAPTCHA token"

});

builder.Services.AddCors(options =>
{
    options.AddPolicy("all",
    builder => builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
});

builder.Services.AddDbContext<OpiskelijanKuvapankki2_0Context>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("local")
    ));

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);

var appSettings = appSettingsSection.Get<AppSettings>();
var key = Encoding.ASCII.GetBytes(appSettings.Key);

builder.Services.AddAuthentication(au =>
{
    au.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    au.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwt =>
{
    jwt.RequireHttpsMetadata = false;
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddScoped<ImageSharpService>();

builder.Services.AddScoped<ImageService>();

builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<ICleanUpService, CleanUpService>();

builder.Services.AddHostedService<BackGroundCleanUpService>();

builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();

var emailProvider = "MailJet";
    
if (emailProvider == "SendGrid")
{
    builder.Services.Configure<SendGridSettings>
        (builder.Configuration.GetSection("SendGrid"));
    builder.Services.AddTransient<IEmailService, SendGridService>();
}
else if (emailProvider == "MailJet")
{
    builder.Services.Configure<MailJetSettings>
         (builder.Configuration.GetSection("MailJet"));
    builder.Services.AddTransient<IEmailService, MailJetService>();
}
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        var httpContext = context.HttpContext;

        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var endpoint = httpContext.Request.Path;

        var logger = httpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning(
            "Rate limit ylitetty. IP: {IP}, Endpoint: {Endpoint}",
            ip,
            endpoint
        );

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await httpContext.Response.WriteAsync("Too many requests", token);
    };

    options.AddFixedWindowLimiter("fixed", o =>
    {
        o.PermitLimit = 20;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    options.AddPolicy("FixedForIp", HttpContent =>
            RateLimitPartition.GetFixedWindowLimiter(
                HttpContent.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(60),
                    QueueLimit = 0
                }));

    options.AddPolicy("ResetPasswordPolicy", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));
});





var app = builder.Build();

app.UseRateLimiter();

//app.UseSerilogRequestLogging(options =>
//{
   
//    options.MessageTemplate =
//        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
//});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}
app.UseCors("all");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
