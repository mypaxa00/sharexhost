using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ShareXHost.Tests;

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "Upload a file and retrieve it successfully")]
    public async Task UploadFile()
    {
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");

        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);

        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse? uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>();

        Assert.NotNull(uploadResult);
        Assert.NotNull(uploadResult.Url);
        Assert.NotNull(uploadResult.DeletionUrl);

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid guid = Guid.Parse(uploadResult.Url.Split('/').Last());
        // get the storage path and original file name from the database to verify that the file is stored correctly
        var resultedFile = await dbContext.Files
            .Where(f => f.Id == guid)
            .Select(f => new
            {
                path = f.StoragePath,
                name = f.OriginalFileName
            })
            .FirstOrDefaultAsync();

        Assert.NotNull(resultedFile?.path);
        Assert.NotNull(resultedFile.name);
        Assert.Equal("test.txt", resultedFile.name);

        IFileStorage fileStorage = _factory.Services.GetRequiredService<IFileStorage>();
        await using Stream? fileStream = await fileStorage.GetFileAsync(resultedFile.path);

        // compare bytes
        using MemoryStream memoryStream = new();
        await fileStream!.CopyToAsync(memoryStream);
        byte[] retrievedBytes = memoryStream.ToArray();

        Assert.Equal(fileBytes, retrievedBytes);
        
        // delete the file
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
        deleteResponse.EnsureSuccessStatusCode();
    }
    
    [Fact(DisplayName = "Upload and delete owned file successfully")]
    public async Task DeleteFile()
    {
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");

        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);

        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid guid = Guid.Parse(uploadResult.Url.Split('/').Last());
        // storage path to verify that the file is deleted from storage after deletion
        string storagePath = (await dbContext.Files.Where(f => f.Id == guid).Select(f => f.StoragePath).FirstOrDefaultAsync())!;

        // delete the file
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
        deleteResponse.EnsureSuccessStatusCode();
        
        // verify that the file is deleted from the database
        bool exists = await dbContext.Files
            .AnyAsync(f => f.Id == guid);

        Assert.False(exists);
        
        IFileStorage fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        // verify that the file is deleted from storage
        Stream? fileStream = await fileStorage.GetFileAsync(storagePath);
        Assert.Null(fileStream);
    }
    
    [Fact(DisplayName = "Upload a file and delete it successfully as guest")]
    public async Task DeleteFileGuest()
    {
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
        
        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid guid = Guid.Parse(uploadResult.Url.Split('/').Last());
        // storage path to verify that the file is deleted from storage after deletion
        string storagePath = (await dbContext.Files.Where(f => f.Id == guid).Select(f => f.StoragePath).FirstOrDefaultAsync())!;

        // delete the file
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
        deleteResponse.EnsureSuccessStatusCode();
        
        // verify that the file is deleted from the database
        bool exists = await dbContext.Files
            .AnyAsync(f => f.Id == guid);

        Assert.False(exists);
        
        IFileStorage fileStorage = _factory.Services.GetRequiredService<IFileStorage>();
        // verify that the file is deleted from storage
        Stream? fileStream = await fileStorage.GetFileAsync(storagePath);
        Assert.Null(fileStream);
    }
    
    [Fact(DisplayName = "Upload and read owned file successfully")]
    public async Task UploadReadOwnedTest()
    {
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
        
        await AuthUser("Alice");
        
        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
        
        // read the file
        HttpResponseMessage readResponse = await _client.GetAsync(uploadResult.Url);
        readResponse.EnsureSuccessStatusCode();
        byte[] readBytes = await readResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, readBytes);

        // delete the file
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
        deleteResponse.EnsureSuccessStatusCode();
    }
    
    [Fact(DisplayName = "Upload a file and read it successfully as guest")]
    public async Task UploadReadGuestTest()
    {
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
        
        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
        
        // read the file
        HttpResponseMessage readResponse = await _client.GetAsync(uploadResult.Url);
        readResponse.EnsureSuccessStatusCode();
        byte[] readBytes = await readResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, readBytes);

        // delete the file
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(uploadResult.DeletionUrl);
        deleteResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task CreateAsync_HashesPassword()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        IPasswordHasher<User> passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        
        User user = await userService.CreateAsync(nameof(CreateAsync_HashesPassword), "password123", "Test", UserRole.User);

        PasswordVerificationResult result =
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "password123");

        Assert.Equal(PasswordVerificationResult.Success, result);

        PasswordVerificationResult wrongPasswordResult =
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "wrong-password");

        Assert.NotEqual(PasswordVerificationResult.Success, wrongPasswordResult);
    }
    
    [Fact]
    public async Task AuthenticateAsync_VerifiesPassword()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        const string testuser = nameof(AuthenticateAsync_VerifiesPassword);
        const string testuser2 = testuser + "2";
        
        User user = await userService.CreateAsync(testuser, "password123", "Test", UserRole.User);
        _ = await userService.CreateAsync(testuser2, "password321", "Test2", UserRole.User);

        User? authenticatedUser = await userService.AuthenticateAsync(testuser, "password123");
        Assert.NotNull(authenticatedUser);
        Assert.Equal(user.Id, authenticatedUser.Id);
        
        User? wrongUserPassword = await userService.AuthenticateAsync(testuser2, "password123");
        Assert.Null(wrongUserPassword);
        
        User? wrongPasswordUser = await userService.AuthenticateAsync(testuser, "wrong");
        Assert.Null(wrongPasswordUser);
        
        User? nonExistentUser = await userService.AuthenticateAsync("does-not-exist", "password123");
        Assert.Null(nonExistentUser);
    }
    
    [Fact]
    public async Task AuthLoginTest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        const string testuser = nameof(AuthLoginTest);
        User user = await userService.CreateAsync(testuser, "password123", "Test", UserRole.User);
        
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Name = testuser,
            Password = "password123"
        });
        loginResponse.EnsureSuccessStatusCode();
        string jwtToken = (await loginResponse.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        HttpResponseMessage meResponse = await _client.GetAsync("/me");
        meResponse.EnsureSuccessStatusCode();
        MeResponse me = (await meResponse.Content.ReadFromJsonAsync<MeResponse>())!;
        
        Assert.Equal(user.Name, me.Name);
        Assert.Equal(user.Role, Enum.Parse<UserRole>(me.Role));
        
        // unsuccessful login
        HttpResponseMessage badLoginResponse =
            await _client.PostAsJsonAsync("/auth/login", new LoginRequest
            {
                Name = testuser,
                Password = "wrong-password"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, badLoginResponse.StatusCode);
    }
    
    [Fact]
    public async Task UserCreationTest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        
        // create regular user
        const string testuser = nameof(UserCreationTest) + "User";
        await userService.CreateAsync(testuser, "password123", "Test", UserRole.User);
        // create an admin user
        const string adminuser = nameof(UserCreationTest) + "Admin";
        await userService.CreateAsync(adminuser, "adminpassword", "Admin", UserRole.Admin);

        const string newuser = nameof(UserCreationTest) + "NewUser";
        CreateUserRequest createUserRequest = new()
        {
            UserName = newuser,
            Password = "password123",
            Name = "New User",
            Role = UserRole.User
        };
        
        // test /admin/users endpoint
        // try to create user without authentication
        HttpResponseMessage unauthenticatedResponse = await _client.PostAsJsonAsync("/admin/users",
            createUserRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);
        User? potentialUser = await userService.FindByUserNameAsync(newuser);
        Assert.Null(potentialUser);
        
        // authenticate as testuser
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Name = testuser,
            Password = "password123"
        });
        loginResponse.EnsureSuccessStatusCode();
        string jwtToken = (await loginResponse.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        
        // try to create a new user as a non-admin
        HttpResponseMessage createUserResponse = await _client.PostAsJsonAsync("/admin/users",
            createUserRequest);
        Assert.Equal(HttpStatusCode.Forbidden, createUserResponse.StatusCode);
        potentialUser = await userService.FindByUserNameAsync(newuser);
        Assert.Null(potentialUser);
        
        // authenticate as admin user
        HttpResponseMessage adminLoginResponse = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Name = adminuser,
            Password = "adminpassword"
        });
        adminLoginResponse.EnsureSuccessStatusCode();
        string adminJwtToken = (await adminLoginResponse.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminJwtToken);

        // try to create a new user as an admin
        HttpResponseMessage createUserAsAdminResponse = await _client.PostAsJsonAsync("/admin/users",
            createUserRequest);
        Assert.Equal(HttpStatusCode.Created, createUserAsAdminResponse.StatusCode);
        potentialUser = await userService.FindByUserNameAsync(newuser);
        Assert.NotNull(potentialUser);
        
        // try to create a user with an existing username
        HttpResponseMessage createUserWithExistingUsernameResponse = await _client.PostAsJsonAsync("/admin/users",
            createUserRequest);
        Assert.Equal(HttpStatusCode.Conflict, createUserWithExistingUsernameResponse.StatusCode);
    }
    
    [Fact]
    public async Task MineFilesTest()
    {
        HttpResponseMessage anonymousResponse = await _client.GetAsync("/files/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        
        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);
        
        string antiForgeryToken = await GetAntiForgeryToken();

        // Upload files as Alice
        const int filesCount = 7;
        for (int i = 0; i < filesCount; i++)
        {
            using MultipartFormDataContent content = new();
            byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
            content.Add(new ByteArrayContent(fileBytes), "file", $"test{i}.txt");
            content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
            HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
            uploadResponse.EnsureSuccessStatusCode();
        }

        // Get Alice's files
        HttpResponseMessage mineResponse = await _client.GetAsync("/files/mine?page=1&pageSize=5");
        mineResponse.EnsureSuccessStatusCode();
        PaginatedResponse<FileResponse> mineFilesResponse =
            (await mineResponse.Content.ReadFromJsonAsync<PaginatedResponse<FileResponse>>())!;
        
        Assert.Equal(filesCount, mineFilesResponse.TotalCount);
        Assert.Equal(5, mineFilesResponse.Items.Count);
        // Verify that the files are ordered newest → oldest
        for (int i = 0; i < mineFilesResponse.Items.Count - 1; i++)
        {
            DateTimeOffset current = mineFilesResponse.Items[i].CreatedAt;
            DateTimeOffset next = mineFilesResponse.Items[i + 1].CreatedAt;
            Assert.True(current >= next, $"File at index {i} is not newer than file at index {i + 1}");
        }
        FileResponse lastFileOnPage1 = mineFilesResponse.Items.Last();
        
        mineResponse = await _client.GetAsync("/files/mine?page=2&pageSize=5");
        mineResponse.EnsureSuccessStatusCode();
        mineFilesResponse = (await mineResponse.Content.ReadFromJsonAsync<PaginatedResponse<FileResponse>>())!;
        
        Assert.Equal(filesCount, mineFilesResponse.TotalCount);
        Assert.Equal(2, mineFilesResponse.Items.Count);
        Assert.True(lastFileOnPage1.CreatedAt >= mineFilesResponse.Items[0].CreatedAt);
        // Verify that the files are ordered newest → oldest
        for (int i = 0; i < mineFilesResponse.Items.Count - 1; i++)
        {
            DateTimeOffset current = mineFilesResponse.Items[i].CreatedAt;
            DateTimeOffset next = mineFilesResponse.Items[i + 1].CreatedAt;
            Assert.True(current >= next, $"File at index {i} is not newer than file at index {i + 1}");
        }
        
        // Upload 2 files as Bob
        await AuthUser("Bob", scope: scope);
        antiForgeryToken = await GetAntiForgeryToken();
        
        for (int i = 0; i < 2; i++)
        {
            using MultipartFormDataContent content = new();
            byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
            content.Add(new ByteArrayContent(fileBytes), "file", $"bob_test{i}.txt");
            content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
            HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
            uploadResponse.EnsureSuccessStatusCode();
        }
        
        // Get Bob's files
        mineResponse = await _client.GetAsync("/files/mine?page=1&pageSize=5");
        mineResponse.EnsureSuccessStatusCode();
        mineFilesResponse = (await mineResponse.Content.ReadFromJsonAsync<PaginatedResponse<FileResponse>>())!;
        
        Assert.Equal(2, mineFilesResponse.TotalCount);
        Assert.Equal(2, mineFilesResponse.Items.Count);
    }

    [Fact]
    public async Task AuthTokenTest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);
        
        HttpResponseMessage tokenResponse = await _client.PostAsync("/auth/tokens", JsonContent.Create(new CreateApiTokenRequest()
        {
            Name = "Test Token"
        }));
        tokenResponse.EnsureSuccessStatusCode();
        
        string token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        
        Assert.False(string.IsNullOrEmpty(token));
        Assert.StartsWith("shx_", token);
        
        ApiToken apiToken = await scope.ServiceProvider
            .GetRequiredService<AppDbContext>()
            .ApiTokens
            .AsNoTracking()
            .SingleAsync(x => x.Name == "Test Token");
        
        Assert.NotEqual(token, apiToken.TokenHash);
        Assert.Equal(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)),
            Convert.FromBase64String(apiToken.TokenHash));
        
        IApiTokenService apiTokenService =
            scope.ServiceProvider.GetRequiredService<IApiTokenService>();

        Guid userId = (await scope.ServiceProvider.GetRequiredService<IUserService>().FindByUserNameAsync("Alice"))!.Id;
        Guid? authenticatedUserId = await apiTokenService.AuthenticateAsync(token);

        Assert.NotNull(authenticatedUserId);
        Assert.Equal(userId, authenticatedUserId);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage adminUsersResponse = await _client.PostAsync("/admin/users", JsonContent.Create(new CreateUserRequest
        {
            UserName = "Bob",
            Password = "password123",
            Name = "Bob",
            Role = UserRole.User
        }));
        Assert.Equal(HttpStatusCode.Forbidden, adminUsersResponse.StatusCode);

        HttpResponseMessage response = await _client.GetAsync("/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        // verify that the token can be used to authenticate and access /files /links endpoints
        using MultipartFormDataContent content = new();
        byte[] fileBytes = [.. "Hello, ShareXHost's World!"u8];
        content.Add(new ByteArrayContent(fileBytes), "file", "test.txt");
        
        string antiForgeryToken = await GetAntiForgeryToken();

        content.Headers.Add("X-XSRF-TOKEN", antiForgeryToken);
        HttpResponseMessage uploadResponse = await _client.PostAsync("/files", content);
        uploadResponse.EnsureSuccessStatusCode();

        UploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!;
        
        Assert.NotNull(uploadResult);
        Assert.NotNull(uploadResult.Url);
        Assert.NotNull(uploadResult.DeletionUrl);
        
        HttpResponseMessage linkResponse = await _client.PostAsJsonAsync(
            "/links",
            new CreateLinkRequest
            {
                Url = "https://example.com"
            });

        linkResponse.EnsureSuccessStatusCode();

        UploadResponse linkResult =
            (await linkResponse.Content.ReadFromJsonAsync<UploadResponse>())!;

        Assert.NotNull(linkResult);
        Assert.NotNull(linkResult.Url);
        Assert.NotNull(linkResult.DeletionUrl);
    }

    [Fact]
    public async Task AuthOwnedTokensTest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);

        HttpResponseMessage tokenResponse1 = await _client.PostAsync("/auth/tokens", JsonContent.Create(new CreateApiTokenRequest()
        {
            Name = "Alice's Token 1"
        }));
        tokenResponse1.EnsureSuccessStatusCode();
        
        HttpResponseMessage tokenResponse2 = await _client.GetAsync("/auth/tokens");
        tokenResponse2.EnsureSuccessStatusCode();
        
        PaginatedResponse<GetApiTokenResult> tokens = (await tokenResponse2.Content.ReadFromJsonAsync<PaginatedResponse<GetApiTokenResult>>())!;
        Assert.Single(tokens.Items);
        Assert.Equal("Alice's Token 1", tokens.Items[0].Name);
        
        string token = (await tokenResponse1.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        HttpResponseMessage tokenResponse3 = await _client.GetAsync("/auth/tokens");
        Assert.Equal(HttpStatusCode.Forbidden, tokenResponse3.StatusCode);

        await AuthUser("Bob", scope: scope);
        
        HttpResponseMessage tokenResponse4 = await _client.PostAsync("/auth/tokens", JsonContent.Create(new CreateApiTokenRequest()
        {
            Name = "Bob's Token 1"
        }));
        tokenResponse4.EnsureSuccessStatusCode();
        
        HttpResponseMessage tokenResponse5 = await _client.GetAsync("/auth/tokens");
        tokenResponse5.EnsureSuccessStatusCode();
        
        PaginatedResponse<GetApiTokenResult> tokensBob = (await tokenResponse5.Content.ReadFromJsonAsync<PaginatedResponse<GetApiTokenResult>>())!;
        Assert.Single(tokensBob.Items);
        Assert.Equal("Bob's Token 1", tokensBob.Items[0].Name);
    }

    [Fact]
    public async Task AuthDeleteTokenTest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await AuthUser("Alice", scope: scope);
        
        HttpResponseMessage tokenResponse1 = await _client.PostAsync("/auth/tokens", JsonContent.Create(new CreateApiTokenRequest()
        {
            Name = "Alice's Token 1"
        }));
        tokenResponse1.EnsureSuccessStatusCode();
        
        string aliceToken = (await tokenResponse1.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        
        // get Alice's tokens
        HttpResponseMessage getAliceTokensResponse = await _client.GetAsync("/auth/tokens?page=1&pageSize=1");
        getAliceTokensResponse.EnsureSuccessStatusCode();
        PaginatedResponse<GetApiTokenResult> aliceTokens =
            (await getAliceTokensResponse.Content.ReadFromJsonAsync<PaginatedResponse<GetApiTokenResult>>())!;
        
        await AuthUser("Bob", scope: scope);
        
        HttpResponseMessage tokenResponse2 = await _client.PostAsync("/auth/tokens", JsonContent.Create(new CreateApiTokenRequest()
        {
            Name = "Bob's Token 1"
        }));
        tokenResponse2.EnsureSuccessStatusCode();
        
        // get Bob's tokens
        HttpResponseMessage getBobsTokensResponse = await _client.GetAsync("/auth/tokens?page=1&pageSize=1");
        getBobsTokensResponse.EnsureSuccessStatusCode();
        PaginatedResponse<GetApiTokenResult> bobsTokens =
            (await getBobsTokensResponse.Content.ReadFromJsonAsync<PaginatedResponse<GetApiTokenResult>>())!;
        
        await AuthUser("Alice", scope: scope);
        
        // Alice tries to delete Bob's token
        HttpResponseMessage deleteBobsTokenResponse = await _client.DeleteAsync($"/auth/tokens/{bobsTokens.Items[0].Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteBobsTokenResponse.StatusCode);
        
        // Alice deletes her own token
        HttpResponseMessage deleteAliceTokenResponse = await _client.DeleteAsync($"/auth/tokens/{aliceTokens.Items[0].Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAliceTokenResponse.StatusCode);
        
        // after deletion, Alice's token should no longer work
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);
        // try post link
        HttpResponseMessage postLinkResponse = await _client.PostAsJsonAsync("/links", new CreateLinkRequest
        {
            Url = "https://example.com"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, postLinkResponse.StatusCode);
        
        // auth with invalid jwt token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        postLinkResponse = await _client.PostAsJsonAsync("/links", new CreateLinkRequest
        {
            Url = "https://example.com"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, postLinkResponse.StatusCode);
    }

    private async Task<string> GetAntiForgeryToken()
    {
        HttpResponseMessage antiForgeryResponse = await _client.GetAsync("/antiforgery/token");
        string antiForgeryToken = (await antiForgeryResponse.Content.ReadFromJsonAsync<AntiForgeryTokenResponse>())!
            .RequestToken;
        return antiForgeryToken;
    }
    
    private async Task AuthUser(string userName, string password = "password123", IServiceScope? scope = null)
    {
        IServiceScope currentScope = scope ?? _factory.Services.CreateScope();
        
        IUserService userService = currentScope.ServiceProvider.GetRequiredService<IUserService>();
        User? user = await userService.FindByUserNameAsync(userName);
        if (user is null) await userService.CreateAsync(userName, password, userName, UserRole.User);

        HttpResponseMessage response = await _client.PostAsync("/auth/login", JsonContent.Create(new LoginRequest
        {
            Name = userName,
            Password = password
        }));
        response.EnsureSuccessStatusCode();
        
        string token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        if (scope is null) currentScope.Dispose();
    }
}