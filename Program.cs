using System;
using System.Collections.Generic;
using System.Linq;

namespace NavigationBatiment
{
    // ─────────────────────────────────────────────
    //  Modèles de données
    // ─────────────────────────────────────────────

    public class Position
    {
        public int Etage { get; set; }
        public int X { get; set; }   // colonne (position horizontale)
        public int Y { get; set; }   // ligne   (position verticale)

        public Position(int etage, int x, int y)
        {
            Etage = etage;
            X = x;
            Y = y;
        }

        // Distance de Manhattan (même étage)
        public int DistanceManhattan(Position autre)
            => Math.Abs(X - autre.X) + Math.Abs(Y - autre.Y);

        public override string ToString() => $"(étage {Etage}, colonne {X}, ligne {Y})";
    }

    public class Salle
    {
        public string Numero { get; set; }
        public Position Position { get; set; }

        public Salle(string numero, int etage, int x, int y)
        {
            Numero = numero;
            Position = new Position(etage, x, y);
        }

        public override string ToString() => $"Salle {Numero} — Étage {Position.Etage}";
    }

    public class Ascenseur
    {
        public string Id { get; set; }
        // Un ascenseur a la même position X,Y sur tous les étages
        public int X { get; set; }
        public int Y { get; set; }
        public List<int> EtagesDesservis { get; set; }

        public Ascenseur(string id, int x, int y, List<int> etagesDesservis)
        {
            Id = id;
            X = x;
            Y = y;
            EtagesDesservis = etagesDesservis;
        }

        public Position PositionEtage(int etage) => new Position(etage, X, Y);

        public bool DesssertEtage(int etage) => EtagesDesservis.Contains(etage);

        public override string ToString() => $"Ascenseur {Id} — étages desservis : {string.Join(", ", EtagesDesservis)}";
    }

    // ─────────────────────────────────────────────
    //  Plan du bâtiment
    // ─────────────────────────────────────────────

    public class Batiment
    {
        private readonly Dictionary<string, Salle> _salles = new();
        private readonly List<Ascenseur> _ascenseurs = new();

        public void AjouterSalle(Salle salle) => _salles[salle.Numero] = salle;
        public void AjouterAscenseur(Ascenseur ascenseur) => _ascenseurs.Add(ascenseur);

        public Salle TrouverSalle(string numero)
        {
            if (_salles.TryGetValue(numero, out var salle)) return salle;
            throw new ArgumentException($"Salle '{numero}' introuvable.");
        }

        /// <summary>
        /// Trouve l'ascenseur le plus proche d'une position donnée
        /// qui dessert les deux étages nécessaires.
        /// </summary>
        public Ascenseur TrouverAscenseurLePlusProche(Position depuis, int etageDestination)
        {
            return _ascenseurs
                .Where(a => a.DesssertEtage(depuis.Etage) && a.DesssertEtage(etageDestination))
                .OrderBy(a => depuis.DistanceManhattan(a.PositionEtage(depuis.Etage)))
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Aucun ascenseur ne relie l'étage {depuis.Etage} à l'étage {etageDestination}.");
        }
    }

    // ─────────────────────────────────────────────
    //  Moteur de navigation
    // ─────────────────────────────────────────────

    public class Navigateur
    {
        private readonly Batiment _batiment;

        public Navigateur(Batiment batiment) => _batiment = batiment;

        public void AfficherChemin(string numeroDepart, string numeroArrivee)
        {
            Console.WriteLine(new string('═', 55));
            Console.WriteLine($"  NAVIGATION : {numeroDepart}  →  {numeroArrivee}");
            Console.WriteLine(new string('═', 55));

            Salle depart = _batiment.TrouverSalle(numeroDepart);
            Salle arrivee = _batiment.TrouverSalle(numeroArrivee);

            Console.WriteLine($"  Départ  : {depart}");
            Console.WriteLine($"  Arrivée : {arrivee}");
            Console.WriteLine(new string('─', 55));

            if (depart.Position.Etage == arrivee.Position.Etage)
            {
                // ── Même étage : chemin direct ──────────────────
                CheminMemeEtage(depart.Position, arrivee.Position, $"la salle {arrivee.Numero}");
            }
            else
            {
                // ── Étages différents : via ascenseur ───────────
                Ascenseur ascenseur = _batiment.TrouverAscenseurLePlusProche(
                    depart.Position, arrivee.Position.Etage);

                Position posAscenseurDepart = ascenseur.PositionEtage(depart.Position.Etage);
                Position posAscenseurArrivee = ascenseur.PositionEtage(arrivee.Position.Etage);

                int distAscenseur = depart.Position.DistanceManhattan(posAscenseurDepart);
                int distFinale = posAscenseurArrivee.DistanceManhattan(arrivee.Position);

                Console.WriteLine($"  Ascenseur le plus proche : {ascenseur.Id}");
                Console.WriteLine();

                // Segment 1 : salle de départ → ascenseur
                Console.WriteLine($"  ① Depuis la salle {depart.Numero} jusqu'à l'ascenseur {ascenseur.Id}");
                CheminMemeEtage(depart.Position, posAscenseurDepart,
                    $"l'ascenseur {ascenseur.Id}", "     ");
                Console.WriteLine($"     Distance : {distAscenseur} porte(s)");

                // Segment 2 : montée / descente
                Console.WriteLine();
                string sens = arrivee.Position.Etage > depart.Position.Etage ? "Monter" : "Descendre";
                Console.WriteLine($"  ② {sens} avec l'ascenseur {ascenseur.Id}");
                Console.WriteLine($"     Étage {depart.Position.Etage}  →  Étage {arrivee.Position.Etage}");

                // Segment 3 : ascenseur → salle d'arrivée
                Console.WriteLine();
                Console.WriteLine($"  ③ Depuis l'ascenseur {ascenseur.Id} jusqu'à la salle {arrivee.Numero}");
                CheminMemeEtage(posAscenseurArrivee, arrivee.Position,
                    $"la salle {arrivee.Numero}", "     ");
                Console.WriteLine($"     Distance : {distFinale} porte(s)");

                Console.WriteLine();
                Console.WriteLine($"  Distance totale estimée : {distAscenseur + distFinale} porte(s)");
            }

            Console.WriteLine(new string('═', 55));
            Console.WriteLine();
        }

