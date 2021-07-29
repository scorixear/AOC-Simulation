using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ant_Colony_Simulation
{
    public class Board
    {
        public List<City> Cities { get; private set; } = new List<City>();
        public List<Street> Streets { get; private set; } = new List<Street>();

        public List<Ant> Ants { get; set; } = new List<Ant>();
        private bool interrupt = false;

        public delegate void SolutionEventHandler(List<Street> solution);
        public event SolutionEventHandler SolutionEvent;

        private void OnSolutionEvent(List<Street> solution)
        {
            SolutionEvent?.Invoke(solution);
        }

        public delegate void ProgressEventHandler(int progress);
        public event ProgressEventHandler ProgressMade;
        private void OnProgressMade(int progress)
        {
            ProgressMade?.Invoke(progress);
        }

        public void AddCity(City city)
        {
            if(Cities.Contains(city))
            {
                throw new ArgumentException("City already exists");
            }
            foreach(City c in Cities)
            {
                Streets.Add(new Street(city, c));
            }
            Cities.Add(city);
        }

        public void Init(int numberOfAnts)
        {
            Ants.Clear();
            Streets.Clear();
            Cities.Clear();
            for (int i = 0; i < numberOfAnts; i++)
            {
                Ants.Add(new Ant());
            }
        }

        public async Task<bool> EvaluationStep()
        {
            List<Task> tasks = new List<Task>();
            foreach(Ant a in Ants)
            {
                if (interrupt)
                {
                    break;
                }
                if(!a.Finished)
                {
                    tasks.Add(Task.Run(()=>a.ChooseNextCity(Streets)));
                }
                else
                {
                    return true;
                }
            }
            await Task.WhenAll(tasks);
            return false;
        }

        public async Task<List<Street>> Evaluate(int numberOfSteps, double evaporation)
        {
            List<Street> bestSolution = new();
            double bestDistance = double.MaxValue;
            int interruptCount = 0;
            Random r = new Random();
            Cities.Sort((a, b) => r.NextDouble() > 0.5 ? -1 : 1);
            Streets.Sort((a, b) => r.NextDouble() > 0.5 ? -1 : 1);
            for (int i = 0; i < numberOfSteps; i++)
            {
                Ants.ForEach(a => a.Init(Cities));
                while (!await EvaluationStep()) 
                {
                    if (interrupt)
                    {
                        break;
                    }
                }
                if (interrupt)
                {
                    interrupt = false;
                    break;
                }
                double minimum = double.MaxValue;
                Ant bestAnt = null;
                Dictionary<Ant, double> solutions = new();
                foreach (Ant ant in Ants)
                {
                    double solution = ant.GetSolutionDistance();
                    solutions.Add(ant, solution);
                    if(solution < minimum)
                    {
                        bestAnt = ant;
                        minimum = solution;
                    }
                }

                if(solutions[bestAnt] < bestDistance)
                {
                    bestSolution = new List<Street>(bestAnt.Solution);
                    bestDistance = solutions[bestAnt];
                } 
                else if(bestDistance == solutions[bestAnt])
                {
                    interruptCount++;
                }
                if(interruptCount > 10)
                {
                    break;
                }

                    OnSolutionEvent(bestSolution);

                foreach (Street street in Streets)
                {
                    street.pheromon = (1 - evaporation) * street.pheromon;
                }
                bestAnt.Solution.ForEach(s => {
                    foreach(Ant a in Ants)
                    {
                        if(a.Solution.Contains(s))
                        {
                            s.pheromon += 1 / solutions[a];
                        }
                    }
                });
                OnProgressMade(i);
            }
            OnProgressMade(numberOfSteps);
            return bestSolution;
        }

        internal void Interrupt()
        {
            interrupt = true;
        }
    }

    public class Ant
    {
        public List<Street> Solution { get; set; } = new List<Street>();
        public List<City> SolutionCities { get; set; } = new List<City>();
        public City CurrentLocation { get; set; }

        public bool Finished { get; private set; }

        public void Init(List<City> cities)
        {
            Random random = new Random();
            if(cities.Count != 0)
            {
                CurrentLocation = cities[random.Next(0, cities.Count - 1)];
            }
            Solution = new List<Street>();
            SolutionCities = new List<City>();
            Finished = false;
        }

        public double GetSolutionDistance()
        {
            return Solution.Sum(s => s.Distance);
        }

        public bool ChooseNextCity(List<Street> streets)
        {
            List<Street> availableStreets = new List<Street>();
            foreach(Street street in  streets)
            {
                if(street.a == CurrentLocation)
                {
                    if(!SolutionCities.Contains(street.b))
                    {
                        availableStreets.Add(street);
                    }
                }
                else if(street.b == CurrentLocation)
                {
                    if (!SolutionCities.Contains(street.a))
                    {
                        availableStreets.Add(street);
                    }
                }
            }

            Street selectedStreet = SelectStreet(availableStreets);
            if(selectedStreet == null)
            {
                Finished = true;
                return true;
            }
            SolutionCities.Add(CurrentLocation);
            if(selectedStreet.a == CurrentLocation)
            {
                CurrentLocation = selectedStreet.b;
            }
            else
            {
                CurrentLocation = selectedStreet.a;
            }
            Solution.Add(selectedStreet);
            return false;
        }
        private static Random streetRandom = new Random();
        class Items
        {
            public double Probability { get; set; }
            public Street Item { get; set; }
        }
        private Street SelectStreet(List<Street> streets)
        {
            if(streets.Count == 0)
            {
                return null;
            }
            double totalChance = 0;
            List<Items> converted = new List<Items>();
            foreach(Street street in streets)
            {
                double prob = street.pheromon * (1 / street.Distance) / streets.Sum(s => s.pheromon * (1 / s.Distance));
                totalChance += prob;
                converted.Add(new Items { Item = street, Probability = totalChance });
            }
            double selected = streetRandom.NextDouble();
            return converted.SkipWhile(i => i.Probability < selected).First().Item;
        }
    }

    public class Street
    {
        public City a;
        public City b;
        public double Distance;
        public double pheromon = 0.5;
        public Street(City a, City b)
        {
            this.a = a;
            this.b = b;
            Distance = Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.y - a.y, 2));
        }
    }

    public class City
    {
        public double x;
        public double y;
    }
}
