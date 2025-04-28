using CommunityToolkit.Mvvm.Input;
using MemoramaHTTP.Models;
using MemoramaHTTP.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace MemoramaHTTP.ViewModels
{
    public class MemoViewModel
    {
        Server server = new();
        public ICommand IniciarCommand { get; set; }
        public ICommand DetenerCommand { get; set; }
        public ObservableCollection<Sala> Salas { get; set; } = new();

        public MemoViewModel()
        {
            IniciarCommand = new RelayCommand(Iniciar);
            DetenerCommand = new RelayCommand(Detener);
            server.SalasActivas += Server_SalasActivas;
        }

        private void Server_SalasActivas(Sala sala)
        {
            App.Current.Dispatcher.Invoke((Delegate)(() =>
            {
                var s = Salas.FirstOrDefault(x => x.Id == sala.Id);
                if (s == null)
                    Salas.Add(sala);
                else
                {
                    s.Jugador1 = sala.Jugador1;
                    s.Jugador2 = sala.Jugador2;
                    s.Turno = sala.Turno;
                    s.Tablero = sala.Tablero;
                    s.PuntosJ1 = sala.PuntosJ1;
                    s.PuntosJ2 = sala.PuntosJ2;
                    s.Ip1 = sala.Ip1;
                    s.Ip2 = sala.Ip2;
                }
            }));
        }

        void Iniciar()
        {
            try
            {
                server.Iniciar();
            }
            catch (HttpListenerException ex)
            {
                if (ex.Message.StartsWith("Acceso denegado"))
                {
                    ProcessStartInfo p = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "http add urlacl url=http://+:19000/memo/ user=Todos",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        Verb = "runas"
                    };

                    var res = Process.Start(p);
                    if (res != null)
                    {
                        res.WaitForExit();
                        if (res.ExitCode == 0)
                        {
                            server.Iniciar();
                        }
                    }
                }
            }
        }

        void Detener()
        {
            server.Detener();
        }

    }
}
