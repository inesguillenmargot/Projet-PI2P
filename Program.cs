using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NavigationBatiment
{
    public class Position
    {
        public int Etage { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Position(int etage, int x, int y)
        {
            Etage = etage;
            X = x;
            Y = y;
        }

        public int DistanceManhattan(Position autre)
            => Math.Abs(X - autre.X) + Math.Abs(Y - autre.Y);

        public override string ToString()
            => $"(étage {Etage}, colonne {X}, ligne {Y})";
    }

    public class Emplacement
    {
        public string Nom { get; set; } = "";
        public string Type { get; set; } = "";
        public string Batiment { get; set; } = "";
        public int Etage { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Position Position => new Position(Etage, X, Y);

        public override string ToString()
            => $"{Nom} ({Type}, bâtiment {Batiment}, étage {Etage})";
    }

    public class Batiment
    {
        private readonly List<Emplacement> _emplacements = new();

        public Batiment(List<Emplacement> emplacements)
        {
            _emplacements = emplacements;
        }

        public Emplacement TrouverEmplacement(string nom, int? etage = null)
        {
            var recherche = _emplacements
                .Where(e => e.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

            if (etage.HasValue)
                recherche = recherche.Where(e => e.Etage == etage.Value);

            return recherche.FirstOrDefault()
                ?? throw new ArgumentException($"Emplacement '{nom}' introuvable.");
        }

        public List<Emplacement> ListerParEtage(string batiment, int etage)
        {
            return _emplacements
                .Where(e => e.Batiment.Equals(batiment, StringComparison.OrdinalIgnoreCase)
                         && e.Etage == etage)
                .OrderBy(e => e.Type)
                .ThenBy(e => e.Nom)
                .ToList();
        }

        public Emplacement TrouverAscenseurLePlusProche(Position depuis, int etageDestination)
        {
            return _emplacements
                .Where(e => e.Type.Equals("Ascenseur", StringComparison.OrdinalIgnoreCase)
                         && e.Etage == depuis.Etage)
                .OrderBy(e => depuis.DistanceManhattan(e.Position))
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Aucun ascenseur trouvé à l'étage {depuis.Etage}.");
        }

        public Emplacement TrouverAscenseurEquivalent(Emplacement ascenseurDepart, int etageDestination)
        {
            return _emplacements
                .Where(e => e.Type.Equals("Ascenseur", StringComparison.OrdinalIgnoreCase)
                         && e.Etage == etageDestination
                         && e.Nom.Equals(ascenseurDepart.Nom, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"L'ascenseur {ascenseurDepart.Nom} n'existe pas à l'étage {etageDestination}.");
        }
    }

    public class Navigateur
    {
        private readonly Batiment _batiment;

        public Navigateur(Batiment batiment)
        {
            _batiment = batiment;
        }

        public void AfficherChemin(string nomDepart, int? etageDepart, string nomArrivee, int? etageArrivee)
        {
            Emplacement depart = _batiment.TrouverEmplacement(nomDepart, etageDepart);
            Emplacement arrivee = _batiment.TrouverEmplacement(nomArrivee, etageArrivee);

            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  NAVIGATION : {depart.Nom} → {arrivee.Nom}");
            Console.WriteLine(new string('═', 60));

            Console.WriteLine($"  Départ  : {depart}");
            Console.WriteLine($"  Arrivée : {arrivee}");
            Console.WriteLine(new string('─', 60));

            if (depart.Position.Etage == arrivee.Position.Etage)
            {
                CheminMemeEtage(depart.Position, arrivee.Position, arrivee.Nom);
            }
            else
            {
                Emplacement ascenseurDepart = _batiment.TrouverAscenseurLePlusProche(
                    depart.Position,
                    arrivee.Position.Etage
                );

                Emplacement ascenseurArrivee = _batiment.TrouverAscenseurEquivalent(
                    ascenseurDepart,
                    arrivee.Position.Etage
                );

                int distAscenseur = depart.Position.DistanceManhattan(ascenseurDepart.Position);
                int distFinale = ascenseurArrivee.Position.DistanceManhattan(arrivee.Position);

                Console.WriteLine($"  Ascenseur choisi : {ascenseurDepart.Nom}");
                Console.WriteLine();

                Console.WriteLine($"  ① Depuis {depart.Nom} jusqu'à {ascenseurDepart.Nom}");
                CheminMemeEtage(depart.Position, ascenseurDepart.Position, ascenseurDepart.Nom, "     ");
                Console.WriteLine($"     Distance : {distAscenseur} porte(s)");

                Console.WriteLine();
                string sens = arrivee.Etage > depart.Etage ? "Monter" : "Descendre";
                Console.WriteLine($"  ② {sens} avec {ascenseurDepart.Nom}");
                Console.WriteLine($"     Étage {depart.Etage} → Étage {arrivee.Etage}");

                Console.WriteLine();
                Console.WriteLine($"  ③ Depuis {ascenseurArrivee.Nom} jusqu'à {arrivee.Nom}");
                CheminMemeEtage(ascenseurArrivee.Position, arrivee.Position, arrivee.Nom, "     ");
                Console.WriteLine($"     Distance : {distFinale} porte(s)");

                Console.WriteLine();
                Console.WriteLine($"  Distance totale estimée : {distAscenseur + distFinale} porte(s)");
            }

            Console.WriteLine(new string('═', 60));
            Console.WriteLine();
        }

        private static void CheminMemeEtage(Position depuis, Position vers, string nomDestination, string indent = "  ")
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
                string direction = dy > 0 ? "bas" : "haut";
                Console.WriteLine($"{indent}  • Allez vers le {direction} ({Math.Abs(dy)} porte(s))");
            }

            Console.WriteLine($"{indent}  → Vous arrivez à {nomDestination}.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string cheminJson = Path.Combine(AppContext.BaseDirectory, "data", "emplacements.json");

            if (!File.Exists(cheminJson))
            {
                Console.WriteLine($"Fichier JSON introuvable : {cheminJson}");
                return;
            }

            string json = File.ReadAllText(cheminJson);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<Emplacement> emplacements =
                JsonSerializer.Deserialize<List<Emplacement>>(json, options)
                ?? new List<Emplacement>();

            var batiment = new Batiment(emplacements);
            var nav = new Navigateur(batiment);

            Console.WriteLine("Bienvenue sur LeoMaps");
            Console.WriteLine();

            Console.Write("Étage de départ : ");
            int etageDepart = int.Parse(Console.ReadLine()?.Trim() ?? "1");

            Console.Write("Emplacement de départ : ");
            string depart = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Étage d'arrivée : ");
            int etageArrivee = int.Parse(Console.ReadLine()?.Trim() ?? "1");

            Console.Write("Emplacement d'arrivée : ");
            string arrivee = Console.ReadLine()?.Trim() ?? "";

            try
            {
                nav.AfficherChemin(depart, etageDepart, arrivee, etageArrivee);
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