using LostFoundDocs.Models;
namespace LostFoundDocs.Services.Validation;

public static class DeclarationRules
{
    public static Dictionary<string, string> Validate(ReportDraft d, int step, DateTime today)
    {
        var errors = new Dictionary<string, string>();
        void Check(string key, string? error) { if (error is not null) errors[key] = error; }
        if (step is 1 or 0)
        {
            Check("Type", d.Type is null ? "Choisissez le type de document." : null);
            if (d.Type == DocumentType.Other) Check("CustomTitle", FieldRules.Required(d.CustomTitle, "Indiquez l’intitulé du document."));
        }
        if (step is 2 or 0)
        {
            if (d.Kind == ReportKind.Lost) Check("Name", FieldRules.FullName(d.HolderName));
            else if (string.IsNullOrWhiteSpace(d.HolderName) && string.IsNullOrWhiteSpace(d.Number))
                Check("Name", "Renseignez au moins le nom ou le numéro visible sur le document.");
            else if (!string.IsNullOrWhiteSpace(d.HolderName)) Check("Name", FieldRules.FullName(d.HolderName));
            Check("Number", FieldRules.OptionalDocumentNumber(d.Number));
            Check("Birth", FieldRules.OptionalBirthDate(d.BirthDate, today));
        }
        if (step is 3 or 0)
        {
            Check("Date", FieldRules.EventDate(d.EventDate, today, d.BirthDate, "La date est obligatoire."));
            Check("City", FieldRules.Required(d.City, "La ville est obligatoire."));
            Check("Place", FieldRules.Required(d.Place, "Indiquez le lieu, même approximatif."));
            Check("Notes", FieldRules.Notes(d.Notes));
        }
        return errors;
    }
}
