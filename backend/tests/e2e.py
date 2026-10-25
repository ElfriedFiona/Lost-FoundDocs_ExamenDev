#!/usr/bin/env python3
"""Vérification de bout en bout de l'API (serveur lancé en local). Usage : python3 e2e.py [http://localhost:5080]"""
import json, sys, uuid, datetime, urllib.request, urllib.error

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080"
ok = ko = 0

def call(method, path, body=None, token=None, raw=False, headers=None, data=None):
    req = urllib.request.Request(BASE + path, method=method)
    if token: req.add_header("Authorization", "Bearer " + token)
    payload = data
    if body is not None:
        payload = json.dumps(body).encode()
        req.add_header("Content-Type", "application/json")
    for k, v in (headers or {}).items(): req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, payload, timeout=20) as r:
            content = r.read()
            return r.status, (content if raw else (json.loads(content) if content else None))
    except urllib.error.HTTPError as e:
        content = e.read()
        try: return e.code, json.loads(content) if content else None
        except Exception: return e.code, content

def check(cond, name, extra=""):
    global ok, ko
    if cond: ok += 1
    else:
        ko += 1; print("ÉCHEC :", name, extra)

today = datetime.date.today()
def d(n): return (today + datetime.timedelta(days=n)).isoformat()
uniq = str(uuid.uuid4().int)[:6]

def register(name, suffix, pw="Motdepasse1"):
    phone = "6" + uniq[:4] + suffix  # 9 chiffres commençant par 6
    s, r = call("POST", "/api/v1/auth/register", {"fullName": name, "phone": phone, "password": pw})
    check(s == 201, f"inscription {name}", f"{s} {r}")
    return phone, r

def new_report(token, kind, dtype, name=None, number=None, birth=None, issuer=None, event=None, city="Douala", place="Akwa", cid=None, title=None):
    body = {"clientRequestId": cid or str(uuid.uuid4()), "kind": kind, "documentType": dtype, "holderName": name, "number": number,
            "birthDate": birth, "issuer": issuer, "eventDate": event or d(-1), "city": city, "place": place, "customTitle": title}
    return call("POST", "/api/v1/reports", body, token)

# ---------- Santé et erreurs ----------
s, r = call("GET", "/health"); check(s == 200 and r["status"] == "ok", "health")
s, r = call("GET", "/api/v1/reports"); check(s == 401 and r["code"] == "unauthorized", "sans jeton → 401", f"{s} {r}")
s, r = call("GET", "/api/v1/reports", token="jeton-bidon"); check(s == 401, "jeton invalide → 401")

# ---------- Inscription ----------
s, r = call("POST", "/api/v1/auth/register", {"fullName": "A", "phone": "12", "email": "abc", "password": "1234567"})
check(s == 400 and set(r["errors"]) >= {"fullName", "phone", "email", "password"}, "inscription invalide : erreurs par champ", f"{s} {r}")
phoneA, ra = register("Aïcha Mbarga", "0001")
tokA = ra["tokens"]["accessToken"]
check(ra["user"]["phoneVerified"] is False and ra["devOtp"] and len(ra["devOtp"]) == 6, "OTP dev renvoyé")
s, r = call("POST", "/api/v1/auth/register", {"fullName": "Doublon", "phone": phoneA, "password": "Motdepasse1"})
check(s == 409 and r["code"] == "phone_in_use", "numéro déjà utilisé → 409")
phoneB, rb = register("Brice Tchoumi", "0002"); tokB = rb["tokens"]["accessToken"]

# ---------- Vérification du téléphone ----------
s, r = call("POST", "/api/v1/auth/phone/verify", {"code": "000000"}, tokA); check(s == 400 and r["code"] == "invalid_code", "code SMS faux")
s, r = call("POST", "/api/v1/auth/phone/verify", {"code": ra["devOtp"]}, tokA); check(s == 200 and r["phoneVerified"], "téléphone vérifié", f"{s} {r}")
s, r = call("POST", "/api/v1/auth/phone/send-code", None, tokA); check(s == 409 and r["code"] == "already_verified", "déjà vérifié")

