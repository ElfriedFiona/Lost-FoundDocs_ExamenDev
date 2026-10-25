# Accessibilité : ce qui est en place et ce qu'il reste à vérifier sur appareil

## En place dans le code
- **Noms accessibles** (`SemanticProperties.Description` / `Hint`) sur tous les boutons icône (retour, fermer, œil, cloche, « + »), les onglets (état actif, nombre de messages non lus), les cartes de signalement (titre, date, statut), les pastilles de statut et le score de correspondance.
- **Titres sémantiques** (`HeadingLevel`) sur les titres d'écran et de section.
- **Erreurs de saisie** : message sous le champ (icône + texte), le champ passe en rouge ; la couleur n'est jamais le seul indicateur (les statuts ont aussi un libellé, les pastilles une coche ou un point + texte).
- **Zones tactiles** d'au moins 44-48 dp (champs et boutons de 48 dp, onglets, icônes cliquables de 44 dp).
- **Texte** : retour à la ligne partout (seuls les aperçus de liste sont tronqués avec « … ») ; tailles relatives aux réglages du système.
- **Contrastes** : texte secondaire `#6B7280` sur blanc ; textes d'état en teintes foncées dérivées (`SuccessText`, `WarningText`, `ErrorText`) car `#F59E0B` seul est trop clair en texte.
- **Navigation** : bouton Retour système géré dans les formulaires (retour d'étape / abandon confirmé).

## À vérifier sur appareil (je n'ai pas pu le tester)
- Lecture avec TalkBack (Android) et Narrateur (Windows) : ordre de lecture, annonces des erreurs.
- Taille de police système à 130-200 % sur un écran de 360 dp.
- Contrastes réels des teintes dérivées face au PDF.
