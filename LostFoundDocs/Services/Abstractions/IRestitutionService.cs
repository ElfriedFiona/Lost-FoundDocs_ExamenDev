using LostFoundDocs.Models;

namespace LostFoundDocs.Services.Abstractions;

public interface IRestitutionService
{
    RestitutionProposal? GetProposal(Guid reportId);
    Receipt? GetReceipt(Guid reportId);

    /// <summary>Propose un rendez-vous (lieu public + créneau futur). Fait passer le signalement à « Correspondance confirmée ».</summary>
    Task<RestitutionProposal> ProposeAsync(Guid reportId, MeetingPlace place, DateTime slot);

    /// <summary>SIMULATION : l'autre personne propose un rendez-vous (côté propriétaire).</summary>
    Task<RestitutionProposal> SimulateCounterpartProposalAsync(Guid reportId);

    Task AcceptAsync(Guid reportId);

    /// <summary>Confirme la remise : statut « Restitué », reçu, notification, conversation clôturée.</summary>
    Task<Receipt> ConfirmAsync(Guid reportId);
}
