using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Planora.IntegrationTests;

/// <summary>
/// Walks the real assignment path — register leader and member, share a project, create a
/// task, assign it — and asserts the member receives an email authored by the leader.
/// </summary>
public sealed class TaskAssignmentEmailFlowTests
{
    [Fact]
    public async Task AssignTask_ToMemberWhoWantsEmails_DeliversMailAuthoredByTheAssigner()
    {
        await using var smtpSink = SmtpMessageSink.Start();
        await using var factory = CreateFactory(smtpSink.Port);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var leader = await RegisterAccountAsync(client, "leader");
        var member = await RegisterAccountAsync(client, "member");
        await EnableEmailNotificationsAsync(client, member);

        var projectId = await CreateProjectAsync(client, leader);
        var roleId = await GetOwnerlessRoleIdAsync(client, leader, projectId);
        var invitationId = await InviteAsync(client, leader, projectId, member.Email, roleId);
        await AcceptInvitationAsync(client, member, invitationId);

        var membershipId = await GetMembershipIdAsync(client, leader, projectId, member.UserId);
        var taskId = await CreateTaskAsync(client, leader, projectId);
        await AssignAsync(client, leader, taskId, membershipId);

        var payload = await smtpSink.WaitForMessageAsync(TimeSpan.FromSeconds(15));
        Assert.Contains("no-reply@planora.test", payload, StringComparison.Ordinal);
        Assert.Contains("via Planora", payload, StringComparison.Ordinal);
        Assert.Contains(leader.Email, payload, StringComparison.Ordinal);
        Assert.Contains(member.Email, payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssignTask_ToMemberWhoDisabledEmails_SendsNothing()
    {
        await using var smtpSink = SmtpMessageSink.Start();
        await using var factory = CreateFactory(smtpSink.Port);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var leader = await RegisterAccountAsync(client, "leader");
        var member = await RegisterAccountAsync(client, "optout");
        await DisableEmailNotificationsAsync(client, member);

        var projectId = await CreateProjectAsync(client, leader);
        var roleId = await GetOwnerlessRoleIdAsync(client, leader, projectId);
        var invitationId = await InviteAsync(client, leader, projectId, member.Email, roleId);
        await AcceptInvitationAsync(client, member, invitationId);

        var membershipId = await GetMembershipIdAsync(client, leader, projectId, member.UserId);
        var taskId = await CreateTaskAsync(client, leader, projectId);
        await AssignAsync(client, leader, taskId, membershipId);

        await Assert.ThrowsAnyAsync<Exception>(() => smtpSink.WaitForMessageAsync(TimeSpan.FromSeconds(3)));
    }

    private static WebApplicationFactory<Program> CreateFactory(int smtpPort) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaskEmailNotifications:SmtpHost"] = "127.0.0.1",
                ["TaskEmailNotifications:SmtpPort"] = smtpPort.ToString(),
                ["TaskEmailNotifications:EnableSsl"] = "false",
                ["TaskEmailNotifications:FromAddress"] = "no-reply@planora.test",
                ["TaskEmailNotifications:FromNameSuffix"] = "via Planora",
                ["TaskEmailNotifications:FrontendBaseUrl"] = "http://localhost:4200"
            }));
        });

    private static async Task<Account> RegisterAccountAsync(HttpClient client, string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"planora.{prefix}.{suffix}@gmail.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Username = $"{prefix}_{suffix}",
            Password = "Strong-pass-2026",
            DisplayName = $"Planora {prefix} {suffix}",
            AcceptedTerms = true,
            DeviceInfo = "task-email-flow-test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationDescriptor>();
        return new Account(authentication!.UserId, email, authentication.AccessToken);
    }

    private static async Task DisableEmailNotificationsAsync(HttpClient client, Account account)
    {
        using var request = Authorized(HttpMethod.Put, "/api/profile/preferences", account);
        request.Content = JsonContent.Create(new
        {
            PreferredLanguage = "vi",
            ThemePreference = "calm",
            TimeZoneId = "Asia/Ho_Chi_Minh",
            EmailTaskNotificationsEnabled = false
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task EnableEmailNotificationsAsync(HttpClient client, Account account)
    {
        using var request = Authorized(HttpMethod.Put, "/api/profile/preferences", account);
        request.Content = JsonContent.Create(new
        {
            PreferredLanguage = "vi",
            ThemePreference = "calm",
            TimeZoneId = "Asia/Ho_Chi_Minh",
            EmailTaskNotificationsEnabled = true
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Account leader)
    {
        using var request = Authorized(HttpMethod.Post, "/api/projects", leader);
        request.Content = JsonContent.Create(new { Name = "Email flow project", Description = "Task email verification" });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<IdDescriptor>();
        return project!.Id;
    }

    private static async Task<Guid> GetOwnerlessRoleIdAsync(HttpClient client, Account leader, Guid projectId)
    {
        using var request = Authorized(HttpMethod.Get, $"/api/projects/{projectId}/roles", leader);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<RoleDescriptor[]>();
        var role = roles!.FirstOrDefault(candidate => !candidate.Code.Contains("owner", StringComparison.OrdinalIgnoreCase)) ?? roles![0];
        return role.Id;
    }

    private static async Task<Guid> InviteAsync(HttpClient client, Account leader, Guid projectId, string email, Guid roleId)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/projects/{projectId}/invitations", leader);
        request.Content = JsonContent.Create(new { Email = email, RoleId = roleId, ExpiresInDays = 7 });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invitation = await response.Content.ReadFromJsonAsync<IdDescriptor>();
        return invitation!.Id;
    }

    private static async Task AcceptInvitationAsync(HttpClient client, Account member, Guid invitationId)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/project-invitations/{invitationId}/accept", member);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<Guid> GetMembershipIdAsync(HttpClient client, Account leader, Guid projectId, Guid memberUserId)
    {
        using var request = Authorized(HttpMethod.Get, $"/api/projects/{projectId}/members", leader);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<MemberDescriptor[]>();
        return Assert.Single(members!, candidate => candidate.UserId == memberUserId).MembershipId;
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, Account leader, Guid projectId)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/projects/{projectId}/tasks", leader);
        request.Content = JsonContent.Create(new
        {
            SprintId = (Guid?)null,
            Title = "Thiết kế ERD",
            Description = "Chuẩn hoá lược đồ dữ liệu.",
            Priority = "High",
            DueAt = DateTimeOffset.UtcNow.AddDays(5),
            AcceptanceCriteria = new[] { "Có sơ đồ quan hệ" },
            Type = "Documentation",
            SubmissionRequirement = "Any"
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<IdDescriptor>();
        return task!.Id;
    }

    private static async Task AssignAsync(HttpClient client, Account leader, Guid taskId, Guid membershipId)
    {
        using var request = Authorized(HttpMethod.Post, $"/api/tasks/{taskId}/assignees", leader);
        request.Content = JsonContent.Create(new { ProjectMemberId = membershipId });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, Account account)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
        return request;
    }

    private sealed record Account(Guid UserId, string Email, string AccessToken);
    private sealed record AuthenticationDescriptor(Guid UserId, string AccessToken);
    private sealed record IdDescriptor(Guid Id);
    private sealed record RoleDescriptor(Guid Id, string Code);
    private sealed record MemberDescriptor(Guid MembershipId, Guid UserId);
}
