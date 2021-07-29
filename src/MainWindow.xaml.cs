using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ant_Colony_Simulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            board.SolutionEvent += Board_SolutionEvent;
            board.ProgressMade += Board_ProgressMade;
        }

        private void Board_ProgressMade(int progress)
        {
            Dispatcher.Invoke(() =>
            {
                Progress.Value = progress;
            });
        }

        private void Board_SolutionEvent(List<Street> solution)
        {

            City[] leftover = new City[2];
            int index = 0;
            foreach (City city in cities)
            {
                if(solution.FindAll(s => s.a == city || s.b == city).Count == 1)
                {
                    leftover[index++] = city;
                }
            }

            Dispatcher.Invoke(() =>
            {
                for (int i = Canvas.Children.Count - 1; i >= 0; i += -1)
                {
                    UIElement Child = Canvas.Children[i];
                    if (Child is Line)
                    {
                        Canvas.Children.Remove(Child);
                    }
                }

                foreach (Street street in solution)
                {
                    Line l = new()
                    {
                        X1 = street.a.x + 5,
                        X2 = street.b.x + 5,
                        Y1 = street.a.y + 5,
                        Y2 = street.b.y + 5,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2
                    };
                    Canvas.Children.Add(l);
                }
                Line end = new()
                {
                    X1 = leftover[0].x + 5,
                    X2 = leftover[1].x + 5,
                    Y1 = leftover[0].y + 5,
                    Y2 = leftover[1].y + 5,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                Canvas.Children.Add(end);
            });
        }

        Board board = new Board();
        List<City> cities = new();

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = Brushes.Black;
            ellipse.Width = 10;
            ellipse.Height = 10;
            Canvas.SetLeft(ellipse, e.GetPosition(Canvas).X);
            Canvas.SetTop(ellipse, e.GetPosition(Canvas).Y);
            Canvas.Children.Add(ellipse);
            cities.Add(new City() { x = e.GetPosition(Canvas).X, y = e.GetPosition(Canvas).Y });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Content.ToString() == "Start")
            {
                
                ((Button)sender).Content = "Running";
                List<Street> solution = new List<Street>();
                int ants = int.Parse(Ants.Text);
                int steps = int.Parse(Steps.Text);
                Progress.Maximum = steps;
                Progress.Value = 0;
                double pheromone = pheromonSlider.Value;
                await Task.Run(async () =>
                {
                    board.Init(ants);
                    cities.ForEach(c => board.AddCity(c));
                    solution = await board.Evaluate(steps, pheromone);
                });
                //for (int i = Canvas.Children.Count - 1; i >= 0; i += -1)
                //{
                //    UIElement Child = Canvas.Children[i];
                //    if (Child is Line)
                //    {
                //        Canvas.Children.Remove(Child);
                //    }
                //}
                //foreach (Street street in solution)
                //{
                //    Line l = new()
                //    {
                //        X1 = street.a.x + 5,
                //        X2 = street.b.x + 5,
                //        Y1 = street.a.y + 5,
                //        Y2 = street.b.y + 5,
                //        Stroke = Brushes.Blue,
                //        StrokeThickness = 2
                //    };
                //    Canvas.Children.Add(l);
                //}
                //Line end = new()
                //{
                //    X1 = cities[cities.Count - 1].x + 5,
                //    X2 = cities[0].x + 5,
                //    Y1 = cities[cities.Count - 1].y + 5,
                //    Y2 = cities[0].y + 5,
                //    Stroke = Brushes.Blue,
                //    StrokeThickness = 2
                //};
                //Canvas.Children.Add(end);
                ((Button)sender).Content = "Finished";
            }
            else if(((Button)sender).Content.ToString() == "Finished")
            {
                for (int i = Canvas.Children.Count - 1; i >= 0; i += -1)
                {
                    UIElement Child = Canvas.Children[i];
                    if (Child is Line)
                    {
                        Canvas.Children.Remove(Child);
                    }
                }
                ((Button)sender).Content = "Start";
            }
            else if(((Button)sender).Content.ToString() == "Running")
            {
                board.Interrupt();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Canvas.Children.Clear();
            cities.Clear();
        }
    }
}
