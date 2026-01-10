using QuestPDF.Infrastructure;
using Supabase;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Giữ nguyên tên thuộc tính như trong Model C#
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
// ✅ THÊM SESSION (BẮT BUỘC)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== SUPABASE CONFIG =====
// ===== ADMIN SUPABASE (SERVICE ROLE) =====
builder.Services.AddSingleton<Supabase.Client>(provider =>
{
    var options = new SupabaseOptions
    {
        AutoConnectRealtime = false
    };

    return new Supabase.Client(
        builder.Configuration["Supabase:Url"],
        builder.Configuration["Supabase:ServiceKey"],
        options
    );
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var supabase = scope.ServiceProvider.GetRequiredService<Supabase.Client>();
    await supabase.InitializeAsync();
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
