using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalInmobiliario.Data;
using Microsoft.Extensions.Caching.StackExchangeRedis;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services.ConfigureApplicationCookie(o =>
{
    o.AccessDeniedPath = "/Home/AccesoDenegado";
});
var redisCnx = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisCnx))
{
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisCnx);
}
else
{
    builder.Services.AddDistributedMemoryCache();  // fallback sin Redis
}

// Session
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor(); 
var app = builder.Build();

// SEED al iniciar (top-level admite await)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
     // 1) Migrar la base (crea AspNetUsers, AspNetRoles, etc.)
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Inmuebles.AnyAsync())
{
    await Seed.RunAsync(db);
   // o el nombre que tengas: Seed.Cargar(db) / Seed.Inicializar(db)
    await db.SaveChangesAsync();
}
    // 1) Crear rol "Broker" si no existe
    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    if (!await roleMgr.RoleExistsAsync("Broker"))
        await roleMgr.CreateAsync(new IdentityRole("Broker"));

    // 2) Crear usuario demo y agregarlo al rol Broker
    var userMgr = services.GetRequiredService<UserManager<IdentityUser>>();
    var email = "broker@demo.local";
    var user = await userMgr.FindByEmailAsync(email);
    if (user is null)
    {
        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        await userMgr.CreateAsync(user, "Pass123$"); // contraseña de prueba
    }
    if (!await userMgr.IsInRoleAsync(user, "Broker"))
        await userMgr.AddToRoleAsync(user, "Broker");
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
