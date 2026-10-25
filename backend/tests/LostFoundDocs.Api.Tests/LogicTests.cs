using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Pdf;
using LostFoundDocs.Api.Qr;
using LostFoundDocs.Api.Security;
using LostFoundDocs.Api.Services;
using Xunit;

namespace LostFoundDocs.Api.Tests;

public class TextNormalizerTests
{
    [Fact]
    public void Normalize_removes_accents_case_and_punctuation() =>
        Assert.Equal("aicha mbarga", TextNormalizer.Normalize("  Aïcha   M'BARGA "));

    [Fact]
    public void Names_are_order_insensitive() =>
        Assert.Equal(TextNormalizer.NormalizeName("Aïcha Mbarga"), TextNormalizer.NormalizeName("MBARGA aicha"));

    [Fact]
    public void Similarity_tolerates_typos_and_extra_given_names()
    {
        var a = TextNormalizer.NormalizeName("Aïcha Mbarga");
        Assert.Equal(1.0, TextNormalizer.NameSimilarity(a, TextNormalizer.NormalizeName("MBARGA Aicha")), 3);
        Assert.True(TextNormalizer.NameSimilarity(a, TextNormalizer.NormalizeName("Aicha Mbarga Marie")) >= 0.9);
        Assert.True(TextNormalizer.NameSimilarity(a, TextNormalizer.NormalizeName("Aicha Mbarha")) >= 0.8);
        Assert.True(TextNormalizer.NameSimilarity(a, TextNormalizer.NormalizeName("Denise Owona")) < 0.5);
    }
}

public class MatchScorerTests
{
    private static readonly MatchingOptions Options = new();
    private static readonly DateTime Day = new(2026, 10, 10);

    private static ReportFacts Facts(string name = "aicha mbarga", string? number = "N1", string? birth = "B1", string issuer = "universite douala",
                                     DocumentType type = DocumentType.Diploma, string city = "douala", int dayOffset = 0, string title = "") =>
        new(type, title, TextNormalizer.NormalizeName(name), number, birth, issuer, city, Day.AddDays(dayOffset));

    [Fact]
    public void Complete_and_consistent_information_is_a_strong_match()
    {
        var result = MatchScorer.Evaluate(Facts(), Facts(dayOffset: 1), Options);
        Assert.NotNull(result);
        Assert.True(result!.Score >= Options.StrongScore);
        Assert.Equal(7, result.Criteria.Count);
    }

    [Fact]
    public void A_matching_number_alone_reaches_the_threshold()
    {
        var lost = Facts(name: "", birth: null, issuer: "");
        var found = Facts(name: "", birth: null, issuer: "", dayOffset: 1);
        var result = MatchScorer.Evaluate(lost, found, Options);
        Assert.NotNull(result);
        Assert.Equal(60, result!.Score);
        Assert.Equal(CriterionState.NotCompared, result.Criteria.First(c => c.Name == "Nom").State);
    }

    [Fact]
    public void Poor_information_never_reaches_the_threshold()
    {
        var lost = Facts(name: "", number: null, birth: null, issuer: "");
        var found = Facts(name: "", number: null, birth: null, issuer: "", dayOffset: 1);
        Assert.Null(MatchScorer.Evaluate(lost, found, Options));
    }

    [Fact]
    public void Contradictory_information_is_rejected()
    {
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(number: "N2"), Options));                       // numéro différent
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(birth: "B2"), Options));                        // naissance différente
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(name: "Denise Owona"), Options));               // nom trop éloigné
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(type: DocumentType.Passport), Options));        // type différent
    }

    [Fact]
    public void Chronology_must_be_plausible()
    {
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(dayOffset: -(Options.LostDateToleranceDays + 1)), Options)); // trouvé bien avant la perte
        Assert.NotNull(MatchScorer.Evaluate(Facts(), Facts(dayOffset: -2), Options));                                 // perte approximative tolérée
        Assert.Null(MatchScorer.Evaluate(Facts(), Facts(dayOffset: Options.MaxGapDays + 1), Options));                // trop tard
    }

    [Fact]
    public void Other_documents_must_have_similar_titles()
    {
        var lost = Facts(type: DocumentType.Other, title: "attestation de residence");
        Assert.NotNull(MatchScorer.Evaluate(lost, Facts(type: DocumentType.Other, title: "attestation residence", dayOffset: 1), Options));
        Assert.Null(MatchScorer.Evaluate(lost, Facts(type: DocumentType.Other, title: "acte de naissance", dayOffset: 1), Options));
    }

    [Fact]
    public void City_difference_does_not_reject_but_lowers_the_score()
    {
        var same = MatchScorer.Evaluate(Facts(), Facts(dayOffset: 1), Options)!;
        var other = MatchScorer.Evaluate(Facts(), Facts(city: "yaounde", dayOffset: 1), Options)!;
        Assert.Equal(same.Score - 5, other.Score);
    }
}

