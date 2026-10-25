# API v1 : `/api/v1`

Format JSON (camelCase, énumérations en texte), dates en UTC (`...Z`). Authentification : en-tête `Authorization: Bearer <accessToken>`. Erreurs : `application/problem+json` avec `status`, `code`, `detail` et, pour une validation, `errors` (champ → messages).

| Méthode et route | Auth | Rôle |
|---|---|---|
| `POST /auth/register` `{fullName, phone, email?, password}` | non | Crée le compte, ouvre une session, envoie le code SMS (201 ; 409 `phone_in_use`) |
| `POST /auth/login` `{phone, password}` | non | Connexion (401 générique ; 429 après 5 échecs) |
| `POST /auth/refresh` `{refreshToken}` | non | Nouveau couple de jetons (rotation) |
| `POST /auth/logout` | oui | Révoque la session |
| `POST /auth/phone/send-code` · `POST /auth/phone/verify` `{code}` | oui | Vérification du numéro |
| `POST /auth/password/forgot` `{phone}` | non | 202 toujours (pas d'énumération de comptes) |
| `POST /auth/password/verify-code` `{phone, code}` | non | Renvoie `resetToken` (15 min, usage unique) |
| `POST /auth/password/reset` `{resetToken, newPassword}` | non | Change le mot de passe, révoque les sessions |
| `GET/PUT /me` · `POST /me/password` · `DELETE /me` | oui | Profil, changement de mot de passe, suppression du compte |
| `POST /reports` | oui | Crée un signalement (idempotent par `clientRequestId`) et lance la recherche |
| `GET /reports?kind=&status=` · `GET /reports/{id}` | oui | Liste / détail (propriétaire uniquement) |
| `POST /reports/{id}/search` | oui | Recherche (ou relance) ; renvoie `{found, match?}` |
| `POST /reports/{id}/cancel` · `/resolve-recovered` | oui | Annulation ; clôture « déjà récupéré » après contestation |
| `POST /reports/{id}/photo` (multipart, champ `file`) · `GET /reports/{id}/photo` | oui | Photo JPEG/PNG ≤ 10 Mo |
| `GET /matches` · `GET /matches/{id}` | oui | Correspondances de l'utilisateur ; nom court de l'autre partie |
| `POST /matches/{id}/dispute` `{reason}` | oui | Contestation : l'autre fiche repart en recherche |
| `GET /conversations` · `GET /conversations/{id}/messages?after=&limit=` | oui | Messagerie (lecture incrémentale) |
| `POST /conversations/{id}/messages` `{clientMessageId, text}` · `POST /conversations/{id}/read` | oui | Envoi idempotent, marquage lu |
| `GET /notifications?since=&limit=` · `/notifications/unread-count` · `POST /notifications/{id}/read` · `/read-all` | oui | Notifications |
| `GET /restitution/places` | oui | Lieux publics recommandés |
| `GET /matches/{id}/restitution` · `POST .../propose {placeName, slot}` · `.../accept` · `.../confirm` | oui | Rendez-vous ; restitution enregistrée quand **les deux** parties ont confirmé |
| `GET /matches/{id}/receipt` · `GET /matches/{id}/receipt/pdf` | oui | Reçu (avec matrice QR) et PDF |
| `GET /receipts/verify/{code}` · `GET /verify/{code}` (page HTML) | non | Vérification publique d'un reçu (aucune donnée sensible) |
| `GET /health` | non | Santé |

Cycle d'un signalement : `Searching → MatchFound → MatchConfirmed → Returned` ; `MatchDisputed` après contestation ; `Expired` (90 jours sans correspondance) ; `Cancelled`.