# ---------- Connexion, verrouillage ----------
s, r = call("POST", "/api/v1/auth/login", {"phone": phoneA, "password": "Motdepasse1"}); check(s == 200 and r["user"]["fullName"] == "Aïcha Mbarga", "connexion")
tokA = r["tokens"]["accessToken"]; refreshA = r["tokens"]["refreshToken"]
s1, r1 = call("POST", "/api/v1/auth/login", {"phone": phoneA, "password": "mauvais"})
s2, r2 = call("POST", "/api/v1/auth/login", {"phone": "699999999", "password": "mauvais"})
check(s1 == 401 and s2 == 401 and r1["detail"] == r2["detail"], "identifiants faux : message générique identique")
phoneL, rl = register("Lock Test", "0003")
codes = [call("POST", "/api/v1/auth/login", {"phone": phoneL, "password": "faux"})[0] for _ in range(5)]
check(codes == [401, 401, 401, 401, 401], "5 échecs → 401", str(codes))
s, r = call("POST", "/api/v1/auth/login", {"phone": phoneL, "password": "Motdepasse1"}); check(s == 429, "compte verrouillé → 429", f"{s} {r}")

# ---------- Jetons : rafraîchissement, déconnexion ----------
s, r = call("POST", "/api/v1/auth/refresh", {"refreshToken": refreshA}); check(s == 200 and r["accessToken"] != tokA, "rafraîchissement")
newTok = r["accessToken"]; newRefresh = r["refreshToken"]
s, _ = call("GET", "/api/v1/me", token=tokA); check(s == 401, "ancien jeton d'accès invalidé après rotation")
s, _ = call("POST", "/api/v1/auth/refresh", {"refreshToken": refreshA}); check(s == 401, "ancien refresh refusé")
tokA = newTok
s, r = call("GET", "/api/v1/me", token=tokA); check(s == 200 and r["phone"] == phoneA, "profil")
s, r = call("PUT", "/api/v1/me", {"fullName": "Aïcha Mbarga", "email": "aicha@exemple.cm"}, tokA); check(s == 200 and r["email"] == "aicha@exemple.cm", "mise à jour profil")

# ---------- Mot de passe oublié ----------
s, r = call("POST", "/api/v1/auth/password/forgot", {"phone": "699999999"}); check(s == 202 and r["devOtp"] is None, "oubli : compte inconnu → même réponse, sans code")
phoneR, rr = register("Reset Test", "0004")
s, r = call("POST", "/api/v1/auth/password/forgot", {"phone": phoneR}); check(s == 202 and r["devOtp"], "oubli : code envoyé"); code = r["devOtp"]
s, r = call("POST", "/api/v1/auth/password/verify-code", {"phone": phoneR, "code": "000000"}); check(s == 400, "code de réinitialisation faux")
s, r = call("POST", "/api/v1/auth/password/verify-code", {"phone": "699999999", "code": code}); check(s == 400, "compte inconnu : même erreur")
s, r = call("POST", "/api/v1/auth/password/verify-code", {"phone": phoneR, "code": code}); check(s == 200 and r["resetToken"], "code valide → jeton de réinitialisation")
reset = r["resetToken"]
s, r = call("POST", "/api/v1/auth/password/reset", {"resetToken": reset, "newPassword": "Nouveau@2027"}); check(s == 204, "réinitialisation")
s, _ = call("POST", "/api/v1/auth/password/reset", {"resetToken": reset, "newPassword": "Autre@2028"}); check(s == 400, "jeton de réinitialisation à usage unique")
s, _ = call("GET", "/api/v1/me", token=rr["tokens"]["accessToken"]); check(s == 401, "sessions révoquées après réinitialisation")
s, r = call("POST", "/api/v1/auth/login", {"phone": phoneR, "password": "Nouveau@2027"}); check(s == 200, "connexion avec le nouveau mot de passe")

# ---------- Changement de mot de passe ----------
s, r = call("POST", "/api/v1/me/password", {"currentPassword": "faux", "newPassword": "Autre@2028"}, tokA); check(s == 400 and r["code"] == "invalid_current_password", "mot de passe actuel faux")
s, r = call("POST", "/api/v1/me/password", {"currentPassword": "Motdepasse1", "newPassword": "Motdepasse1"}, tokA); check(s == 400, "nouveau identique refusé")

