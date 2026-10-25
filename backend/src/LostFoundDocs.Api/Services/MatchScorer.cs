using System.Text.Json;
using LostFoundDocs.Api.Domain;

namespace LostFoundDocs.Api.Services;

public enum CriterionState { Match, Partial, NotCompared }

public sealed record CriterionResult(string Name, CriterionState State);

public sealed record ReportFacts(
    DocumentType Type, string TitleNormalized, string NameNormalized, string? NumberHash, string? BirthDateHash,
    string IssuerNormalized, string CityNormalized, DateTime EventDate);

public sealed record MatchEvaluation(int Score, IReadOnlyList<CriterionResult> Criteria)
{
    public string CriteriaJson => JsonSerializer.Serialize(Criteria.Select(c => new { name = c.Name, state = c.State.ToString() }));
}

/// <summary>
/// Moteur de correspondance (pur, sans base de données). Un document « perdu » est comparé à un document « trouvé ».
/// Rejet immédiat si les informations renseignées des deux côtés se contredisent (numéro différent, date de naissance
/// différente, nom trop éloigné, types différents, chronologie impossible). Sinon score sur 100 :
/// type 15 · numéro 35 · nom 25 · naissance 10 · organisme 5 · ville 5 · date 5.
/// Un critère non renseigné d'un côté est « non comparé » et ne rapporte aucun point : des informations trop pauvres
/// ne suffisent donc jamais à atteindre le seuil.
/// </summary>
public static class MatchScorer
{
    public static MatchEvaluation? Evaluate(ReportFacts lost, ReportFacts found, MatchingOptions options)
    {
        if (lost.Type != found.Type) return null;

        var gapDays = (found.EventDate.Date - lost.EventDate.Date).Days;
        if (gapDays < -options.LostDateToleranceDays || gapDays > options.MaxGapDays) return null;

        var criteria = new List<CriterionResult>();
        var score = 0;

        // Type (et intitulé pour « Autre document »)
        if (lost.Type == DocumentType.Other)
        {
            if (lost.TitleNormalized.Length > 0 && found.TitleNormalized.Length > 0
                && TextNormalizer.NameSimilarity(lost.TitleNormalized, found.TitleNormalized) < 0.7) return null;
        }
        criteria.Add(new("Type de document", CriterionState.Match));
        score += 15;

        // Nom
        var nameState = CriterionState.NotCompared;
        if (lost.NameNormalized.Length > 0 && found.NameNormalized.Length > 0)
        {
            var sim = TextNormalizer.NameSimilarity(lost.NameNormalized, found.NameNormalized);
            if (sim < 0.7) return null;
            nameState = sim >= 0.9 ? CriterionState.Match : CriterionState.Partial;
            score += nameState == CriterionState.Match ? 25 : 15;
        }
        criteria.Add(new("Nom", nameState));

        // Numéro
        var numberState = CriterionState.NotCompared;
        if (lost.NumberHash is not null && found.NumberHash is not null)
        {
            if (lost.NumberHash != found.NumberHash) return null;
            numberState = CriterionState.Match;
            score += 35;
        }
        criteria.Add(new("Numéro", numberState));

        // Date de naissance
        var birthState = CriterionState.NotCompared;
        if (lost.BirthDateHash is not null && found.BirthDateHash is not null)
        {
            if (lost.BirthDateHash != found.BirthDateHash) return null;
            birthState = CriterionState.Match;
            score += 10;
        }
        criteria.Add(new("Date de naissance", birthState));

        // Organisme émetteur
        var issuerState = CriterionState.NotCompared;
        if (lost.IssuerNormalized.Length > 0 && found.IssuerNormalized.Length > 0)
        {
            var sim = TextNormalizer.TokenOverlap(lost.IssuerNormalized, found.IssuerNormalized);
            if (sim >= 0.8) { issuerState = CriterionState.Match; score += 5; }
            else if (sim >= 0.5) { issuerState = CriterionState.Partial; score += 3; }
        }
        criteria.Add(new("Établissement", issuerState));

        // Ville
        var cityState = CriterionState.NotCompared;
        if (lost.CityNormalized.Length > 0 && lost.CityNormalized == found.CityNormalized)
        {
            cityState = CriterionState.Match;
            score += 5;
        }
        criteria.Add(new("Ville", cityState));

        // Date : plus la découverte suit la perte de près, mieux c'est
        var absGap = Math.Abs(gapDays);
        var dateState = absGap <= 7 ? CriterionState.Match : CriterionState.Partial;
        score += absGap <= 7 ? 5 : absGap <= 60 ? 3 : 0;
        criteria.Add(new("Date", dateState));

        return score >= options.MinScore ? new MatchEvaluation(Math.Min(score, 100), criteria) : null;
    }
}
