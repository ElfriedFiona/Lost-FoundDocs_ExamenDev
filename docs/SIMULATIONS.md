# Fonctionnalités simulées (frontend uniquement)

Chaque simulation est marquée `// SIMULATION` dans le code et signalée dans l'interface. Le backend devra fournir, pour chacune, l'équivalent réel.

| Fonction | Simulation actuelle | Où dans le code | Le backend devra fournir |
|---|---|---|---|
| Connexion / inscription | Comptes en mémoire ; mot de passe gardé en clair ; compte de démonstration | `MockAuthService`, `DemoSettings` | Authentification sécurisée (hachage, jetons, sessions), vérification du numéro |
| Mot de passe oublié | Code fixe `123456`, aucun SMS | `MockAuthService`, `ForgotPasswordViewModel` | Envoi réel de SMS, expiration du code |
| Signalements | Liste en mémoire par utilisateur ; données du compte démo générées avec des dates relatives | `MockReportService`, `DemoData` | Base de données, API, historique |
| Recherche de correspondance | Règle locale (type + ville) ou forcée (toujours / jamais) ; score fictif 89-94 % | `MatchSimulator`, `MockMatchService` | Moteur de correspondance serveur, scoring réel, recherche continue |
| « Recherche active vérifiée il y a 12 min » | Heure fictive | `HomeViewModel.SearchSummary` | Horodatage réel de la dernière comparaison |
| Messagerie | Messages locaux ; réponse automatique fictive (marquée « Réponse simulée ») | `MockConversationService` | Messagerie entre utilisateurs, modération |
| Notifications | Générées localement | `MockNotificationService`, `DemoSeeds` | Notifications push et SMS réelles |
| Photos | Choix « Prendre une photo / Galerie » simulé, nom de fichier fictif | `DeclarationViewModelBase.PickPhotoAsync` | Accès appareil photo/galerie, stockage, contrôle de taille et de format |
| Rendez-vous de restitution | Proposition de l'autre personne déclenchée par un bouton de démonstration ; contre-proposition acceptée automatiquement | `MockRestitutionService` | Négociation réelle entre les deux parties |
| Reçu et preuve | Reçu fictif ; code de vérification dérivé de la référence ; QR dessiné localement, non scannable ; « Télécharger » = état « Reçu enregistré » | `Receipt.BuildCode`, `FakeQrView`, `ReceiptViewModel` | Reçu signé, QR vérifiable, génération PDF |
| Préférences de notification | Gardées en mémoire, sans effet | `NotificationPrefsViewModel`, `DemoSettings` | Préférences persistées côté serveur |
| Support | Bouton simulé | `HelpViewModel` | Canal de support |
