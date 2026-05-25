using System.Net;
using System.Text.RegularExpressions;
using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRReserveSystem.Tests;

public class HrIntegrationTests
{
    private sealed record InterviewPostData(
        int InterviewId,
        int ApplicationId,
        string CandidateName,
        string CandidateEmail,
        string VacancyTitle,
        int? RecruiterId,
        string? RecruiterEmail);

    [Fact]
    public async Task Project_Starts_And_Login_Page_Loads()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Works()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await LoginAsync(client, "admin", "admin123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString ?? "/");
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Does_Not_Work()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await LoginAsync(client, "admin", "wrong-password");
        var dashboardResponse = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, dashboardResponse.StatusCode);
        Assert.Contains("/Account/Login", dashboardResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Recruiter_Cannot_Open_Admin_Pages()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await client.GetAsync("/Recruiters");

        AssertAccessDeniedRedirect(response);
    }

    [Fact]
    public async Task Logout_Get_Is_Not_Allowed_And_Does_Not_Sign_Out()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "admin", "admin123");

        var logoutResponse = await client.GetAsync("/Account/Logout");
        var dashboardResponse = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
    }

    [Fact]
    public async Task Interviewer_Cannot_Open_Candidates()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "interviewer", "interviewer123");

        var response = await client.GetAsync("/Candidates");

        AssertAccessDeniedRedirect(response);
    }

    [Fact]
    public async Task Interviewer_Dashboard_Does_Not_Render_Hiring_Links()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "interviewer", "interviewer123");

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("href=\"/Candidates", html);
        Assert.DoesNotContain("href=\"/Vacancies", html);
        Assert.DoesNotContain("href=\"/Applications", html);
        Assert.Contains("href=\"/InterviewFeedbacks", html);
        Assert.Contains("href=\"/SoftSkillAssessments", html);
    }

    [Fact]
    public async Task Candidate_Create_Works()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await PostFormAsync(client, "/Candidates/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Тестовий Кандидат",
            ["Email"] = "candidate.integration@example.com",
            ["Phone"] = "+380671234000",
            ["ExperienceYears"] = "2",
            ["Skills"] = "ASP.NET Core, SQL",
            ["ResumeSummary"] = "Кандидат для integration test."
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Candidates.AnyAsync(candidate => candidate.Email == "candidate.integration@example.com"));
    }

    [Fact]
    public async Task Candidate_Create_Ignores_Posted_ResumeFilePath()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await PostFormAsync(client, "/Candidates/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Path Injection Candidate",
            ["Email"] = "path.injection@example.com",
            ["Phone"] = "+380671234002",
            ["ExperienceYears"] = "1",
            ["Skills"] = "Security review",
            ["ResumeFilePath"] = "https://example.com/evil.exe"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var resumePath = await db.Candidates
            .Where(candidate => candidate.Email == "path.injection@example.com")
            .Select(candidate => candidate.ResumeFilePath)
            .SingleAsync();
        Assert.Null(resumePath);
    }

    [Fact]
    public async Task Candidate_Duplicate_Email_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await PostFormAsync(client, "/Candidates/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Дублікат",
            ["Email"] = "olena.koval@example.com",
            ["Phone"] = "+380671234001",
            ["ExperienceYears"] = "3",
            ["Skills"] = "C#"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Candidates.CountAsync(candidate => candidate.Email == "olena.koval@example.com"));
    }

    [Fact]
    public async Task Duplicate_Application_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await PostFormAsync(client, "/Applications/Create", new Dictionary<string, string>
        {
            ["CandidateId"] = "1",
            ["VacancyId"] = "1",
            ["Status"] = "New",
            ["RecruiterComment"] = "Duplicate pair test."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Applications.CountAsync(application => application.CandidateId == 1 && application.VacancyId == 1));
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_Application()
    {
        using var factory = new HrReserveWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Applications.Add(new Application
        {
            CandidateId = 1,
            VacancyId = 1,
            Status = "New",
            AppliedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Interview_Create_With_Smtp_Disabled_Creates_Outbox_Message()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");
        var interviewData = await GetNewInterviewPostDataAsync(factory);
        var interviewDate = new DateTime(2031, 6, 3, 10, 30, 0);

        var response = await PostFormAsync(client, "/Interviews/Create", new Dictionary<string, string>
        {
            ["ApplicationId"] = interviewData.ApplicationId.ToString(),
            ["RecruiterId"] = interviewData.RecruiterId?.ToString() ?? string.Empty,
            ["InterviewDate"] = interviewDate.ToString("yyyy-MM-ddTHH:mm"),
            ["InterviewType"] = "Technical",
            ["Result"] = "Pending",
            ["Notes"] = "Outbox create integration test."
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var message = AssertSingleOutboxMessage(factory);
        Assert.Contains("Заплановано співбесіду", message);
        Assert.Contains(interviewData.CandidateEmail, message);
        Assert.Contains(interviewData.CandidateName, message);
        Assert.Contains(interviewData.VacancyTitle, message);
        Assert.Contains(interviewDate.ToString("g"), message);
        Assert.Contains("Technical", message);
        Assert.Contains("Outbox create integration test.", message);
        Assert.Contains("SMTP is disabled or host is empty.", message);

        if (!string.IsNullOrWhiteSpace(interviewData.RecruiterEmail))
        {
            Assert.Contains(interviewData.RecruiterEmail, message);
        }
    }

    [Fact]
    public async Task Interview_Edit_With_Smtp_Disabled_Creates_Outbox_Message()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "admin", "admin123");
        var interviewData = await GetExistingInterviewPostDataAsync(factory);
        var interviewDate = new DateTime(2031, 7, 4, 14, 45, 0);

        var response = await PostFormAsync(client, $"/Interviews/Edit/{interviewData.InterviewId}", new Dictionary<string, string>
        {
            ["Id"] = interviewData.InterviewId.ToString(),
            ["ApplicationId"] = interviewData.ApplicationId.ToString(),
            ["RecruiterId"] = interviewData.RecruiterId?.ToString() ?? string.Empty,
            ["InterviewDate"] = interviewDate.ToString("yyyy-MM-ddTHH:mm"),
            ["InterviewType"] = "Final",
            ["Result"] = "Pending",
            ["Notes"] = "Outbox edit integration test."
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var message = AssertSingleOutboxMessage(factory);
        Assert.Contains("Оновлено співбесіду", message);
        Assert.Contains(interviewData.CandidateEmail, message);
        Assert.Contains(interviewData.CandidateName, message);
        Assert.Contains(interviewData.VacancyTitle, message);
        Assert.Contains(interviewDate.ToString("g"), message);
        Assert.Contains("Final", message);
        Assert.Contains("Outbox edit integration test.", message);
        Assert.Contains("SMTP is disabled or host is empty.", message);

        if (!string.IsNullOrWhiteSpace(interviewData.RecruiterEmail))
        {
            Assert.Contains(interviewData.RecruiterEmail, message);
        }
    }

    [Fact]
    public async Task Feedback_Score_11_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "admin", "admin123");

        var response = await PostFormAsync(client, "/InterviewFeedbacks/Create", new Dictionary<string, string>
        {
            ["InterviewId"] = "1",
            ["Comment"] = "Score validation test.",
            ["Score"] = "11",
            ["Recommendation"] = "Hire"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.InterviewFeedbacks.AnyAsync(feedback => feedback.Score == 11));
    }

    [Fact]
    public async Task Database_Rejects_Invalid_Feedback_Score()
    {
        using var factory = new HrReserveWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.InterviewFeedbacks.Add(new InterviewFeedback
        {
            InterviewId = 1,
            Comment = "Direct DB constraint test.",
            Score = 11,
            Recommendation = "Hire",
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SoftSkill_Score_Outside_Range_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "admin", "admin123");

        var response = await PostFormAsync(client, "/SoftSkillAssessments/Create", new Dictionary<string, string>
        {
            ["CandidateId"] = "1",
            ["Communication"] = "0",
            ["Teamwork"] = "11",
            ["Responsibility"] = "5",
            ["StressResistance"] = "5",
            ["Leadership"] = "5",
            ["OverallComment"] = "Range validation test."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.SoftSkillAssessments.AnyAsync(assessment => assessment.Communication == 0 || assessment.Teamwork == 11));
    }

    [Fact]
    public async Task Upload_Exe_Resume_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var beforePath = await GetCandidateResumePathAsync(factory, 1);
        var response = await UploadResumeAsync(client, "/Candidates/UploadResume/1", "malware.exe", "MZ"u8.ToArray());
        var afterPath = await GetCandidateResumePathAsync(factory, 1);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(beforePath, afterPath);
    }

    [Fact]
    public async Task Upload_Too_Large_Resume_Does_Not_Pass()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var beforePath = await GetCandidateResumePathAsync(factory, 1);
        var oversizedPdf = new byte[(5 * 1024 * 1024) + 1];
        var response = await UploadResumeAsync(client, "/Candidates/UploadResume/1", "too-large.pdf", oversizedPdf);
        var afterPath = await GetCandidateResumePathAsync(factory, 1);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(beforePath, afterPath);
    }

    [Fact]
    public async Task Api_Unauthenticated_Request_Returns_401_Not_Login_Redirect()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/candidates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_Candidates_Returns_Dtos_Without_Password_Fields()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await client.GetAsync("/api/candidates");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("olena.koval@example.com", json);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_Forbidden_Role_Returns_403()
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "interviewer", "interviewer123");

        var response = await client.GetAsync("/api/candidates");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("resume.pdf", "%PDF-1.7")]
    [InlineData("resume.doc", "DOC")]
    [InlineData("resume.docx", "DOCX")]
    public async Task Upload_Allowed_Resume_File_Passes(string fileName, string content)
    {
        using var factory = new HrReserveWebApplicationFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client, "recruiter", "recruiter123");

        var response = await UploadResumeAsync(client, "/Candidates/UploadResume/1", fileName, System.Text.Encoding.UTF8.GetBytes(content));
        var resumePath = await GetCandidateResumePathAsync(factory, 1);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/uploads/resumes/", resumePath);
        Assert.EndsWith(Path.GetExtension(fileName), resumePath);
    }

    private static async Task<InterviewPostData> GetNewInterviewPostDataAsync(HrReserveWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.Applications
            .AsNoTracking()
            .Include(item => item.Candidate)
            .Include(item => item.Vacancy)
            .OrderBy(item => item.Id)
            .FirstAsync();
        var recruiter = await db.Recruiters
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .FirstAsync(item => item.Login == "recruiter");

        return new InterviewPostData(
            InterviewId: 0,
            ApplicationId: application.Id,
            CandidateName: application.Candidate!.FullName,
            CandidateEmail: application.Candidate.Email,
            VacancyTitle: application.Vacancy!.Title,
            RecruiterId: recruiter.Id,
            RecruiterEmail: recruiter.Email);
    }

    private static async Task<InterviewPostData> GetExistingInterviewPostDataAsync(HrReserveWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var interview = await db.Interviews
            .AsNoTracking()
            .Include(item => item.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(item => item.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(item => item.Recruiter)
            .OrderBy(item => item.Id)
            .FirstAsync();

        return new InterviewPostData(
            InterviewId: interview.Id,
            ApplicationId: interview.ApplicationId,
            CandidateName: interview.Application!.Candidate!.FullName,
            CandidateEmail: interview.Application.Candidate.Email,
            VacancyTitle: interview.Application.Vacancy!.Title,
            RecruiterId: interview.RecruiterId,
            RecruiterEmail: interview.Recruiter?.Email);
    }

    private static string AssertSingleOutboxMessage(HrReserveWebApplicationFactory factory)
    {
        Assert.True(Directory.Exists(factory.OutboxPath), $"Outbox directory was not created: {factory.OutboxPath}");
        var filePath = Assert.Single(Directory.GetFiles(factory.OutboxPath, "*.txt"));
        return File.ReadAllText(filePath);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password)
    {
        return await PostFormAsync(client, "/Account/Login", new Dictionary<string, string>
        {
            ["Login"] = login,
            ["Password"] = password,
            ["RememberMe"] = "false"
        });
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string path, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await GetAntiforgeryTokenAsync(client, path);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    private static async Task<HttpResponseMessage> UploadResumeAsync(HttpClient client, string path, string fileName, byte[] bytes)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Candidates/Details/1");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), "__RequestVerificationToken");
        content.Add(new ByteArrayContent(bytes), "resumeFile", fileName);

        return await client.PostAsync(path, content);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success)
        {
            match = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        }

        Assert.True(match.Success, $"Antiforgery token was not found on {path}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<string?> GetCandidateResumePathAsync(HrReserveWebApplicationFactory factory, int candidateId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Candidates
            .Where(candidate => candidate.Id == candidateId)
            .Select(candidate => candidate.ResumeFilePath)
            .FirstAsync();
    }

    private static void AssertAccessDeniedRedirect(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }
}
