# Bilan du frontend (phases A, B, C et finitions)

Date : octobre 2026 · .NET 9 · MAUI · cibles Android et Windows · **frontend uniquement**.

## Ce qui a été vérifié, et ce qui ne l'a pas été
| Élément | Vérification |
|---|---|
| Phase A (thème, composants, connexion, inscription, accueil) | Compilée et lancée dans Visual Studio par le développeur. |
| Phases B, C et finitions (écrans MAUI) | **Écrits sans compilateur MAUI** (non disponible dans l'environnement de rédaction). Contrôles statiques effectués : XAML bien formé, chaque liaison de chaque page existe dans son view-model, chaque ressource, style et icône référencé existe. **À compiler dans Visual Studio.** |
| Logique pure (modèles, validation, services fictifs, règles de statut, correspondance, restitution, cohérence chronologique) | **Compilée (SDK .NET 8, sans warning) et exécutée** : 69 vérifications de scénarios complets et 46 tests (mêmes fichiers que `LostFoundDocs.Tests`) réussis. xUnit lui-même n'a pas pu être téléchargé : les tests ont été exécutés avec un exécuteur minimal ; ils restent à lancer avec `dotnet test`. |
| Rendu visuel face aux maquettes, TalkBack / Narrateur, tailles de police | **Non vérifiés** : comparaison à faire sur appareil. |

## Écrans terminés (27 pages XAML + composants)
| Maquette (31 pages du PDF) | Implémentation |
|---|---|
| Lancement | `SplashPage` (fond bleu : hypothèse, le PDF ne donne que le texte) |
| Connexion, Inscription | `LoginPage`, `RegisterPage` (+ `ForgotPasswordPage`, ajoutée) |
| Accueil | `HomePage` |
| Document perdu, étapes 1 à 4, états d'erreur, récapitulatif | `DeclareLostPage` |
| Recherche de correspondance (perte et trouvaille) | `SearchPage` |
| Correspondance trouvée (perte et trouvaille), modale « Signaler une erreur », état après signalement | `MatchResultPage` + `ReasonDialog` |
| Document trouvé (vide, rempli, erreur) | `DeclareFoundPage` |
| Conversation, Messages | `ConversationPage`, `MessagesPage` |
| Modale « Confirmer la restitution ? », Restitution du document | `RestitutionPage` (+ confirmation native) |
| Document restitué, Preuve de restitution (+ « Reçu enregistré ») | `RestitutedPage`, `ReceiptPage` |
| Mes signalements (Perdus, Trouvés), modale « Nouveau signalement » | `ReportsPage` (feuille d'actions native pour « + ») |
| Profil, Notifications | `ProfilePage`, `NotificationsPage` |

Écrans ajoutés (absents du PDF) : confirmation de déclaration, aucune correspondance, détails d'une correspondance, détails d'un signalement, mot de passe oublié, restitution côté propriétaire, six sous-pages du profil (informations, sécurité, notifications, confidentialité, aide, à propos), états vides, feuille de choix de photo.

## Interactions réellement fonctionnelles
Connexion, inscription, déconnexion et mot de passe oublié (comptes en mémoire) · formulaires perdu (4 étapes) et trouvé avec libellés adaptés au type, validation, erreurs, retour sans perte de données, abandon confirmé · enregistrement unique d'un signalement et évolution de son statut · liste avec onglets, compteurs, filtre Tous/Actifs/Terminés et états vides · détails avec actions selon le statut (rechercher, voir la correspondance, conversation, organiser, preuve, clôturer, annuler) · résultat avec ou sans correspondance, relance, signalement d'erreur · conversations multiples, envoi de messages, non lus, pastille de l'onglet Messages · notifications (aujourd'hui / plus tôt, tout marquer lu, ouverture de la cible) · organisation de la restitution, proposition, acceptation, confirmation, reçu · pages du profil avec enregistrement local · réinitialisation des données de démonstration.

## Fonctionnalités simulées
Voir [SIMULATIONS.md](SIMULATIONS.md) : authentification, SMS, recherche de correspondance, messagerie et réponse automatique, notifications, photos, proposition de rendez-vous de l'autre personne, reçu, QR code, téléchargement.

## Limites et points restants
- **Compilation et rendu** des phases B/C/finitions à confirmer (voir plus haut) ; je corrigerai à partir des messages d'erreur.
- **Modales** : seule « Signaler une erreur » est stylée comme la maquette ; « Confirmer la restitution ? », « Se déconnecter ? », « Abandonner ? », « Annuler le signalement ? » et « Nouveau signalement » utilisent les dialogues natifs (comportement identique, apparence différente).
- **Sélecteur de date** : saisie « jj/mm/aaaa » + calendrier natif ; choisir au calendrier la date déjà affichée par défaut (aujourd'hui) ne déclenche pas d'événement : la saisir au clavier.
- **Photo** : aucune caméra ni galerie réelle (simulation).
- **Police** : OpenSans (fournie par le modèle .NET) au lieu d'Inter ; icônes Lucide.
- **Écran de lancement** : conçu d'après le texte seul du PDF.
- **Thème** : clair uniquement (la maquette n'existe qu'en clair).
- **Windows** : le retrait des traits natifs des champs (Entry, Picker) est à valider visuellement.
- Cibles iOS et Mac Catalyst non ciblées.

## Lancer l'application
Voir le [README](../README.md) : Visual Studio 2022 17.14, charge de travail MAUI, SDK .NET 9 ; ouvrir `LostFoundDocs.sln`, choisir Windows Machine ou Android Emulator, F5. Compte de démonstration : `6 77 12 48 21` / `Demo@2026`.
