using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using Xunit;

namespace LostFoundDocs.Tests;

public class StatusRulesTests : TestBase
{
    [Fact]
    public void Normal_lifecycle_is_allowed()
    {
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.Searching, ReportStatus.MatchFound));
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchFound, ReportStatus.MatchConfirmed));
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchConfirmed, ReportStatus.Returned));
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchFound, ReportStatus.MatchDisputed));
    }

    [Fact]
    public void Skipping_steps_and_leaving_final_states_is_forbidden()
    {
        Assert.False(ReportStatusRules.CanTransition(ReportStatus.Searching, ReportStatus.Returned));
        foreach (var final in new[] { ReportStatus.Returned, ReportStatus.Expired, ReportStatus.Cancelled })
        {
            Assert.True(ReportStatusRules.IsTerminal(final));
            Assert.False(ReportStatusRules.CanTransition(final, ReportStatus.Searching));
        }
    }
}

public class AuthTests : TestBase
{
    [Fact]
    public async Task Demo_account_signs_in_with_any_phone_format()
    {
        var auth = new MockAuthService();
        Assert.True((await auth.SignInAsync("+237 6 77 12 48 21", DemoSettings.DemoPassword)).Success);
        Assert.True(auth.CurrentUser!.IsDemoAccount);
    }

    [Fact]
    public async Task Wrong_credentials_get_the_same_generic_failure()
    {
        var auth = new MockAuthService();
        var wrongPassword = await auth.SignInAsync(DemoSettings.DemoPhone, "mauvais");
        var unknownPhone = await auth.SignInAsync("699999999", "x");
        Assert.False(wrongPassword.Success);
        Assert.False(unknownPhone.Success);
        Assert.Equal(wrongPassword.Failure, unknownPhone.Failure);
        Assert.Null(auth.CurrentUser);
    }

    [Fact]
    public async Task Registering_an_existing_phone_is_refused_and_a_new_account_starts_empty()
    {
        var auth = new MockAuthService();
        var duplicate = await auth.RegisterAsync(new RegistrationRequest("Awa Nkou", DemoSettings.DemoPhone, null, "Motdepasse1"));
        Assert.Equal(AuthFailure.PhoneAlreadyUsed, duplicate.Failure);

        Assert.True((await auth.RegisterAsync(new RegistrationRequest("Awa Nkou", "655443322", null, "Motdepasse1"))).Success);
        Assert.False(auth.CurrentUser!.IsDemoAccount);
        Assert.Empty(new MockReportService(auth).Reports);
    }

    [Fact]
    public async Task Password_reset_flow_and_no_account_enumeration()
    {
        var auth = new MockAuthService();
        Assert.Equal(AuthFailure.InvalidCode, (await auth.VerifyResetCodeAsync(DemoSettings.DemoPhone, "000000")).Failure);
        Assert.Equal(AuthFailure.InvalidCode, (await auth.VerifyResetCodeAsync("699999999", DemoSettings.ResetCode)).Failure);

        Assert.True((await auth.VerifyResetCodeAsync(DemoSettings.DemoPhone, DemoSettings.ResetCode)).Success);
        Assert.True((await auth.ResetPasswordAsync(DemoSettings.DemoPhone, "Nouveau@2027")).Success);
        Assert.False((await auth.SignInAsync(DemoSettings.DemoPhone, DemoSettings.DemoPassword)).Success);
        Assert.True((await auth.SignInAsync(DemoSettings.DemoPhone, "Nouveau@2027")).Success);
    }

    [Fact]
    public async Task Reset_requires_a_verified_code()
    {
        var auth = new MockAuthService();
        Assert.Equal(AuthFailure.NotVerified, (await auth.ResetPasswordAsync(DemoSettings.DemoPhone, "Nouveau@2027")).Failure);
    }

    [Fact]
    public async Task Changing_password_checks_the_current_one()
    {
        var auth = await SignedInDemoAsync();
        Assert.Equal(AuthFailure.InvalidCredentials, (await auth.ChangePasswordAsync("faux", "Autre@2028")).Failure);
        Assert.True((await auth.ChangePasswordAsync(DemoSettings.DemoPassword, "Autre@2028")).Success);
    }
}