public class CryptoTests
{
    [Fact]
    public void Password_hash_roundtrip_and_rehash_flag()
    {
        var hash = PasswordHasher.Hash("Motdepasse1", 1000);
        Assert.True(PasswordHasher.Verify("Motdepasse1", hash, out var rehash, 1000));
        Assert.False(rehash);
        Assert.True(PasswordHasher.Verify("Motdepasse1", hash, out rehash, 5000));
        Assert.True(rehash);
        Assert.False(PasswordHasher.Verify("autre", hash, out _, 1000));
        Assert.False(PasswordHasher.Verify("x", "!", out _, 1000));
        Assert.NotEqual(hash, PasswordHasher.Hash("Motdepasse1", 1000)); // sel aléatoire
    }

    [Fact]
    public void Tokens_are_random_and_hashed_deterministically()
    {
        var a = Tokens.Generate();
        Assert.NotEqual(a, Tokens.Generate());
        Assert.Equal(Tokens.Hash(a), Tokens.Hash(a));
        Assert.NotEqual(a, Tokens.Hash(a));
        Assert.Equal(64, Tokens.Hash(a).Length);
        Assert.Matches(@"^\d{6}$", Tokens.SixDigitCode());
        Assert.Matches(@"^[A-Z2-9]{4}-[A-Z2-9]{4}$", Tokens.VerificationCode());
    }

    [Fact]
    public void Fingerprints_normalize_and_separate_domains()
    {
        var fp = new Fingerprinter("a-secret-of-at-least-sixteen-chars");
        Assert.Equal(fp.Number("d 112-048 213"), fp.Number("D112048213".Replace("-", "")) );
        Assert.NotEqual(fp.Number("D112048213"), fp.Number("D112048214"));
        Assert.NotEqual(fp.Hash("number", "X"), fp.Hash("birth", "X"));
        Assert.NotEqual(new Fingerprinter("another-secret-of-sixteen-chars").Number("D1"), fp.Number("D1"));
    }
}

public class ValidatorTests
{
    [Fact]
    public void Phone_rules() { Assert.True(PhoneNumber.IsValid("+237 6 77 12 48 21")); Assert.False(PhoneNumber.IsValid("577124821")); }

    [Fact]
    public void Errors_are_grouped_by_field()
    {
        var v = new Validator();
        v.FullName("fullName", "A");
        v.Phone("phone", "12");
        v.NewPassword("password", "1234567");
        v.Notes("notes", "numéro 112048213");
        var ex = Assert.Throws<ApiException>(() => v.ThrowIfInvalid());
        Assert.Equal(400, ex.Status);
        Assert.Equal(4, ex.Errors!.Count);
    }
}

public class QrAndPdfTests
{
    [Fact]
    public void Qr_has_the_expected_size_and_finder_patterns()
    {
        var m = QrEncoder.Encode("https://exemple.cm/verify/K7QD-4M2X");
        Assert.Equal(17 + 4 * QrEncoder.LastVersion, m.Length);
        Assert.True(m[0][0] && m[0][6] && m[6][0] && m[6][6] && !m[1][1] && m[3][3]);
        Assert.Equal(string.Join("|", QrEncoder.ToRows(m)), string.Join("|", QrEncoder.ToRows(QrEncoder.Encode("https://exemple.cm/verify/K7QD-4M2X"))));
    }

    [Fact]
    public void Qr_rejects_too_long_text() => Assert.Throws<ArgumentException>(() => QrEncoder.Encode(new string('x', 300)));

    [Fact]
    public void Pdf_is_well_formed()
    {
        var pdf = new SimplePdf();
        pdf.Text(50, 780, 12, true, "Reçu (test) \\ « ok »");
        var bytes = pdf.Build();
        var text = System.Text.Encoding.Latin1.GetString(bytes);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("%%EOF", text);
        Assert.Contains("\\(test\\)", text);
    }
}

public class StatusRulesTests
{
    [Fact]
    public void Dispute_and_reproposal_transitions_are_allowed_but_final_states_are_final()
    {
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchFound, ReportStatus.Searching));
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchConfirmed, ReportStatus.MatchFound));
        Assert.True(ReportStatusRules.CanTransition(ReportStatus.MatchDisputed, ReportStatus.Returned));
        Assert.False(ReportStatusRules.CanTransition(ReportStatus.Searching, ReportStatus.Returned));
        foreach (var s in new[] { ReportStatus.Returned, ReportStatus.Expired, ReportStatus.Cancelled })
            Assert.True(ReportStatusRules.IsTerminal(s));
    }

    [Fact]
    public void Names_are_shortened_for_the_other_party()
    {
        Assert.Equal("Aïcha M.", Names.Short("Aïcha Mbarga"));
        Assert.Equal("Brice", Names.Short("Brice"));
        Assert.Equal("Brice T.", Names.Short("brice tchoumi"));
    }

    [Fact]
    public void French_dates_do_not_depend_on_the_server_culture()
    {
        Assert.Equal("2 octobre 2026", Formatting.LongDate(new DateTime(2026, 10, 2)));
        Assert.Equal("02/10/2026 à 14:05", Formatting.LocalDateTime(new DateTime(2026, 10, 2, 13, 5, 0, DateTimeKind.Utc), 1));
    }
}
