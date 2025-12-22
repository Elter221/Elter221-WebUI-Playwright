using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using WebAPITests.Helpers;
using WebAPITests.Models;
using Xunit;

namespace WebAPITests
{
    public class TestFixture : IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; }
        public AuthHelper AuthHelper { get; private set; }
        public IConfiguration Configuration { get; private set; }
        public ILoggerFactory LoggerFactory { get; private set; }

        private readonly List<string> _createdBookIds = [];
        private readonly List<string> _createdUserIds = [];
        private string _accessToken = string.Empty;
        private readonly JsonSerializerOptions jsonOptions;

        public TestFixture()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .AddHttpClient()
                .AddSingleton<IConfiguration>(Configuration)
                .AddSingleton<AuthHelper>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            HttpClient = httpClientFactory.CreateClient();
            HttpClient.Timeout = TimeSpan.FromSeconds(30);

            AuthHelper = serviceProvider.GetRequiredService<AuthHelper>();

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task InitializeAsync()
        {
            _accessToken = await AuthHelper.GetAccessTokenAsync();
            HttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task<string> CreateTestBookAsync()
        {
            var book = new CreateBookRequest
            {
                Title = $"Test Book {Guid.NewGuid()}",
                Author = "Test Author",
                ISBN = $"978-{Random.Shared.Next(100000000, 999999999)}",
                PublishedDate = DateTime.UtcNow.AddYears(-10)
            };

            var response = await PostAsync($"/Books", book);
            var createdBook = await DeserializeResponseAsync<Book>(response);
            _createdBookIds.Add(createdBook!.Id);
            return createdBook.Id;
        }

        public async Task<string> CreateTestUserAsync()
        {
            var user = new
            {
                name = $"Test User {Guid.NewGuid()}",
                email = $"testuser{Guid.NewGuid():N}@example.com"
            };

            var response = await PostAsync($"/Users", user);
            var createdUser = await DeserializeResponseAsync<dynamic>(response);
            string userId = createdUser!.GetProperty("id").GetString()!;
            _createdUserIds.Add(userId);
            return userId;
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await HttpClient.GetAsync($"{Configuration["ApiSettings:BaseUrl"]}{endpoint}");
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await HttpClient.PostAsync($"{Configuration["ApiSettings:BaseUrl"]}{endpoint}", content);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await HttpClient.PutAsync($"{Configuration["ApiSettings:BaseUrl"]}{endpoint}", content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await HttpClient.DeleteAsync($"{Configuration["ApiSettings:BaseUrl"]}{endpoint}");
        }

        public async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, jsonOptions);
        }

        public async Task CleanupAsync()
        {
            foreach (var bookId in _createdBookIds)
            {
                await DeleteAsync($"/Books/{bookId}");
            }

            foreach (var userId in _createdUserIds)
            {
                await DeleteAsync($"/Users/{userId}");
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
