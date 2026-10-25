# Lost&Found Docs : backend (API ASP.NET Core 9 + SQLite)

API REST de l'application mobile : comptes, signalements, recherche de correspondance, messagerie, notifications, restitution à double confirmation et reçus vérifiables (QR code + PDF).

## Démarrer en local
```
cd src/LostFoundDocs.Api
dotnet run            # environnement Development : http://localhost:5080
```
La base SQLite (`data/lostfound.db`) et le dossier des photos sont créés automatiquement. En Development, le code SMS est renvoyé dans la réponse (`devOtp`) et écrit dans les journaux ; aucun SMS réel n'est envoyé.

## Docker (production)
```
cp .env.example .env      # renseigner SERVER_SECRET (32 caractères minimum) et PUBLIC_BASE_URL
docker compose up -d --build
```
Placez un reverse proxy HTTPS devant l'API (puis `TRUST_FORWARDED_HEADERS=true`). L'application mobile doit parler en HTTPS en production.

## Configuration (appsettings.json / variables d'environnement `Section__Clé`)
| Clé | Rôle |
|---|---|
| `Security:ServerSecret` | **Obligatoire en production** (≥ 32 car.) : empreintes des numéros et codes SMS |
| `Security:ExposeOtpInResponse` | Développement uniquement (refusé en production) |
| `Security:AuthRequestsPerMinute`, `MessagesPerMinute` | Limitation de débit |
| `Security:MaxFailedLogins`, `LockoutMinutes` | Verrouillage du compte (5 essais, 15 min) |
| `Sms:Provider` | `Log` (défaut) ou `Http` (passerelle SMS : `Sms:Url`, `AuthHeaderValue`, `BodyTemplate`) |
| `Storage:DatabasePath`, `PhotosPath` | Emplacements des données (à sauvegarder) |
| `Matching:*`, `Restitution:*` | Seuils de correspondance, lieux publics, horaires de rendez-vous |
| `Public:BaseUrl` | Adresse publique (contenue dans le QR code des reçus) |

## Sécurité
Mots de passe PBKDF2-SHA256 (210 000 itérations, sel aléatoire) · jetons de session opaques de 256 bits, **seul leur hachage est stocké** (déconnexion immédiate, rotation du refresh) · verrouillage après 5 échecs · réponses identiques que le compte existe ou non (connexion, mot de passe oublié) · codes SMS à 6 chiffres hachés, expirants, limités en essais et en nombre · numéros de documents et dates de naissance **jamais stockés** (empreintes HMAC + 4 derniers caractères / année) · photos vérifiées par signature (JPEG/PNG), 10 Mo max, visibles du seul propriétaire · limitation de débit · en-têtes de sécurité · erreurs RFC 7807 sans détail interne · toutes les dates en UTC.

## API
Voir [docs/API.md](docs/API.md).

## Tests
- `dotnet test tests/LostFoundDocs.Api.Tests` : logique pure (correspondance, normalisation, cryptographie, QR, PDF, statuts) et tâche périodique.
- `python3 tests/e2e.py http://localhost:5080` : parcours complet contre un serveur lancé (150 vérifications : inscription, connexion et verrouillage, correspondance, messagerie, restitution à double confirmation, reçu, contestation, photos, suppression du compte). Pour l'exécuter, lancez le serveur avec `Security__AuthRequestsPerMinute=100000 Security__MessagesPerMinute=100000`.

## État de la vérification (à lire)
Le serveur a été **compilé et exécuté** avec ses tests de bout en bout (142 vérifications réussies) et 22 tests unitaires, **mais sur une imitation en mémoire d'EF Core** : l'environnement de rédaction n'avait pas accès à NuGet. Le vrai fournisseur SQLite n'a donc pas été exécuté ici : lancez `dotnet test` puis `e2e.py` chez vous avant toute mise en ligne.
Limites connues : le schéma est créé par `EnsureCreated` (pas de migrations : à introduire avant la première évolution du schéma) ; pas de notifications push (l'application interroge l'API) ; l'envoi de SMS réels demande une passerelle configurée.
Le branchement de l'application mobile sur ce serveur n'est pas encore livré.