# ---------- Signalements ----------
s, r = new_report(tokA, "Lost", "Diploma", name=None); check(s == 400 and "holderName" in r["errors"], "perte sans nom refusée")
s, r = new_report(tokA, "Found", "Passport"); check(s == 400 and "holderName" in r["errors"], "trouvaille sans nom ni numéro refusée")
s, r = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", event=d(3)); check(s == 400 and "eventDate" in r["errors"], "date future refusée")
s, r = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", birth=d(-30), event=d(-60)); check(s == 400 and "eventDate" in r["errors"], "perte avant naissance refusée")
s, r = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", number="ab1"); check(s == 400 and "number" in r["errors"], "numéro invalide")
s, r = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", event=d(-2)); check(s == 201 and r["status"] == "Searching", "perte sans numéro ni naissance acceptée (aucune correspondance)")
weak = r
cid = str(uuid.uuid4())
s, r = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", number="D112048213", birth="1998-05-04", issuer="Université de Douala", event=d(-2), cid=cid)
check(s == 201 and r["maskedNumber"] == "•••• 8213" and r["birthYear"] == 1998 and r["reference"].startswith("LF-"), "perte complète créée", f"{s} {r}")
lostA = r
s, r2 = new_report(tokA, "Lost", "Diploma", name="Aïcha Mbarga", number="D112048213", birth="1998-05-04", issuer="Université de Douala", event=d(-2), cid=cid)
check(s == 200 and r2["id"] == lostA["id"], "idempotence : même clientRequestId → même signalement")
s, r = call("GET", "/api/v1/reports", token=tokA); check(s == 200 and len(r) == 2 and len({x["reference"] for x in r}) == 2, "liste sans doublon")
check("D112048213" not in json.dumps(lostA) and "1998-05-04" not in json.dumps(lostA), "numéro complet et naissance complète jamais renvoyés")
s, r = call("GET", f"/api/v1/reports/{lostA['id']}", token=tokB); check(s == 404, "signalement d'un autre utilisateur → 404")

# ---------- Recherche sans résultat ----------
s, r = call("POST", f"/api/v1/reports/{lostA['id']}/search", None, tokA); check(s == 200 and r["found"] is False, "recherche : aucun résultat")

# ---------- Correspondance (trouvaille de B) ----------
s, r = new_report(tokB, "Found", "Diploma", name="MBARGA Aicha", number="d 112048213", birth="1998-05-04", issuer="universite de douala", event=d(-1))
check(s == 201 and r["status"] == "MatchFound" and r["matchId"], "trouvaille → correspondance immédiate", f"{s} {r}")
foundB = r
s, r = call("GET", f"/api/v1/reports/{lostA['id']}", token=tokA); check(r["status"] == "MatchFound" and r["matchId"] == foundB["matchId"], "fiche de A passée en « correspondance trouvée »")
s, r = call("POST", f"/api/v1/reports/{lostA['id']}/search", None, tokA)
check(s == 200 and r["found"] and r["match"]["score"] >= 85 and r["match"]["strength"] == "Correspondance forte", "recherche : correspondance forte", f"{r}")
matchId = r["match"]["id"]
m = r["match"]
check(m["counterpartName"] == "Brice T." and m["userSide"] == "Lost" and len(m["criteria"]) == 7, "correspondance vue du propriétaire (nom court de l'autre)", f"{m}")
check(all(c["state"] == "Match" for c in m["criteria"] if c["name"] in ("Type de document", "Nom", "Numéro", "Date de naissance")), "critères comparés")
s, mB = call("GET", f"/api/v1/matches/{matchId}", token=tokB); check(s == 200 and mB["counterpartName"] == "Aïcha M." and mB["userSide"] == "Found", "correspondance vue du trouveur")
s, r = call("GET", f"/api/v1/matches/{matchId}", token=tokA)
phoneC, rc = register("Intrus Test", "0005"); tokC = rc["tokens"]["accessToken"]
s, _ = call("GET", f"/api/v1/matches/{matchId}", token=tokC); check(s == 404, "tiers : correspondance introuvable")
s, r = call("GET", "/api/v1/matches", token=tokA); check(s == 200 and len(r) == 1, "liste des correspondances")

