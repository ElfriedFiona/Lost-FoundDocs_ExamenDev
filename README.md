# Lost&Found Docs

Application mobile de restitution de documents officiels, académiques, administratifs et professionnels (Douala), développée en **.NET MAUI** (C#, XAML, MVVM).

> ## ⚠️ Frontend uniquement
> Aucun backend, aucune API, aucune base de données, aucune authentification réelle, aucune notification push, aucune messagerie entre appareils. Les données sont **fictives et gardées en mémoire** ; elles disparaissent à la fermeture de l'application. Toutes les simulations sont signalées par `// SIMULATION` dans le code et par un bandeau « Mode démonstration » dans l'interface.

## Prérequis
- Windows 10/11, **Visual Studio Community 2022 (17.14 ou plus récent)**
- Charge de travail **« Développement d'applications multiplateformes .NET »** (Visual Studio Installer), avec le SDK **.NET 9**
- Pour Android : SDK Android et un émulateur (Android Device Manager) ou un téléphone en mode développeur
- Pour Windows : mode développeur activé (Paramètres > Confidentialité et sécurité > Pour les développeurs)

Cibles : `net9.0-android` et `net9.0-windows10.0.19041.0` (iOS et Mac Catalyst ne sont pas ciblés).

## Ouvrir et lancer
1. Ouvrir `LostFoundDocs.sln` dans Visual Studio.
2. Choisir la cible **Windows Machine** ou **Android Emulator** dans la barre d'outils.
3. Lancer avec **F5**.

En ligne de commande :
```
dotnet build LostFoundDocs/LostFoundDocs.csproj -f net9.0-android
dotnet build LostFoundDocs/LostFoundDocs.csproj -f net9.0-windows10.0.19041.0
```

## Compte de démonstration
| | |
|---|---|
| Téléphone | `6 77 12 48 21` (préfixe +237) |
| Mot de passe | `Demo@2026` |
| Code SMS « mot de passe oublié » | `123456` |

Le bouton « Utiliser le compte de démonstration » de l'écran de connexion remplit les champs. Un compte créé via l'inscription est vide (permet de tester les états vides) et n'existe que pendant la session.

## Architecture
```
LostFoundDocs/
  Models/            modèles purs (Report, ReportDraft, UserAccount, règles de statut)
  Services/
    Abstractions/    interfaces (IAuthService, IReportService, INavigationService, IDialogService)
    Mock/            implémentations fictives en mémoire (SIMULATION) + données de démonstration
    Navigation/      routes, navigation Shell, dialogues
    Validation/      règles de saisie, masquage, formats de date
  ViewModels/        état et commandes (MVVM sans dépendance externe)
  Views/             écrans (Auth, Home, Shared) et Components (composants réutilisables)
  UI/                icônes, accès aux ressources, présentation
  Resources/         Colors.xaml, Styles.xaml, police d'icônes Lucide
```
Pour brancher un backend plus tard : remplacer les deux lignes `MockAuthService` / `MockReportService` de `MauiProgram.cs` par des implémentations HTTP des mêmes interfaces.

## Icônes
Police d'icônes **Lucide** (licence ISC), fichier `Resources/Fonts/lucide.ttf`, alias `Lucide`. Les glyphes sont déclarés dans `UI/Icons.cs`. Voir `THIRD_PARTY_NOTICES.md`.

## Règles garanties par le frontend
- **Un signalement n'est enregistré qu'une fois** : un brouillon (`ReportDraft`) devient un `Report` à la validation, un double appui renvoie le même signalement ; ensuite seul son **statut** évolue (transitions contrôlées dans `ReportStatusRules`).
- **Dates cohérentes** : toutes les dates fictives sont calculées à partir de la date du jour ; `DemoDataValidator` vérifie les invariants (résultat dans la fenêtre Sortie en Debug).
- **Date de naissance facultative**, numéro de document jamais obligatoire.

## Parcours disponibles
- Déclaration de perte en quatre étapes et déclaration de trouvaille, validation par champ, numéro facultatif, photo fictive et confirmation avant abandon.
- Recherche déterministe : CNI ou diplôme à Douala pour une perte ; passeport ou diplôme à Douala pour une trouvaille. Le profil → À propos permet de forcer un résultat positif ou négatif.
- Signalements filtrés, détails et historique des changements de statut, contestation et annulation.
- Conversations locales liées aux correspondances, compteurs de non-lus et notifications avec navigation vers leur cible.
- Proposition, acceptation et confirmation de restitution dans les deux rôles ; reçu de démonstration non vérifiable.
- Informations personnelles, sécurité, préférences de notification, confidentialité, aide et réglages de démonstration.

Le thème clair, les couleurs et les composants de la phase A sont conservés. La déconnexion vide les signalements de session. À propos permet de repartir avec un compte vide ou les exemples fictifs.

## Simulations
| Fonction | Comportement local | À fournir par le backend |
|---|---|---|
| Comptes et mot de passe | Mémoire uniquement, code SMS fixe | Hachage, jetons, SMS et récupération réelle |
| Signalements et correspondances | Objets en mémoire, règle type/ville, score fictif | Persistance, autorisation et moteur de recherche |
| Photos | Illustration fictive et refus de fichier simulé | Sélection native, stockage privé et validation serveur |
| Messages et notifications | Aucune transmission entre appareils | Messagerie, push et synchronisation |
| Rendez-vous et reçus | Propositions locales, QR non scannable | Confirmation partagée et reçu signé |
| Télécharger le reçu | Confirmation simulée, aucun fichier PDF créé | Génération et téléchargement réels |

Les interfaces des services sont dans `Services/Abstractions`; leurs implémentations fictives sont dans `Services/Mock`.

## Vérifications des règles métier
Aucune dépendance de test supplémentaire. Depuis la racine, avec .NET 9 :

```sh
dotnet run --project Tests/LostFoundDocs.Tests.csproj
```

Le programme vérifie la validation, l’idempotence, les dates, les transitions, les deux rôles de restitution, les compteurs, les états vides et l’isolation entre comptes. Un échec termine le programme avec une erreur.

## Recette manuelle
1. Remplir le compte de démonstration et se connecter.
2. Refuser des identifiants incorrects et vérifier le message.
3. Créer un compte et constater les listes vides.
4. Vérifier le code de récupération simulé et son erreur.
5. Ouvrir une perte sans choisir de type, puis continuer.
6. Choisir Autre et vérifier l’intitulé obligatoire.
7. Vérifier le nom obligatoire pour une perte.
8. Vérifier un numéro trop court et un numéro absent.
9. Revenir à l’étape précédente et retrouver la saisie.
10. Vérifier un lieu absent et les raccourcis de lieu.
11. Refuser un numéro complet dans les notes.
12. Ajouter, remplacer et supprimer une photo simulée.
13. Simuler une photo invalide.
14. Annuler puis confirmer l’abandon du formulaire.
15. Déclarer une perte et vérifier qu’elle n’apparaît qu’une fois.
16. Vérifier une correspondance et ses informations masquées.
17. Signaler une erreur et constater le statut contesté.
18. Forcer Aucun résultat dans À propos et refaire une recherche.
19. Déclarer une trouvaille avec seulement un nom ou seulement un numéro.
20. Filtrer Perdus/Trouvés et ouvrir un détail.
21. Ouvrir une conversation, vérifier le non-lu et envoyer un message local.
22. Accepter une proposition côté propriétaire et confirmer la récupération.
23. Proposer un créneau futur côté trouveur et confirmer la remise.
24. Ouvrir la preuve depuis une notification et simuler son enregistrement.
25. Modifier le profil, vérifier les préférences et réinitialiser le compte de démonstration.

Android et Windows restent les cibles .NET 9 du projet. Une vérification avec un autre SDK ne remplace pas la compilation dans Visual Studio sur ces cibles.
