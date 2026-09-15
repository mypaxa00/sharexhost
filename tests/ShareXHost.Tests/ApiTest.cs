// using System.Net.Http.Headers;
// using System.Net.Http.Json;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace ShareXHost.Tests;
//
// public class ApiTests : IClassFixture<CustomWebApplicationFactory>
// {
//     private readonly CustomWebApplicationFactory _factory;
//     private readonly HttpClient _client;
//
//     public ApiTests(CustomWebApplicationFactory factory)
//     {
//         _factory = factory;
//         _client = factory.CreateClient();
//     }
//
//     [Fact(DisplayName = "Upload a file and retrieve it successfully")]
//     public async Task UploadFile()
//     {
//         using MultipartFormDataContent content = new();
//         byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
//         content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
//
//         await AuthAlice();
//
//         string antiForgeryToken = await GetAntiForgeryToken();
//
//         content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
//         HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
//         uploadResponse.EnsureSuccessStatusCode();
//
//         UploadFileResult? uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResult>();
//
//         Assert.NotNull(uploadResult);
//         Assert.NotNull(uploadResult.Url);
//         Assert.NotNull(uploadResult.DeletionUrl);
//
//         using IServiceScope scope = _factory.Services.CreateScope();
//         AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         Guid guid = Guid.Parse(uploadResult.Url.Split('/').Last());
//         string? storagePath =
//             await dbContext.Files.Where(f => f.Id == guid).Select(f => f.StoragePath).FirstOrDefaultAsync();
//
//         Assert.NotNull(storagePath);
//
//         IFileStorage fileStorage = _factory.Services.GetRequiredService<IFileStorage>();
//         using Stream? fileStream = await fileStorage.GetFileAsync(storagePath);
//
//         // compare bytes
//         using MemoryStream memoryStream = new();
//         await fileStream!.CopyToAsync(memoryStream);
//         byte[] retrievedBytes = memoryStream.ToArray();
//
//         Assert.Equal(fileBytes, retrievedBytes);
//         
//         // delete the file
//         HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
//         deleteResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact(DisplayName = "Upload and delete owned file successfully")]
//     public async Task DeleteFile()
//     {
//         using MultipartFormDataContent content = new();
//         byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
//         content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
//
//         await AuthAlice();
//
//         string antiForgeryToken = await GetAntiForgeryToken();
//
//         content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
//         HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
//         uploadResponse.EnsureSuccessStatusCode();
//
//         UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
//
//         using IServiceScope scope = _factory.Services.CreateScope();
//         AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         Guid guid = Guid.Parse(uploadResult.Url!.Split('/').Last());
//         // storage path to verify that the file is deleted from storage after deletion
//         string storagePath = (await dbContext.Files.Where(f => f.Id == guid).Select(f => f.StoragePath).FirstOrDefaultAsync())!;
//
//         // delete the file
//         HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
//         deleteResponse.EnsureSuccessStatusCode();
//         
//         // verify that the file is deleted from the database
//         bool exists = await dbContext.Files
//             .AnyAsync(f => f.Id == guid);
//
//         Assert.False(exists);
//         
//         IFileStorage fileStorage = _factory.Services.GetRequiredService<IFileStorage>();
//         // verify that the file is deleted from storage
//         Stream? fileStream = await fileStorage.GetFileAsync(storagePath);
//         Assert.Null(fileStream);
//     }
//     
//     [Fact(DisplayName = "Upload a file and delete it successfully as guest")]
//     public async Task DeleteFileGuest()
//     {
//         using MultipartFormDataContent content = new();
//         byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
//         content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
//         
//         string antiForgeryToken = await GetAntiForgeryToken();
//
//         content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
//         HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
//         uploadResponse.EnsureSuccessStatusCode();
//
//         UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
//
//         using IServiceScope scope = _factory.Services.CreateScope();
//         AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         Guid guid = Guid.Parse(uploadResult.Url!.Split('/').Last());
//         // storage path to verify that the file is deleted from storage after deletion
//         string storagePath = (await dbContext.Files.Where(f => f.Id == guid).Select(f => f.StoragePath).FirstOrDefaultAsync())!;
//
//         // delete the file
//         HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
//         deleteResponse.EnsureSuccessStatusCode();
//         
//         // verify that the file is deleted from the database
//         bool exists = await dbContext.Files
//             .AnyAsync(f => f.Id == guid);
//
//         Assert.False(exists);
//         
//         IFileStorage fileStorage = _factory.Services.GetRequiredService<IFileStorage>();
//         // verify that the file is deleted from storage
//         Stream? fileStream = await fileStorage.GetFileAsync(storagePath);
//         Assert.Null(fileStream);
//     }
//     
//     [Fact(DisplayName = "Upload and read owned file successfully")]
//     public async Task UploadReadOwnedTest()
//     {
//         using MultipartFormDataContent content = new();
//         byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
//         content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
//         
//         await AuthAlice();
//         string antiForgeryToken = await GetAntiForgeryToken();
//
//         content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
//         HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
//         uploadResponse.EnsureSuccessStatusCode();
//
//         UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
//         
//         // read the file
//         HttpResponseMessage readResponse = await _client.GetAsync(uploadResult.Url);
//         readResponse.EnsureSuccessStatusCode();
//         byte[] readBytes = await readResponse.Content.ReadAsByteArrayAsync();
//         Assert.Equal(fileBytes, readBytes);
//
//         // delete the file
//         HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
//         deleteResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact(DisplayName = "Upload a file and read it successfully as guest")]
//     public async Task UploadReadGuestTest()
//     {
//         using MultipartFormDataContent content = new();
//         byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
//         content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
//         
//         string antiForgeryToken = await GetAntiForgeryToken();
//
//         content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
//         HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
//         uploadResponse.EnsureSuccessStatusCode();
//
//         UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
//         
//         // read the file
//         HttpResponseMessage readResponse = await _client.GetAsync(uploadResult.Url);
//         readResponse.EnsureSuccessStatusCode();
//         byte[] readBytes = await readResponse.Content.ReadAsByteArrayAsync();
//         Assert.Equal(fileBytes, readBytes);
//
//         // delete the file
//         HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
//         deleteResponse.EnsureSuccessStatusCode();
//     }
//
//     private async Task<string> GetAntiForgeryToken()
//     {
//         HttpResponseMessage antiForgeryResponse = await _client.GetAsync("/antiforgery/token");
//         string antiForgeryToken = (await antiForgeryResponse.Content.ReadFromJsonAsync<AntiForgeryTokenResponse>())!
//             .RequestToken;
//         return antiForgeryToken;
//     }
//
//     private async Task AuthAlice()
//     {
//         HttpResponseMessage response = await _client.GetAsync("/dev-token/Alice");
//         string token = (await response.Content.ReadFromJsonAsync<JwtTokenResponse>())!.Token;
//         _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//     }
// }