# ---------- Notifications ----------
s, r = call("GET", "/api/v1/notifications", token=tokA); check(s == 200 and any(n["kind"] == "Match" for n in r) and all(not n["isRead"] for n in r), "notification de correspondance (A)")
nid = r[0]["id"]
s, r = call("GET", "/api/v1/notifications/unread-count", token=tokA); unread = r["count"]; check(unread >= 1, "compteur de non lues")
s, _ = call("POST", f"/api/v1/notifications/{nid}/read", None, tokA); check(s == 204, "marquer lue")
s, r = call("GET", "/api/v1/notifications/unread-count", token=tokA); check(r["count"] == unread - 1, "compteur décrémenté")
s, _ = call("POST", f"/api/v1/notifications/{nid}/read", None, tokB); check(s == 404, "notification d'un autre utilisateur → 404")
s, _ = call("POST", "/api/v1/notifications/read-all", None, tokA); check(s == 204, "tout marquer lu")
s, r = call("GET", "/api/v1/notifications/unread-count", token=tokA); check(r["count"] == 0, "tout lu")

# ---------- Messagerie ----------
s, convs = call("GET", "/api/v1/conversations", token=tokA)
check(s == 200 and len(convs) == 1 and convs[0]["userIsOwner"] and convs[0]["counterpartName"] == "Brice T." and convs[0]["unreadCount"] == 0, "conversation créée avec la correspondance", f"{convs}")
cv = convs[0]["id"]
s, msgs = call("GET", f"/api/v1/conversations/{cv}/messages", token=tokA); check(s == 200 and len(msgs) == 1 and msgs[0]["isSystem"], "message système initial")
s, _ = call("GET", f"/api/v1/conversations/{cv}/messages", token=tokC); check(s == 404, "tiers : conversation introuvable")
cm = str(uuid.uuid4())
s, r = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": cm, "text": "Bonjour, merci d'avoir trouvé mon diplôme !"}, tokA); check(s == 201 and r["isMine"], "envoi d'un message")
s, r2 = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": cm, "text": "Bonjour, merci d'avoir trouvé mon diplôme !"}, tokA); check(r2["id"] == r["id"], "message idempotent")
s, r = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": str(uuid.uuid4()), "text": "   "}, tokA); check(s == 400, "message vide refusé")
s, r = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": str(uuid.uuid4()), "text": "x" * 1001}, tokA); check(s == 400, "message trop long refusé")
s, convsB = call("GET", "/api/v1/conversations", token=tokB); check(convsB[0]["unreadCount"] == 1 and convsB[0]["lastMessagePreview"].startswith("Bonjour"), "non lu côté B")
s, nB = call("GET", "/api/v1/notifications", token=tokB); check(any(n["kind"] == "Message" for n in nB), "notification de message (B)")
s, _ = call("POST", f"/api/v1/conversations/{cv}/read", None, tokB); check(s == 204, "marquer la conversation lue")
s, convsB = call("GET", "/api/v1/conversations", token=tokB); check(convsB[0]["unreadCount"] == 0, "non lu remis à zéro")
s, r = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": str(uuid.uuid4()), "text": "Avec plaisir, quand puis-je vous le remettre ?"}, tokB)
s, msgs = call("GET", f"/api/v1/conversations/{cv}/messages?limit=10", token=tokA); check(len(msgs) == 3 and msgs[-1]["isMine"] is False, "messages dans l'ordre, côté A")
last = msgs[1]["createdAt"]
s, newer = call("GET", f"/api/v1/conversations/{cv}/messages?after={urllib.request.quote(last)}", token=tokA); check(len(newer) == 1, "récupération incrémentale (after)", f"{len(newer)}")

# ---------- Restitution ----------
s, places = call("GET", "/api/v1/restitution/places", token=tokA); check(s == 200 and len(places) == 3, "lieux publics recommandés")
def slot(days, hour, minute=0):
    t = datetime.datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0) + datetime.timedelta(days=days, hours=hour - 1, minutes=minute)  # heure locale UTC+1
    return t.strftime("%Y-%m-%dT%H:%M:%SZ")
