using Venus.Builder;
using Venus.Classes;
using Venus.DependencyResolver;

public class Program
{
    public static void Main(string[] args)
    {
        var webBuilder = new BuilderApplication(args, builder =>
        {
            // Add services to the container.
            builder.Services.AddRazorPages();
        });

        // Configure the HTTP request pipeline.
        if (!webBuilder.App.Environment.IsDevelopment())
        {
            webBuilder.App.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            webBuilder.App.UseHsts();
        }

        webBuilder.App.UseHttpsRedirection();
        webBuilder.App.UseStaticFiles();
        webBuilder.App.UseRouting();
        webBuilder.App.UseAuthorization();
        webBuilder.UseDependencyInjection();

        webBuilder.GetInstance(typeof(Dependent));

        webBuilder.App.Run();
    }
}