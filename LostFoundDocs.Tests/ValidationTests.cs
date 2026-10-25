using LostFoundDocs.Models;
using LostFoundDocs.Services.Validation;
using Xunit;

namespace LostFoundDocs.Tests;

public class ValidationTests : TestBase
{
    [Theory]
    [InlineData("677124821")]
    [InlineData("6 77 12 48 21")]
    [InlineData("+237 6 77 12 48 21")]
    [InlineData("00237 677124821")]
    public void Phone_valid_formats(string input) => Assert.Null(FieldRules.Phone(input));

    [Theory]
    [InlineData("")]
    [InlineData("577124821")]
    [InlineData("67712482")]
    public void Phone_invalid(string input) => Assert.NotNull(FieldRules.Phone(input));

    [Fact]
    public void BirthDate_is_optional_but_checked_when_given()
    {
        Assert.Null(FieldRules.OptionalBirthDate(null, Today));
        Assert.NotNull(FieldRules.OptionalBirthDate(Today.AddDays(1), Today));
        Assert.NotNull(FieldRules.OptionalBirthDate(Today.AddYears(-130), Today));
        Assert.Null(FieldRules.OptionalBirthDate(Today.AddYears(-25), Today));
    }

    [Fact]
    public void EventDate_rules()
    {
        Assert.Equal("obligatoire", FieldRules.EventDate(null, Today, null, "obligatoire"));
        Assert.NotNull(FieldRules.EventDate(Today.AddDays(1), Today, null, "x"));
        Assert.NotNull(FieldRules.EventDate(new DateTime(2020, 1, 1), Today, new DateTime(2021, 1, 1), "x"));
        Assert.Null(FieldRules.EventDate(Today, Today, null, "x"));
    }

    [Fact]
    public void DocumentNumber_is_never_required()
    {
        Assert.Null(FieldRules.OptionalDocumentNumber(""));
        Assert.Null(FieldRules.OptionalDocumentNumber(null));
        Assert.Null(FieldRules.OptionalDocumentNumber("AB-12 34"));
        Assert.NotNull(FieldRules.OptionalDocumentNumber("AB1"));
        Assert.NotNull(FieldRules.OptionalDocumentNumber(new string('1', 21)));
        Assert.NotNull(FieldRules.OptionalDocumentNumber("12#4567"));
    }

    [Fact]
    public void Notes_reject_long_numbers_and_long_text()
    {
        Assert.Null(FieldRules.Notes("dans une pochette bleue"));
        Assert.NotNull(FieldRules.Notes("mon numéro 112048213"));
        Assert.NotNull(FieldRules.Notes(new string('a', 301)));
    }

    [Fact]
    public void Email_is_optional()
    {
        Assert.Null(FieldRules.OptionalEmail(""));
        Assert.Null(FieldRules.OptionalEmail("nom@exemple.cm"));
        Assert.NotNull(FieldRules.OptionalEmail("abc"));
    }

    [Fact]
    public void Passwords()
    {
        Assert.NotNull(FieldRules.NewPassword("1234567"));
        Assert.Null(FieldRules.NewPassword("12345678"));
        Assert.NotNull(FieldRules.PasswordConfirmation("12345678", "autre"));
        Assert.Null(FieldRules.PasswordConfirmation("12345678", "12345678"));
    }

    [Fact]
    public void Masking_shows_only_last_four()
    {
        Assert.Equal("•••• 8213", Masking.MaskNumber("112048213"));
        Assert.Equal(string.Empty, Masking.MaskNumber(null));
        Assert.Equal("•••", Masking.MaskNumber("123"));
    }

    [Fact]
    public void Date_formats_are_french_and_unique()
    {
        var d = new DateTime(2026, 10, 2);
        Assert.Equal("02 oct. 2026", DateFormat.Short(d));
        Assert.Equal("02 octobre 2026", DateFormat.Long(d));
        var now = new DateTime(2026, 10, 18, 12, 0, 0);
        Assert.Equal("il y a 12 min", DateFormat.Ago(now.AddMinutes(-12), now));
        Assert.Equal("hier", DateFormat.Ago(now.AddDays(-1), now));
        Assert.Equal("Il y a 12 min", DateFormat.AgoCapitalized(now.AddMinutes(-12), now));
    }

    [Fact]
    public void Names_and_grammatical_gender()
    {
        Assert.Equal("Aïcha M.", NameFormat.Short("Aïcha Mbarga"));
        Assert.Equal("Aicha Mbarga", NameFormat.Capitalize("aicha mbarga"));
        Assert.Equal("Perdue", DocumentTypes.EventVerb(DocumentType.NationalId, ReportKind.Lost));
        Assert.Equal("Trouvé", DocumentTypes.EventVerb(DocumentType.Diploma, ReportKind.Found));
    }

    [Fact]
    public void Document_types_have_dynamic_labels()
    {
        Assert.Equal("Numéro du passeport", DocumentTypes.Get(DocumentType.Passport).NumberLabel);
        Assert.Equal("Matricule", DocumentTypes.Get(DocumentType.StudentCard).NumberLabel);
        Assert.Equal(10, DocumentTypes.All.Count);
        Assert.DoesNotContain(DocumentTypes.All.Where(t => t.Type != DocumentType.Diploma), t => t.NumberLabel == "Numéro du diplôme");
    }

    [Fact]
    public void Restitution_slots_are_always_in_the_future()
    {
        var days = RestitutionRules.Days(Today);
        Assert.Equal(3, days.Count);
        Assert.All(days, d => Assert.True(d.Date > Today));
        Assert.StartsWith("Demain,", days[0].Label);
        Assert.Equal(new DateTime(2026, 10, 19, 10, 0, 0), RestitutionRules.Combine(days[0].Date, "10:00"));
    }
}