base = f"/api/v1/matches/{matchId}/restitution"
s, r = call("POST", base + "/propose", {"placeName": "Chez moi", "slot": slot(1, 10)}, tokB); check(s == 400 and "placeName" in r["errors"], "lieu non public refusé")
s, r = call("POST", base + "/propose", {"placeName": places[0]["name"], "slot": slot(-1, 10)}, tokB); check(s == 400 and "slot" in r["errors"], "créneau passé refusé")
s, r = call("POST", base + "/propose", {"placeName": places[0]["name"], "slot": slot(1, 23)}, tokB); check(s == 400 and "slot" in r["errors"], "créneau de nuit refusé")
s, r = call("POST", base + "/propose", {"placeName": places[0]["name"], "slot": slot(30, 10)}, tokB); check(s == 400 and "slot" in r["errors"], "créneau trop lointain refusé")
s, r = call("POST", base + "/confirm", None, tokB); check(s == 409 and r["code"] == "not_accepted", "confirmer sans rendez-vous accepté refusé")
s, r = call("POST", base + "/propose", {"placeName": places[0]["name"], "slot": slot(1, 10)}, tokB)
check(s == 200 and r["proposal"]["proposedByMe"] and not r["proposal"]["accepted"], "proposition valide", f"{s} {r}")
s, r = call("POST", base + "/accept", None, tokB); check(s == 409 and r["code"] == "own_proposal", "on n'accepte pas sa propre proposition")
s, r = call("GET", base, token=tokA); check(not r["proposal"]["proposedByMe"] and not r["proposal"]["accepted"], "A voit la proposition de B")
s, r = call("POST", base + "/accept", None, tokA); check(s == 200 and r["proposal"]["accepted"], "acceptation par A")
s, r = call("GET", f"/api/v1/reports/{lostA['id']}", token=tokA); check(r["status"] == "MatchConfirmed", "statut « correspondance confirmée »")
s, r = call("POST", base + "/propose", {"placeName": places[1]["name"], "slot": slot(2, 14)}, tokA); check(s == 200 and not r["proposal"]["accepted"], "contre-proposition annule l'accord")
s, r = call("GET", f"/api/v1/reports/{foundB['id']}", token=tokB); check(r["status"] == "MatchFound", "statuts revenus à « correspondance trouvée »")
s, r = call("POST", base + "/accept", None, tokB); check(s == 200 and r["proposal"]["accepted"], "B accepte la contre-proposition")
s, r = call("POST", base + "/confirm", None, tokA); check(s == 200 and r["myConfirmed"] and not r["completed"] and r["receipt"] is None, "1re confirmation : en attente")
s, r = call("GET", f"/api/v1/reports/{lostA['id']}", token=tokA); check(r["status"] == "MatchConfirmed", "pas encore restitué")
s, r = call("POST", base + "/confirm", None, tokB); check(s == 200 and r["completed"] and r["receipt"]["verificationCode"], "2e confirmation : restitution enregistrée", f"{r}")
receipt = r["receipt"]
s, r = call("GET", f"/api/v1/reports/{lostA['id']}", token=tokA); check(r["status"] == "Returned", "perte : restitué")
s, r = call("GET", f"/api/v1/reports/{foundB['id']}", token=tokB); check(r["status"] == "Returned", "trouvaille : restitué")
s, r = call("POST", f"/api/v1/conversations/{cv}/messages", {"clientMessageId": str(uuid.uuid4()), "text": "Merci"}, tokA); check(s == 409 and r["code"] == "conversation_closed", "conversation clôturée après restitution")
s, r = call("POST", base + "/confirm", None, tokB); check(s == 200 and r["completed"], "confirmation répétée : idempotente")
s, r = call("POST", f"/api/v1/reports/{lostA['id']}/cancel", None, tokA); check(s == 409, "annuler un signalement restitué refusé")

# ---------- Reçu ----------
s, r = call("GET", f"/api/v1/matches/{matchId}/receipt", token=tokA)
check(s == 200 and r["ownerName"] == "Aïcha M." and r["finderName"] == "Brice T." and len(r["qrRows"]) >= 21 and r["verificationUrl"].endswith(r["verificationCode"]), "reçu (rôles, QR)", f"{r}")
check(r["reportId"] == lostA["id"], "reçu : identifiant du signalement du demandeur")
s, pdf = call("GET", f"/api/v1/matches/{matchId}/receipt/pdf", token=tokB, raw=True); check(s == 200 and pdf[:5] == b"%PDF-" and b"%%EOF" in pdf, "PDF du reçu")
open("/tmp/recu.pdf", "wb").write(pdf)
s, _ = call("GET", f"/api/v1/matches/{matchId}/receipt", token=tokC); check(s == 404, "reçu : tiers refusé")
s, v = call("GET", f"/api/v1/receipts/verify/{receipt['verificationCode']}"); check(s == 200 and v["valid"] and v["reference"] == lostA["reference"], "vérification publique du reçu")
check("D112048213" not in json.dumps(v) and "Mbarga" not in json.dumps(v), "vérification publique : pas de donnée sensible")
s, v = call("GET", "/api/v1/receipts/verify/AAAA-BBBB"); check(s == 404 and v["valid"] is False, "code inconnu → 404")
s, html = call("GET", f"/verify/{receipt['verificationCode']}", raw=True); check(s == 200 and b"Re\xc3\xa7u authentique" in html, "page de vérification HTML")
s, html = call("GET", "/verify/ZZZZ-9999", raw=True); check(s == 404 and "introuvable".encode() in html, "page de vérification : reçu inconnu")
s, _ = call("POST", base + "/propose", {"placeName": places[0]["name"], "slot": slot(1, 10)}, tokA); check(s == 409, "restitution terminée : plus de proposition")

