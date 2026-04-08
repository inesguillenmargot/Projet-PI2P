using System;
using System.Collections.Generic;
using System.Linq;

namespace LeoMaps
{
    // ─────────────────────────────────────────────
    // Modèles de base
    // ─────────────────────────────────────────────

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
            => $"(étage {Etage}, x={X}, y={Y})";
    }

    public class Salle
    {
        public string Numero { get; set; }
        public int NumeroLocal { get; set; }
        public Position Position { get; set; }

        public Salle(string numero, int x, int y)
        {
            if (!int.TryParse(numero, out int num) || num < 100)
                throw new ArgumentException($"Numéro de salle invalide : '{numero}'.");

            Numero = numero;
            int etage = num / 100;
            NumeroLocal = num % 100;
            Position = new Position(etage, x, y);
        }

        public override string ToString()
            => $"Salle {Numero} (étage {Position.Etage})";
    }

    public class Ascenseur
    {
        public string Id { get; set; }
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

        public bool DessertEtage(int etage) => EtagesDesservis.Contains(etage);

        public override string ToString()
            => $"Ascenseur {Id}";
    }

    // ─────────────────────────────────────────────
    // Résultat structuré pour le web
    // ─────────────────────────────────────────────

    public class EtapeNavigation
    {
        public string Type { get; set; } = ""; 
        // Exemples : "horizontal", "vertical", "ascenseur", "arrivee"

        public string Instruction { get; set; } = "";
        public Position Debut { get; set; } = null!;
        public Position Fin { get; set; } = null!;
        public int Distance { get; set; }
    }

    public class Itineraire
    {
        public string SalleDepart { get; set; } = "";
        public string SalleArrivee { get; set; } = "";
        public int DistanceTotale { get; set; }
        public List<EtapeNavigation> Etapes { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // Plan du bâtiment
    // ─────────────────────────────────────────────

    public class Batiment
    {
        private readonly Dictionary<string, Salle> _salles = new();
        private readonly List<Ascenseur> _ascenseurs = new();

        public void AjouterSalle(Salle salle) => _salles[salle.Numero] = salle;
        public void AjouterAscenseur(Ascenseur ascenseur) => _ascenseurs.Add(ascenseur);

        public Salle TrouverSalle(string numero)
        {
            if (_salles.TryGetValue(numero, out var salle))
                return salle;

            throw new ArgumentException($"Salle '{numero}' introuvable.");
        }

        public Ascenseur TrouverAscenseurLePlusProche(Position depuis, int etageDestination)
        {
            return _ascenseurs
                .Where(a => a.DessertEtage(depuis.Etage) && a.DessertEtage(etageDestination))
                .OrderBy(a => depuis.DistanceManhattan(a.PositionEtage(depuis.Etage)))
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Aucun ascenseur ne relie l'étage {depuis.Etage} à l'étage {etageDestination}.");
        }
    }

    // ─────────────────────────────────────────────
    // Moteur de navigation
    // ─────────────────────────────────────────────

    public class Navigateur
    {
        private readonly Batiment _batiment;

        public Navigateur(Batiment batiment)
        {
            _batiment = batiment;
        }

        public Itineraire CalculerItineraire(string numeroDepart, string numeroArrivee)
        {
            Salle depart = _batiment.TrouverSalle(numeroDepart);
            Salle arrivee = _batiment.TrouverSalle(numeroArrivee);

            var itineraire = new Itineraire
            {
                SalleDepart = depart.Numero,
                SalleArrivee = arrivee.Numero
            };

            if (depart.Position.Etage == arrivee.Position.Etage)
            {
                AjouterEtapesMemeEtage(
                    itineraire,
                    depart.Position,
                    arrivee.Position,
                    $"Aller à la salle {arrivee.Numero}"
                );
            }
            else
            {
                Ascenseur ascenseur = _batiment.TrouverAscenseurLePlusProche(
                    depart.Position,
                    arrivee.Position.Etage
                );

                Position posAscenseurDepart = ascenseur.PositionEtage(depart.Position.Etage);
                Position posAscenseurArrivee = ascenseur.PositionEtage(arrivee.Position.Etage);

                AjouterEtapesMemeEtage(
                    itineraire,
                    depart.Position,
                    posAscenseurDepart,
                    $"Aller jusqu'à l'ascenseur {ascenseur.Id}"
                );

                itineraire.Etapes.Add(new EtapeNavigation
                {
                    Type = "ascenseur",
                    Instruction = $"Prendre l'ascenseur {ascenseur.Id} de l'étage {depart.Position.Etage} à l'étage {arrivee.Position.Etage}",
                    Debut = posAscenseurDepart,
                    Fin = posAscenseurArrivee,
                    Distance = 0
                });

                AjouterEtapesMemeEtage(
                    itineraire,
                    posAscenseurArrivee,
                    arrivee.Position,
                    $"Aller à la salle {arrivee.Numero}"
                );
            }

            itineraire.Etapes.Add(new EtapeNavigation
            {
                Type = "arrivee",
                Instruction = $"Vous êtes arrivé à la salle {arrivee.Numero}",
                Debut = arrivee.Position,
                Fin = arrivee.Position,
                Distance = 0
            });

            itineraire.DistanceTotale = itineraire.Etapes.Sum(e => e.Distance);

            return itineraire;
        }

        private static void AjouterEtapesMemeEtage(
            Itineraire itineraire,
            Position depuis,
            Position vers,
            string instructionFinale)
        {
            int dx = vers.X - depuis.X;
            int dy = vers.Y - depuis.Y;

            Position positionCourante = new Position(depuis.Etage, depuis.X, depuis.Y);

            if (dx != 0)
            {
                var nouvellePosition = new Position(
                    positionCourante.Etage,
                    vers.X,
                    positionCourante.Y
                );

                itineraire.Etapes.Add(new EtapeNavigation
                {
                    Type = "horizontal",
                    Instruction = dx > 0
                        ? $"Aller vers la droite sur {Math.Abs(dx)} porte(s)"
                        : $"Aller vers la gauche sur {Math.Abs(dx)} porte(s)",
                    Debut = new Position(positionCourante.Etage, positionCourante.X, positionCourante.Y),
                    Fin = nouvellePosition,
                    Distance = Math.Abs(dx)
                });

                positionCourante = nouvellePosition;
            }

            if (dy != 0)
            {
                var nouvellePosition = new Position(
                    positionCourante.Etage,
                    positionCourante.X,
                    vers.Y
                );

                itineraire.Etapes.Add(new EtapeNavigation
                {
                    Type = "vertical",
                    Instruction = dy > 0
                        ? $"Aller vers le bas sur {Math.Abs(dy)} porte(s)"
                        : $"Aller vers le haut sur {Math.Abs(dy)} porte(s)",
                    Debut = new Position(positionCourante.Etage, positionCourante.X, positionCourante.Y),
                    Fin = nouvellePosition,
                    Distance = Math.Abs(dy)
                });

                positionCourante = nouvellePosition;
            }

            itineraire.Etapes.Add(new EtapeNavigation
            {
                Type = "instruction",
                Instruction = instructionFinale,
                Debut = new Position(positionCourante.Etage, positionCourante.X, positionCourante.Y),
                Fin = new Position(positionCourante.Etage, positionCourante.X, positionCourante.Y),
                Distance = 0
            });
        }
    }

    // ─────────────────────────────────────────────
    // Affichage console temporaire
    // ─────────────────────────────────────────────

    public static class AfficheurConsole
    {
        public static void Afficher(Itineraire itineraire)
        {
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"ITINÉRAIRE : {itineraire.SalleDepart} → {itineraire.SalleArrivee}");
            Console.WriteLine(new string('═', 60));

            for (int i = 0; i < itineraire.Etapes.Count; i++)
            {
                var etape = itineraire.Etapes[i];
                Console.WriteLine($"{i + 1}. [{etape.Type}] {etape.Instruction}");

                if (etape.Debut != null && etape.Fin != null)
                {
                    Console.WriteLine($"   De {etape.Debut} vers {etape.Fin}");
                }

                if (etape.Distance > 0)
                {
                    Console.WriteLine($"   Distance : {etape.Distance}");
                }

                Console.WriteLine();
            }

            Console.WriteLine($"Distance totale : {itineraire.DistanceTotale}");
            Console.WriteLine(new string('═', 60));
            Console.WriteLine();
        }
    }

    // ─────────────────────────────────────────────
    // Programme principal
    // ─────────────────────────────────────────────

    class Program
    {
        static void Main(string[] args)
        {
            var batiment = new Batiment();

            batiment.AjouterAscenseur(new Ascenseur("A1", 3, 0, new List<int> { 1, 2, 3, 4 }));
            batiment.AjouterAscenseur(new Ascenseur("A2", 8, 0, new List<int> { 1, 2, 3 }));

            batiment.AjouterSalle(new Salle("101", 1, 2));
            batiment.AjouterSalle(new Salle("102", 5, 2));
            batiment.AjouterSalle(new Salle("103", 9, 4));
            batiment.AjouterSalle(new Salle("104", 2, 6));

            batiment.AjouterSalle(new Salle("201", 1, 1));
            batiment.AjouterSalle(new Salle("202", 6, 3));
            batiment.AjouterSalle(new Salle("203", 9, 5));

            batiment.AjouterSalle(new Salle("301", 2, 1));
            batiment.AjouterSalle(new Salle("302", 7, 4));

            batiment.AjouterSalle(new Salle("401", 4, 2));
            batiment.AjouterSalle(new Salle("402", 1, 5));

            var navigateur = new Navigateur(batiment);

            try
            {
                var itineraire1 = navigateur.CalculerItineraire("101", "103");
                AfficheurConsole.Afficher(itineraire1);

                var itineraire2 = navigateur.CalculerItineraire("101", "202");
                AfficheurConsole.Afficher(itineraire2);

                Console.Write("Salle de départ : ");
                string dep = Console.ReadLine()?.Trim() ?? "";

                Console.Write("Salle d'arrivée : ");
                string arr = Console.ReadLine()?.Trim() ?? "";

                var itineraireUtilisateur = navigateur.CalculerItineraire(dep, arr);
                AfficheurConsole.Afficher(itineraireUtilisateur);
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