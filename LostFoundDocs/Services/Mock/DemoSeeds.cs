using LostFoundDocs.Models;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// SIMULATION (frontend uniquement) : correspondances, conversations, notifications et reçus du compte de démonstration.
/// Tout est dérivé des signalements fictifs (<see cref="DemoData"/>) pour rester chronologiquement cohérent :
/// correspondance ≥ déclaration, messages après la correspondance, restitution après les échanges.
/// </summary>
public static class DemoSeeds
{
    public static void Populate(UserWorld world, IReadOnlyList<Report> reports, UserAccount user, DateTime now)
    {
        Report? R(string n) => reports.FirstOrDefault(r => r.Reference.EndsWith("-" + n));
        var cni = R("003317");            // perdue, correspondance trouvée
        var dipActive = R("004102");      // trouvé, correspondance trouvée
        var dipReturned = R("004698");    // trouvé, restitué
        var permis = R("001204");         // perdu, restitué
        var student = R("002741");        // perdue, recherche en cours
        var me = NameFormat.Short(user.FullName);

        if (cni is not null)
        {
            var m = AddMatch(world, cni, "Yannick K.", 92, 4801, cni.UpdatedAt, now);
            var c = NewConversation(world, cni, m);
            Msg(c, "Le trouveur a été notifié automatiquement.", system: true, at: m.FoundAt);
            Msg(c, "Bonjour, j’ai trouvé votre carte nationale d’identité.", mine: false, at: m.FoundAt.AddMinutes(15));
            Msg(c, "Bonjour ! Merci beaucoup. Où pouvons-nous organiser la restitution ?", mine: true, at: m.FoundAt.AddMinutes(40));
            Notify(world, NotificationKind.Match, "Une correspondance a été trouvée pour votre carte nationale d’identité.",
                "Ouvrez la correspondance pour contacter la personne qui l’a trouvée.", m.FoundAt, cni.Id, null, read: false);
        }

        if (dipActive is not null)
        {
            var m = AddMatch(world, dipActive, "Brice T.", 89, 7120, dipActive.UpdatedAt, now);
            var c = NewConversation(world, dipActive, m);
            Msg(c, "Le propriétaire a été notifié automatiquement.", system: true, at: m.FoundAt);
            Msg(c, "Bonjour, j’ai retrouvé votre diplôme.", mine: true, at: m.FoundAt.AddMinutes(10));
            var reply = now.AddMinutes(-20);
            Msg(c, "Bonjour, merci beaucoup ! Il s’agit bien de mon diplôme.", mine: false, at: reply);
            c.UnreadCount = 1;
            Notify(world, NotificationKind.Message, "Un utilisateur a répondu à votre message.", "Brice T. · Diplôme",
                reply, dipActive.Id, c.Id, read: false);
        }

        if (dipReturned is not null)
        {
            var matchAt = dipReturned.CreatedAt.AddHours(2);
            var m = AddMatch(world, dipReturned, "Chantal O.", 91, 6655, matchAt, now);
            var c = NewConversation(world, dipReturned, m);
            Msg(c, "Le propriétaire a été notifié automatiquement.", system: true, at: matchAt);
            Msg(c, "Bonjour, j’ai trouvé votre diplôme.", mine: true, at: matchAt.AddMinutes(10));
            Msg(c, "Merci infiniment ! On peut se voir demain ?", mine: false, at: matchAt.AddMinutes(25));
            Msg(c, $"Restitution confirmée · Réf. {dipReturned.Reference}", system: true, at: dipReturned.UpdatedAt);
            c.IsClosed = true;
            world.Receipts[dipReturned.Id] = MakeReceipt(dipReturned, m, owner: m.CounterpartName, handedBy: me);
            Notify(world, NotificationKind.Restitution, "La restitution du diplôme a été enregistrée.",
                $"Référence : {dipReturned.Reference}", dipReturned.UpdatedAt, dipReturned.Id, null, read: false);
        }

        if (permis is not null)
        {
            var matchAt = permis.CreatedAt.AddDays(1);
            var m = AddMatch(world, permis, "Paul M.", 90, 5512, matchAt, now);
            world.Receipts[permis.Id] = MakeReceipt(permis, m, owner: me, handedBy: m.CounterpartName);
            Notify(world, NotificationKind.Restitution, "La restitution de votre permis de conduire a été enregistrée.",
                $"Référence : {permis.Reference}", permis.UpdatedAt, permis.Id, null, read: true);
        }

        if (student is not null)
        {
            Notify(world, NotificationKind.Reminder, "Votre signalement « Carte d’étudiant » reste actif.",
                "Nous comparons automatiquement chaque nouveau document trouvé.", student.CreatedAt.AddDays(6), student.Id, null, read: true);
        }

        world.Notifications.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
    }

    private static MatchResult AddMatch(UserWorld world, Report report, string counterpart, int score, int counterpartRef,
                                        DateTime foundAt, DateTime now)
    {
        var match = new MatchResult
        {
            ReportId = report.Id,
            UserSide = report.Kind,
            CounterpartReference = $"LF-{now.Year}-{counterpartRef:D6}",
            CounterpartName = counterpart,
            Type = report.Type,
            City = report.City,
            CounterpartDate = MatchSimulator.CounterpartDate(report, now),
            Score = score,
            Criteria = MatchSimulator.BuildCriteria(report),
            FoundAt = foundAt,
        };
        world.Matches[report.Id] = match;
        world.Outcomes[report.Id] = new MatchOutcome(true, match);
        return match;
    }

    private static Conversation NewConversation(UserWorld world, Report report, MatchResult match)
    {
        var c = new Conversation
        {
            ReportId = report.Id,
            DocumentTitle = report.Title,
            Reference = report.Reference,
            UserIsOwner = report.Kind == ReportKind.Lost,
            CounterpartName = match.CounterpartName,
        };
        world.Conversations.Add(c);
        return c;
    }

    private static void Msg(Conversation c, string text, bool mine = false, bool system = false, DateTime at = default) =>
        c.Add(new ChatMessage { Text = text, IsMine = mine, IsSystem = system, SentAt = at });

    private static void Notify(UserWorld world, NotificationKind kind, string title, string? detail, DateTime at,
                               Guid reportId, Guid? conversationId, bool read) =>
        world.Notifications.Add(new AppNotification
        {
            Kind = kind, Title = title, Detail = detail, CreatedAt = at, ReportId = reportId,
            ConversationId = conversationId, IsRead = read,
        });

    private static Receipt MakeReceipt(Report report, MatchResult match, string owner, string handedBy) => new()
    {
        ReportId = report.Id,
        Reference = report.Reference,
        DocumentTitle = report.Title,
        ReturnedAt = report.UpdatedAt,
        City = report.City,
        OwnerName = owner,
        HandedByName = handedBy,
        Score = match.Score,
        VerificationCode = Receipt.BuildCode(report.Reference),
    };
}
