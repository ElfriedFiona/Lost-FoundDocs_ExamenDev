# Lost&Found Docs

Application mobile de restitution de documents officiels, académiques, administratifs et professionnels (Douala), développée en **.NET MAUI** (C#, XAML, MVVM).

## Prérequis
- Windows 10/11, **Visual Studio Community 2022 (17.14 ou plus récent)**
- Charge de travail **« Développement d'applications multiplateformes .NET »** avec le SDK **.NET 9**
- Android : SDK Android et un émulateur (Android Device Manager) ou un téléphone en mode développeur
- Windows : mode développeur activé (Paramètres > Confidentialité et sécurité > Pour les développeurs)

Cibles : `net9.0-android` et `net9.0-windows10.0.19041.0` (iOS et Mac Catalyst ne sont pas ciblés).

## Ouvrir et lancer
1. Ouvrir `LostFoundDocs.sln` dans Visual Studio.
2. Projet de démarrage : `LostFoundDocs`. Choisir **Windows Machine** ou **Android Emulator**.
3. Lancer avec **F5**.

Ligne de commande :
```
dotnet build LostFoundDocs/LostFoundDocs.csproj -f net9.0-windows10.0.19041.0
dotnet build LostFoundDocs/LostFoundDocs.csproj -f net9.0-android
dotnet test  LostFoundDocs.Tests/LostFoundDocs.Tests.csproj
```

## Compte de démonstration
| | |
|---|---|
| Téléphone | `6 77 12 48 21` (préfixe +237) |
| Mot de passe | `Demo@2026` |
| Code SMS « mot de passe oublié » | `123456` |

Un compte créé par l'inscription démarre **vide** (pour voir les états vides) et n'existe que pendant la session. **Profil > À propos > Mode démonstration** permet de forcer « toujours / jamais de correspondance », de couper la réponse automatique et de réinitialiser les données.

## Parcours à essayer
- **Perte** : Accueil > « J'ai perdu un document » (Diplôme, Douala) > 4 étapes > Déclarer > Confirmation > Recherche > Correspondance > Détails > Contacter > Restitution > Proposition (simulée) > Accepter > « J'ai récupéré mon document » > Preuve.
- **Trouvaille** : Accueil > « J'ai trouvé un document » (Passeport, Douala) > Déclarer > Recherche > Contacter le propriétaire > Restitution > lieu + créneau > Proposer > « J'ai remis le document » > Preuve.
- **Sans résultat** : mode « Jamais de correspondance » ou un type absent du jeu fictif (ex. Certificat).

La liste complète des cas de test est dans [docs/Cas_de_test.docx](docs/Cas_de_test.docx).

## Architecture
```
LostFoundDocs/
  Models/            modèles purs (Report, ReportDraft, MatchResult, Conversation, AppNotification, Receipt…)
  Services/
    Abstractions/    interfaces (IAuthService, IReportService, IMatchService, IConversationService,
                     INotificationService, IRestitutionService, INavigationService, IDialogService)
    Mock/            implémentations fictives en mémoire (SIMULATION) : DemoWorld, DemoSeeds, MatchSimulator…
    Navigation/      routes, navigation Shell, dialogues
    Validation/      règles de saisie, masquage, formats de date, règles de restitution
  ViewModels/        état et commandes (MVVM sans dépendance externe)
  Views/             Auth, Home, Declaration, Matching, Reports, Messages, Restitution, Profile, Shared
    Components/      composants réutilisables (FormField, DateField, SelectField, ChipGroup, MatchCard, BottomNavBar…)
  UI/                icônes Lucide, accès aux ressources, présentation
  Resources/         Colors.xaml, Styles.xaml, police d'icônes
LostFoundDocs.Tests/ tests xUnit des règles pures (validation, statuts, signalements, correspondance, restitution)
docs/                analyse des maquettes, cas de test, simulations, accessibilité, bilan
```

## Règles garanties par le frontend
- **Un signalement n'est enregistré qu'une fois** (brouillon `ReportDraft` → `Report`, idempotent, double appui protégé) ; ensuite seul son **statut** évolue (`ReportStatusRules`).
- **Cohérence chronologique** : toutes les dates fictives sont relatives à la date du jour ; validateurs `DemoDataValidator` / `DemoWorldValidator` et moniteur de cohérence en Debug (fenêtre Sortie).
- **Date de naissance facultative**, numéro de document jamais obligatoire, libellés adaptés à chaque type de document.

## Icônes
Police **Lucide** (licence ISC), `Resources/Fonts/lucide.ttf`, alias `Lucide`, glyphes dans `UI/Icons.cs`. Voir `THIRD_PARTY_NOTICES.md`.

## Documentation
[Bilan](docs/BILAN.md) · [Simulations](docs/SIMULATIONS.md) · [Accessibilité](docs/ACCESSIBILITE.md) · Analyse des maquettes et cas de test (Word) dans `docs/`.
