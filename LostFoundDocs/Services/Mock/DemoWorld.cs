using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Mock;

/// <summary>État fictif d'un utilisateur : correspondances, conversations, notifications, restitutions.</summary>
public sealed class UserWorld
{
    public List<Conversation> Conversations { get; } = new();
    public List<AppNotification> Notifications { get; } = new();
    public Dictionary<Guid, MatchResult> Matches { get; } = new();
    public Dictionary<Guid, MatchOutcome> Outcomes { get; } = new();
    public Dictionary<Guid, RestitutionProposal> Proposals { get; } = new();
    public Dictionary<Guid, Receipt> Receipts { get; } = new();
}

/// <summary>
/// SIMULATION (frontend uniquement) : conteneur unique des données fictives par utilisateur.
/// Le compte de démonstration reçoit un jeu de données cohérent (voir <see cref="DemoSeeds"/>) ; un compte neuf démarre vide.
/// </summary>
public sealed class DemoWorld : IResettable
{
    private readonly IAuthService _auth;
    private readonly IReportService _reports;
    private readonly Dictionary<Guid, UserWorld> _worlds = new();

    public DemoWorld(IAuthService auth, IReportService reports)
    {
        _auth = auth;
        _reports = reports;
    }

    public object Gate { get; } = new();
    public event EventHandler? Cleared;

    public UserWorld Current
    {
        get
        {
            var user = _auth.CurrentUser;
            if (user is null) return new UserWorld();
            lock (Gate)
            {
                if (!_worlds.TryGetValue(user.Id, out var world))
                {
                    world = new UserWorld();
                    _worlds[user.Id] = world;
                    if (user.IsDemoAccount)
                    {
                        DemoSeeds.Populate(world, _reports.Reports, user, Clock.Now);
                        DemoWorldValidator.LogValidation(world, _reports.Reports, Clock.Now);
                    }
                }
                return world;
            }
        }
    }

    public void Reset()
    {
        lock (Gate) { _worlds.Clear(); }
        Cleared?.Invoke(this, EventArgs.Empty);
    }
}
