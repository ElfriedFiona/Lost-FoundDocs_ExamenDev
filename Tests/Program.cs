using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.Services.Validation;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("ÉCHEC : " + name);
    Console.WriteLine("OK : " + name);
    passed++;
}
async Task Reject(Func<Task> action, string name)
{
    try { await action(); }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
    { Check(true, name); return; }
    throw new Exception("ÉCHEC : exception attendue : " + name);
}
Clock.Provider = () => new DateTime(2026, 10, 18, 12, 30, 0);
DemoSettings.SimulatedLatencyMs = 0;
var auth = new MockAuthService();
var reports = new MockReportService(auth);
var notifications = new MockNotificationService(reports);
var matches = new MockMatchService(reports, notifications);
var conversations = new MockConversationService(reports, matches);
var returns = new MockRestitutionService(reports, matches, conversations, notifications, auth);
var profile = new MockProfileService(auth, reports);
ReportDraft Draft(ReportKind kind = ReportKind.Lost, DocumentType type = DocumentType.Diploma) => new()
{
    Kind = kind, Type = type, HolderName = "Test Utilisateur", Number = "AB123456",
    City = "Douala", Place = "Akwa", EventDate = Clock.Today.AddDays(-2)
};
Check(!(await auth.SignInAsync(DemoSettings.DemoPhone, "incorrect")).Success, "Identifiants invalides refusés");
Check((await auth.SignInAsync(DemoSettings.DemoPhone, DemoSettings.DemoPassword)).Success, "Connexion de démonstration");
Check(DemoDataValidator.Validate(reports.Reports, Clock.Now).Count == 0, "Chronologie des six signalements fictifs");
Check(conversations.Conversations.Count == 3, "Trois conversations initiales cohérentes");
await profile.ResetDemoAsync(false);
Check(reports.Reports.Count == 0 && conversations.Conversations.Count == 0 && notifications.Notifications.Count == 0, "Compte vide : trois listes vides");
Check(DeclarationRules.Validate(new ReportDraft(), 1, Clock.Today).ContainsKey("Type"), "Type obligatoire");
var other = Draft(type: DocumentType.Other);
Check(DeclarationRules.Validate(other, 0, Clock.Today).ContainsKey("CustomTitle"), "Intitulé Autre obligatoire pour une perte");
var foundDraft = Draft(ReportKind.Found, DocumentType.Passport);
foundDraft.HolderName = ""; foundDraft.Number = "";
Check(DeclarationRules.Validate(foundDraft, 0, Clock.Today).ContainsKey("Name"), "Trouvaille sans nom ni numéro refusée");
foundDraft.Number = "AB1234";
Check(DeclarationRules.Validate(foundDraft, 0, Clock.Today).Count == 0, "Trouvaille avec numéro seulement autorisée");
foundDraft.HolderName = "Test Nom"; foundDraft.Number = "";
Check(DeclarationRules.Validate(foundDraft, 0, Clock.Today).Count == 0, "Trouvaille avec nom seulement autorisée");
Check(FieldRules.OptionalBirthDate(Clock.Today.AddDays(1), Clock.Today) is not null, "Naissance future refusée");
Check(FieldRules.EventDate(null, Clock.Today, null, "Obligatoire") is not null, "Date absente refusée");
Check(FieldRules.EventDate(Clock.Today.AddDays(1), Clock.Today, null, "Obligatoire") is not null, "Date future refusée");
Check(FieldRules.Notes("Document 123456789") is not null, "Numéro complet interdit dans les notes");
Check(FieldRules.OptionalDocumentNumber("ABC") is not null && FieldRules.OptionalDocumentNumber("") is null, "Numéro court refusé, numéro absent autorisé");
var draft = Draft();
var report = await reports.SubmitAsync(draft);
Check((await reports.SubmitAsync(draft)).Id == report.Id && reports.Reports.Count == 1, "Double soumission idempotente");
Check(!report.MaskedNumber.Contains("123456") && report.MaskedNumber.EndsWith("3456"), "Numéro masqué");
await Reject(() => conversations.OpenAsync(report.Id), "Conversation interdite avant correspondance");
var match = await matches.SearchAsync(report.Id);
Check(match is not null && report.Status == ReportStatus.MatchFound && match.EventDate >= report.EventDate, "Correspondance perte et dates compatibles");
Check((await matches.SearchAsync(report.Id)) == match && notifications.Notifications.Count == 1, "Recherche répétée sans doublon de notification");
var chat = await conversations.OpenAsync(report.Id);
Check(chat.Unread == 0 && chat.Contact == "Brice T.", "Lecture remet le compteur à zéro et rôle trouveur correct");
await Reject(() => conversations.SendAsync(report.Id, "   "), "Message vide refusé");
await conversations.SendAsync(report.Id, "Bonjour");
Check(chat.Messages.Last().Text == "Bonjour", "Message local ajouté");
var proposal = await returns.GetProposalAsync(report.Id);
Check(proposal is { Accepted: false } && proposal.Appointment > Clock.Now, "Proposition propriétaire future");
await Reject(() => returns.ConfirmAsync(report.Id), "Restitution refusée sans acceptation");
await returns.AcceptAsync(report.Id);
var receipt = await returns.ConfirmAsync(report.Id);
Check(report.Status == ReportStatus.Returned && receipt.Owner == auth.CurrentUser!.FullName && receipt.Finder == "Brice T.", "Restitution propriétaire avec rôles corrects");
Check(ReferenceEquals(receipt, await returns.ConfirmAsync(report.Id)), "Confirmation et reçu idempotents");
await Reject(() => conversations.SendAsync(report.Id, "Encore"), "Conversation restituée clôturée");
await Reject(() => reports.UpdateStatusAsync(report.Id, ReportStatus.Searching), "Statut terminal protégé");
var found = await reports.SubmitAsync(foundDraft);
var foundMatch = await matches.SearchAsync(found.Id);
Check(foundMatch is not null && foundMatch.EventDate <= found.EventDate, "Correspondance trouvaille chronologiquement compatible");
await Reject(() => returns.ProposeAsync(found.Id, "Akwa", Clock.Now.AddHours(-1)), "Créneau passé refusé");
await returns.ProposeAsync(found.Id, "Hôtel de ville", Clock.Today.AddDays(1).AddHours(10));
var foundReceipt = await returns.ConfirmAsync(found.Id);
Check(foundReceipt.Owner == "Aïssatou N." && foundReceipt.Finder == auth.CurrentUser!.FullName, "Restitution trouveur avec rôles inversés");
profile.MatchModeIndex = 2;
var absent = await reports.SubmitAsync(Draft());
Check(await matches.SearchAsync(absent.Id) is null && absent.Status == ReportStatus.Searching, "Mode sans résultat conserve la recherche");
profile.MatchModeIndex = 1;
Check(await matches.SearchAsync(absent.Id) is not null, "Mode toujours une correspondance");
await matches.DisputeAsync(absent.Id, "Ce n’est pas mon document");
Check(absent.Status == ReportStatus.MatchDisputed, "Contestation conservée sur le même signalement");
await Reject(() => conversations.OpenAsync(absent.Id), "Correspondance contestée non contactable");
var cancelled = await reports.SubmitAsync(Draft());
Check(await reports.CancelAsync(cancelled.Id) && cancelled.Status == ReportStatus.Cancelled, "Annulation du signalement");
notifications.MarkAllRead();
Check(notifications.Notifications.All(n => n.IsRead), "Toutes les notifications marquées lues");
await profile.SaveInformationAsync("Test Profil", "test@example.com");
Check(auth.CurrentUser!.FullName == "Test Profil", "Profil modifiable");
await Reject(() => profile.ChangePasswordAsync("incorrect", "NewPass123", "NewPass123"), "Ancien mot de passe vérifié");
await profile.ChangePasswordAsync(DemoSettings.DemoPassword, "NewPass123", "NewPass123");
Check(auth.CurrentUser!.Password == "NewPass123", "Changement du mot de passe simulé");
await auth.SignOutAsync();
await auth.RegisterAsync(new RegistrationRequest("Nouveau Compte", "699000001", null, "TestPass123"));
Check(reports.Reports.Count == 0 && notifications.Notifications.Count == 0 && conversations.Conversations.Count == 0, "Nouveau compte isolé et vide");
await Reject(() => reports.UpdateStatusAsync(report.Id, ReportStatus.Cancelled), "Isolation entre comptes");
Console.WriteLine($"{passed} vérifications réussies.");