# ---------- Contestation ----------
phoneD, rd = register("Denise Owona", "0006"); tokD = rd["tokens"]["accessToken"]
phoneE, re_ = register("Eric Fotso", "0007"); tokE = re_["tokens"]["accessToken"]
s, lostD = new_report(tokD, "Lost", "Passport", name="Denise Owona", number="P1234567", event=d(-3))
s, foundE = new_report(tokE, "Found", "Passport", name="Owona Denise", number="P1234567", event=d(-2))
check(foundE["status"] == "MatchFound", "passeport : correspondance")
s, sr = call("POST", f"/api/v1/reports/{lostD['id']}/search", None, tokD); mid = sr["match"]["id"]
check(sr["match"]["score"] >= 55, "score de la correspondance sans naissance", str(sr["match"]["score"]))
s, r = call("POST", f"/api/v1/matches/{mid}/dispute", {"reason": ""}, tokD); check(s == 400, "contestation sans motif refusée")
s, r = call("POST", f"/api/v1/matches/{mid}/dispute", {"reason": "Ce n’est pas mon document"}, tokD); check(s == 200 and r["isDisputed"], "contestation", f"{r}")
s, r = call("GET", f"/api/v1/reports/{lostD['id']}", token=tokD); check(r["status"] == "MatchDisputed" and r["matchId"] == mid, "contestant : statut « contesté »")
s, r = call("GET", f"/api/v1/reports/{foundE['id']}", token=tokE); check(r["status"] == "Searching" and r["matchId"] is None, "autre partie : retour en recherche")
s, r = call("POST", f"/api/v1/matches/{mid}/dispute", {"reason": "x"}, tokD); check(s == 409, "contestation déjà faite")
s, r = call("POST", f"/api/v1/matches/{mid}/restitution/propose", {"placeName": places[0]["name"], "slot": slot(1, 10)}, tokE); check(s == 409, "restitution impossible après contestation")
s, nE = call("GET", "/api/v1/notifications", token=tokE); check(any("contestée" in n["title"] for n in nE), "l'autre partie est notifiée")
s, r = call("POST", f"/api/v1/reports/{lostD['id']}/search", None, tokD); check(s == 200 and r["found"] is False, "relance : la paire contestée n'est pas reproposée")
s, r = call("GET", f"/api/v1/reports/{lostD['id']}", token=tokD); check(r["status"] == "Searching", "contestant : retour en recherche à la relance")
phoneF, rf = register("Francis Ndi", "0008"); tokF = rf["tokens"]["accessToken"]
s, lostF = new_report(tokF, "Lost", "Passport", name="Denise Owona", number="P1234567", event=d(-3))
check(lostF["status"] == "MatchFound", "nouvelle perte → rapprochée de la trouvaille remise en recherche", f"{lostF['status']}")
s, r = call("POST", f"/api/v1/reports/{lostD['id']}/resolve-recovered", None, tokD); check(s == 409, "clôture « déjà récupéré » refusée hors contestation")