        // ── Génère les instructions directionnelles entre deux points ──
        private static void CheminMemeEtage(
            Position depuis, Position vers, string nomDestination, string indent = "  ")
        {
            int dx = vers.X - depuis.X;
            int dy = vers.Y - depuis.Y;

            if (dx == 0 && dy == 0)
            {
                Console.WriteLine($"{indent}Vous êtes déjà à destination : {nomDestination}.");
                return;
            }

            Console.WriteLine($"{indent}Directions :");

            if (dx != 0)
            {
                string direction = dx > 0 ? "droite" : "gauche";
                Console.WriteLine($"{indent}  • Allez vers la {direction} ({Math.Abs(dx)} porte(s))");
            }

            if (dy != 0)
            {
                string direction = dy > 0 ? "bas (sud)" : "haut (nord)";
                Console.WriteLine($"{indent}  • Allez vers le {direction} ({Math.Abs(dy)} porte(s))");
            }

            Console.WriteLine($"{indent}  → Vous arrivez à {nomDestination}.");
        }
    }

    // ─────────────────────────────────────────────
    //  Programme principal — exemple de bâtiment
    // ─────────────────────────────────────────────

    class Program
    {
        static void Main(string[] args)
        {
            // ── Construction du bâtiment ────────────────────────
            var batiment = new Batiment();

            // Ascenseurs (id, colonneX, ligneY, étages desservis)
            batiment.AjouterAscenseur(new Ascenseur("A1", 3, 0, new List<int> { 0, 1, 2, 3 }));
            batiment.AjouterAscenseur(new Ascenseur("A2", 8, 0, new List<int> { 0, 1, 2 }));

            // Salles — Étage 0 (RDC)
            batiment.AjouterSalle(new Salle("101", 0, 1, 2));
            batiment.AjouterSalle(new Salle("102", 0, 5, 2));
            batiment.AjouterSalle(new Salle("103", 0, 9, 4));
            batiment.AjouterSalle(new Salle("104", 0, 2, 6));

            // Salles — Étage 1
            batiment.AjouterSalle(new Salle("201", 1, 1, 1));
            batiment.AjouterSalle(new Salle("202", 1, 6, 3));
            batiment.AjouterSalle(new Salle("203", 1, 9, 5));

            // Salles — Étage 2
            batiment.AjouterSalle(new Salle("301", 2, 2, 1));
            batiment.AjouterSalle(new Salle("302", 2, 7, 4));

            // Salles — Étage 3
            batiment.AjouterSalle(new Salle("401", 3, 4, 2));
            batiment.AjouterSalle(new Salle("402", 3, 1, 5));

            // ── Navigation ──────────────────────────────────────
            var nav = new Navigateur(batiment);

            // Cas 1 : même étage
            nav.AfficherChemin("101", "103");

            // Cas 2 : étages différents
            nav.AfficherChemin("101", "202");

            // Cas 3 : plusieurs étages d'écart
            nav.AfficherChemin("102", "401");

            // ── Mode interactif ─────────────────────────────────
            Console.WriteLine("── Mode interactif ──────────────────────────────");
            Console.Write("Numéro de salle de départ : ");
            string dep = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Numéro de salle d'arrivée : ");
            string arr = Console.ReadLine()?.Trim() ?? "";

            try
            {
                nav.AfficherChemin(dep, arr);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erreur : {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}