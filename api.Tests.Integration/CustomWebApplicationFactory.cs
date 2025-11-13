using api.Data;
using api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq; // Required for Mock
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    // Make the mock collection accessible for tests so we can verify interactions
    public Mock<IMongoCollection<QuarterlyUpdate>> MockQuarterlyUpdatesCollection { get; private set; } = null!;

    public void ResetDatabase()
    {
        using (var scope = Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            // Ensure the in-memory database is completely reset for isolation
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Reset the mock for MongoDB collection to clear all previous setups
            MockQuarterlyUpdatesCollection.Reset();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing ApplicationDbContext registration if any,
            // to replace it with an in-memory database
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            // Add ApplicationDbContext using an in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Remove existing MongoDB registrations
            services.RemoveAll(typeof(IMongoClient));
            services.RemoveAll(typeof(IMongoDatabase));
            services.RemoveAll(typeof(IMongoCollection<QuarterlyUpdate>));

            // Configure a mock MongoDB collection and register it as a singleton
            MockQuarterlyUpdatesCollection = new Mock<IMongoCollection<QuarterlyUpdate>>();
            services.AddSingleton(MockQuarterlyUpdatesCollection.Object);
        });

        // Set the environment to IntegrationTests to prevent SQL Server DbContext registration
        builder.UseEnvironment("IntegrationTests");
    }
}