# ---------- Annulation d'une fiche appariée ----------
s, r = call("POST", f"/api/v1/reports/{lostF['id']}/cancel", None, tokF); check(s == 200 and r["status"] == "Cancelled", "annulation")
s, r = call("GET", f"/api/v1/reports/{foundE['id']}", token=tokE); check(r["status"] == "Searching", "l'autre fiche est libérée")
s, r = call("POST", f"/api/v1/reports/{lostF['id']}/cancel", None, tokF); check(s == 200 and r["status"] == "Cancelled", "annulation répétée : idempotente")
# D (en recherche) est rapproché dès que E repasse en recherche ? (E est déjà rapprochée avec D ? paire contestée exclue) → reste en recherche
# ---------- « J'ai déjà récupéré » ----------
phoneG, rg = register("Gaby Mba", "0009"); tokG = rg["tokens"]["accessToken"]
phoneH, rh = register("Hervé Kamga", "0010"); tokH = rh["tokens"]["accessToken"]
s, lostG = new_report(tokG, "Lost", "NationalId", name="Gaby Mba", number="C99887766", event=d(-4))
s, foundH = new_report(tokH, "Found", "NationalId", name="Mba Gaby", number="C99887766", event=d(-3))
s, sr = call("POST", f"/api/v1/reports/{lostG['id']}/search", None, tokG)
s, r = call("POST", f"/api/v1/matches/{sr['match']['id']}/dispute", {"reason": "J’ai déjà récupéré mon document"}, tokG)
s, r = call("POST", f"/api/v1/reports/{lostG['id']}/resolve-recovered", None, tokG); check(s == 200 and r["status"] == "Returned", "clôture « j'ai déjà récupéré mon document »")

# ---------- Photos ----------
png = b"\x89PNG\r\n\x1a\n" + b"0" * 200
def multipart(name, content, fname="p.png"):
    b = "----bnd" + uniq
    data = (f"--{b}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{fname}\"\r\nContent-Type: application/octet-stream\r\n\r\n").encode() + content + f"\r\n--{b}--\r\n".encode()
    return data, {"Content-Type": f"multipart/form-data; boundary={b}"}
data, h = multipart("file", png)
s, r = call("POST", f"/api/v1/reports/{weak['id']}/photo", token=tokA, data=data, headers=h); check(s == 204, "photo PNG acceptée", f"{s} {r}")
s, img = call("GET", f"/api/v1/reports/{weak['id']}/photo", token=tokA, raw=True); check(s == 200 and img == png, "photo restituée au propriétaire")
s, _ = call("GET", f"/api/v1/reports/{weak['id']}/photo", token=tokB, raw=True); check(s == 404, "photo : autre utilisateur refusé")
data, h = multipart("file", b"ceci n'est pas une image", "x.png")
s, r = call("POST", f"/api/v1/reports/{weak['id']}/photo", token=tokA, data=data, headers=h); check(s == 400 and r["code"] == "photo_format", "faux fichier image refusé (signature)")
data, h = multipart("file", png + b"0" * (11 * 1024 * 1024))
s, r = call("POST", f"/api/v1/reports/{weak['id']}/photo", token=tokA, data=data, headers=h); check(s in (400, 413), "photo trop lourde refusée", str(s))
s, r = call("GET", f"/api/v1/reports/{weak['id']}", token=tokA); check(r["hasPhoto"] is True, "hasPhoto")

# ---------- Déconnexion et suppression du compte ----------
s, _ = call("POST", "/api/v1/auth/logout", None, tokA); check(s == 204, "déconnexion")
s, _ = call("GET", "/api/v1/me", token=tokA); check(s == 401, "jeton révoqué immédiatement")
s, r = call("POST", "/api/v1/auth/login", {"phone": phoneA, "password": "Motdepasse1"}); tokA = r["tokens"]["accessToken"]
s, r = call("DELETE", "/api/v1/me", {"password": "faux"}, tokA); check(s == 400, "suppression : mot de passe faux")
s, r = call("DELETE", "/api/v1/me", {"password": "Motdepasse1"}, tokA); check(s == 204, "suppression du compte")
s, _ = call("GET", "/api/v1/me", token=tokA); check(s == 401, "compte supprimé : accès refusé")
s, r = call("POST", "/api/v1/auth/login", {"phone": phoneA, "password": "Motdepasse1"}); check(s == 401, "compte supprimé : connexion refusée")
s, r = call("POST", "/api/v1/auth/register", {"fullName": "Nouvelle Aïcha", "phone": phoneA, "password": "Motdepasse1"}); check(s == 201, "le numéro peut être réutilisé après suppression")

# ---------- En-têtes de sécurité ----------
req = urllib.request.Request(BASE + "/health")
with urllib.request.urlopen(req) as resp:
    check(resp.headers.get("X-Content-Type-Options") == "nosniff" and resp.headers.get("X-Frame-Options") == "DENY", "en-têtes de sécurité")

print(f"Résultat : {ok} réussis, {ko} échecs")
sys.exit(0 if ko == 0 else 1)
