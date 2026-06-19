using Microsoft.EntityFrameworkCore;
using TodoList.Components;

var builder = WebApplication.CreateBuilder(args);

// 1. Dodavanje servisa kontejneru (Očišćeno od duplog koda)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// aplikacija i lokalno i na webu čita datoteku todo.db iz mape same aplikacije.
// 2. Registracija SQLite baze podataka kompatibilna s Dockerom
builder.Services.AddDbContextFactory<TodoDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Lokalni rad u Visual Studiju
        string fiksnaLokalnaPutanja = @"C:\Users\Miljenko Temer\source\repos\TodoList\TodoList\todonova.db";
        options.UseSqlite($"Data Source={fiksnaLokalnaPutanja}");
    }
    else
    {
        // NA RAILWAYU (Docker Linux): Čitamo iz mape aplikacije bez fiksnih Windows staza
        string prodDbPath = Path.Combine(AppContext.BaseDirectory, "todonova.db");
        options.UseSqlite($"Data Source={prodDbPath}");
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TodoList.Client._Imports).Assembly);

// PRIVREMENI KOD ZA PRISILNO ČIŠĆENJE WEB BAZE
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TodoDbContext>>();
    using var context = dbFactory.CreateDbContext();

    // Pokrećemo ovaj kod SAMO na Railwayu (u produkciji)
    if (!app.Environment.IsDevelopment())
    {
        // 1. Brišemo apsolutno sve stare prastare zapise iz tablice
        context.Database.ExecuteSqlRaw("DELETE FROM Todos;");

        // 2. Ručno ubacujemo vašu jednu jedinu ispravnu stavku s računala
        // (Promijenite tekstove ispod ako se vaša stavka zove drugačije)
        context.Database.ExecuteSqlRaw(
            "INSERT INTO Todos (Title, Sifra, IsDone) VALUES ('iunjimiu', 'AA-BB-01', 0);"
        );
    }
}

// Vraćamo automatsku migraciju kako bi Railway primijenio fiksni kod iz DbContexta
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    dbContext.Database.Migrate();
}

app.Run();