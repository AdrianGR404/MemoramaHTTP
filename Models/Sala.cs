using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MemoramaHTTP.Models
{
    public class Sala:INotifyPropertyChanged
    {
        public int Id { get; set; }
        private string jugador1 = "";
        public string Jugador1
        {
            get => jugador1;
            set
            {
                if (jugador1 != value)
                {
                    jugador1 = value;
                    PropertyChanged?.Invoke(this, new(nameof(Jugador1)));
                }
            }
        }

        private string jugador2 = "";
        public string Jugador2
        {
            get => jugador2;
            set
            {
                if (jugador2 != value)
                {
                    jugador2 = value;
                    PropertyChanged?.Invoke(this, new(nameof(Jugador2)));
                }
            }
        }

        private int puntosJ1;
        public int PuntosJ1
        {
            get => puntosJ1;
            set
            {
                if (puntosJ1 != value)
                {
                    puntosJ1 = value;
                    PropertyChanged?.Invoke(this, new(nameof(PuntosJ1)));
                }
            }
        }

        private int puntosJ2;
        public int PuntosJ2
        {
            get => puntosJ2;
            set
            {
                if (puntosJ2 != value)
                {
                    puntosJ2 = value;
                    PropertyChanged?.Invoke(this, new(nameof(PuntosJ2)));
                }
            }
        }
        private string ip1 = "";
        public string Ip1
        {
            get => ip1;
            set
            {
                if (ip1 != value)
                {
                    ip1 = value;
                    PropertyChanged?.Invoke(this, new(nameof(Ip1)));
                }
            }
        }
        private string ip2 = "";
        public string Ip2
        {
            get => ip2;
            set
            {
                if (ip2 != value)
                {
                    ip2 = value;
                    PropertyChanged?.Invoke(this, new(nameof(Ip2)));
                }
            }
        }
        
        public string Resultado { get; set; }

        public string Turno { get; set; } = "";

        public int[,] Tablero { get; set; } = new int[5, 4]; 
        public int[,] Cartas { get; set; } = new int[5, 4];  

        public List<(int x, int y)> CartasVolteadas { get; set; } = new();
        public bool c { get; set; } = false;
        public bool chambeando {  get; set; } = false;

        public int Estado { get; set; }


        public bool EstaCompleto => Jugador1 != "" && Jugador2 != "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AgregarJugador(string nombre, string ip)
        {
            if (Jugador1 == "")
            {
                Jugador1 = nombre;
                Ip1 = ip;
                Estado = 0;
                Turno = nombre;
                return;
            }

            if (Jugador2 == "")
            {
                if (nombre == Jugador1)
                    throw new ArgumentException("Los jugadores no pueden tener el mismo nombre");

                Jugador2 = nombre;
                Ip2 = ip;
                Estado = 1;
                GenerarCartas(); 
            }
        }

        public bool ValidarTurno(string nombre)
        {
            return nombre == Turno && !chambeando && CartasVolteadas.Count < 2;
        }

        public bool CartaYaVolteada(int x, int y)
        {
            return Tablero[x, y] != 0 || CartasVolteadas.Any(c => c.x == x && c.y == y);
        }

        public void RealizarMovimiento(int index)
        {
            if (chambeando) return;
            chambeando = true;
            int columnas = Cartas.GetLength(1);
            int x = index / columnas;
            int y = index % columnas;

            if (CartaYaVolteada(x, y))
                return;

            CartasVolteadas.Add((x, y));
            
            if (Turno == Jugador1)
            {
                Tablero[x, y] = 1;
            }
            else
            {
                Tablero[x, y] = 2;
            }
            c = true;
            chambeando= false;

        }
        public void VerificarJugada()
        {
            if (CartasVolteadas.Count == 2)
            {
                var (x1, y1) = CartasVolteadas[0];
                var (x2, y2) = CartasVolteadas[1];

                if (Cartas[x1, y1] == Cartas[x2, y2])
                {
                    int jugador = Turno == Jugador1 ? 1 : 2;
                    if (jugador == 1) PuntosJ1++;
                    else PuntosJ2++;
                }
                else
                {
                    Tablero[x1, y1] = 0;
                    Tablero[x2, y2] = 0;
                    Turno = Turno == Jugador1 ? Jugador2 : Jugador1;
                }
                CartasVolteadas.Clear();
                if (VerificarCompleto())
                {
                    if(puntosJ1 > puntosJ2)
                    {
                        Resultado = "Victoria para "+Jugador1;
                    }else if(puntosJ1 < puntosJ2)
                    {
                        Resultado = "Victoria para " + Jugador2;
                    }
                    else
                    {
                        Resultado = "Empate";
                    }
                    Estado = 2;
                    c=true;
                }
            }
        }
        public bool VerificarCompleto()
        {
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 4; j++)
                    if (Tablero[i, j] == 0)
                        return false;
            return true;
        }

        public void GenerarCartas()
        {
            var valores = Enumerable.Range(0, 10) 
                                  .SelectMany(i => new[] { i, i }) 
                                  .OrderBy(_ => Guid.NewGuid()) 
                                  .ToArray();
            int index = 0;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 4; j++)
                    Cartas[i, j] = valores[index++];
        }
    }
}
