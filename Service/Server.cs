using MemoramaHTTP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MemoramaHTTP.Service
{
    public class Server
    {
        HttpListener listener = new HttpListener();
        byte[] PaginaIndex;
        public event Action<Sala>? SalasActivas;
        List<Sala> salas = new();

        public Server()
        {
            PaginaIndex = File.ReadAllBytes("assets/index.html");
        }

        public void Iniciar()
        {
            if (!listener.IsListening)
            {
                listener = new();
                listener.Prefixes.Add("http://+:19000/memo/");

                listener.Start();
                new Thread(EscucharPeticiones)
                { IsBackground = true }
                .Start();
            }
        }

        public void Detener()
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
        public ObservableCollection<Sala> Salas()
        {
            return new ObservableCollection<Sala>(salas);
        }

        void EscucharPeticiones()
        {
            try
            {
                var context = listener.GetContext();
                new Thread(EscucharPeticiones)
                { IsBackground = true }
                .Start();

                if (context.Request.HttpMethod == "GET")
                {
                    var url = context.Request.RawUrl;

                    switch (url)
                    {
                        case "/memo/":
                            PaginaIndex = File.ReadAllBytes("assets/index.html");

                            context.Response.ContentLength64 = PaginaIndex.Length;
                            context.Response.ContentType = "text/html";
                            context.Response.StatusCode = 200;
                            context.Response.OutputStream.Write(PaginaIndex, 0, PaginaIndex.Length);
                            break;

                        case string when url.StartsWith("/memo/estado/"):

                            var idSala = context.Request.QueryString["idSala"];
                            if (!string.IsNullOrEmpty(idSala) && int.TryParse(idSala, out int salaId))
                            {
                                Sala? s = salas.FirstOrDefault(x => x.Id == salaId);
                                if (s != null)
                                {
                                    s.VerificarJugada();
                                    s.c = true;
                                    var memo = new MemoDTO()
                                    {
                                        Estado = s.Estado,
                                        Jugador1 = s.Jugador1,
                                        Jugador2 = s.Jugador2,
                                        PuntosJ1 = s.PuntosJ1,
                                        PuntosJ2 = s.PuntosJ2,
                                        Tablero = s.Tablero.Cast<int>().ToArray(),
                                        Turno = s.Turno,
                                        IdSala = s.Id,
                                        Resultado = s.Resultado
                                    };

                                    byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));
                                    context.Response.ContentType = "application/json";
                                    context.Response.ContentLength64 = dato.Length;
                                    context.Response.StatusCode = 200;
                                    context.Response.OutputStream.Write(dato, 0, dato.Length);
                                }
                                else
                                {
                                    context.Response.StatusCode = 404;
                                }
                            }
                            break;

                        case string when url.StartsWith("/memo/archivos/"):
                            string requestedFile = url.Replace("/memo/archivos/", "", StringComparison.Ordinal);

                            string assetsPath = Path.Combine(
                                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!
                                        .Parent!.Parent!.Parent!.FullName,
                                "Assets",
                                "Archivos"
                            );
                            string filePath = Path.Combine(assetsPath, requestedFile);

                            if (File.Exists(filePath))
                            {
                                string extension = Path.GetExtension(filePath).ToLower();
                                string contentType = extension switch
                                {
                                    ".mp3" => "audio/mpeg",
                                    ".jpg" => "image/jpeg",
                                    ".jpeg" => "image/jpeg",
                                    ".png" => "image/png",
                                    ".gif" => "image/gif",
                                    ".svg" => "image/svg+xml",
                                    ".webp" => "image/webp",
                                    ".ico" => "image/x-icon",
                                    ".wav" => "audio/wav",
                                    ".ogg" => "audio/ogg",
                                    ".mp4" => "video/mp4",
                                    _ => "application/octet-stream"
                                };

                                byte[] buffer = File.ReadAllBytes(filePath);
                                context.Response.ContentType = contentType;
                                context.Response.ContentLength64 = buffer.Length;
                                context.Response.StatusCode = 200;
                                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                context.Response.StatusCode = 404;
                                using var writer = new StreamWriter(context.Response.OutputStream);
                                writer.Write("Archivo no encontrado");
                            }
                            break;

                        default:
                            context.Response.StatusCode = 404;
                            break;
                    }
                }
                else if (context.Request.HttpMethod == "POST")
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/memo/iniciar/":

                            var buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            var json = Encoding.UTF8.GetString(buffer);
                            var jugador = JsonSerializer.Deserialize<JugadorDTO>(json);

                            if (jugador != null)
                            {
                                var ip = context.Request.RemoteEndPoint.Address.ToString();


                                Sala? s = salas.FirstOrDefault(x =>
                                x.Jugador1 == jugador.Nombre && x.Ip1 == ip ||
                                x.Jugador2 == jugador.Nombre && x.Ip2 == ip);

                                if (s != null)
                                {
                                    if (s.Estado == 2)
                                    {
                                        salas.Remove(s);
                                        s = salas.FirstOrDefault(x => x.EstaCompleto == false);
                                        if (s == null)
                                        {
                                            s = new Sala()
                                            {
                                                Id = salas.Count + 1,
                                            };
                                            s.AgregarJugador(jugador.Nombre, ip);
                                            s.GenerarCartas();
                                            salas.Add(s);
                                            SalasActivas?.Invoke(s);
                                        }
                                        else
                                        {
                                            s.AgregarJugador(jugador.Nombre, ip);
                                            SalasActivas?.Invoke(s);

                                        }
                                        while (s.EstaCompleto == false)
                                        {
                                            Thread.Sleep(1000);
                                        }
                                        var memo = new MemoDTO()
                                        {
                                            Estado = s.Estado,
                                            Jugador1 = s.Jugador1,
                                            Jugador2 = s.Jugador2,
                                            PuntosJ1 = s.PuntosJ1,
                                            PuntosJ2 = s.PuntosJ2,
                                            Tablero = s.Cartas.Cast<int>().ToArray(),
                                            Turno = s.Turno,
                                            IdSala = s.Id
                                        };
                                        byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));
                                        context.Response.ContentLength64 = dato.Length;
                                        context.Response.ContentType = "application/json";
                                        context.Response.OutputStream.Write(dato);
                                        context.Response.StatusCode = 200; 
                                    }
                                    else
                                    {
                                        var memo = new MemoDTO()
                                        {
                                            Estado = s.Estado,
                                            Jugador1 = s.Jugador1,
                                            Jugador2 = s.Jugador2,
                                            PuntosJ1 = s.PuntosJ1,
                                            PuntosJ2 = s.PuntosJ2,
                                            Tablero = s.Cartas.Cast<int>().ToArray(),
                                            Turno = s.Turno,
                                            IdSala = s.Id
                                        };
                                        byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));
                                        context.Response.ContentLength64 = dato.Length;
                                        context.Response.ContentType = "application/json";
                                        context.Response.OutputStream.Write(dato);
                                        context.Response.StatusCode = 200; 
                                    }

                                }
                                else
                                {

                                    s = salas.FirstOrDefault(x => x.EstaCompleto == false);
                                    if (s == null)
                                    {
                                        s = new Sala()
                                        {
                                            Id = salas.Count + 1,
                                        };
                                        s.AgregarJugador(jugador.Nombre, ip);
                                        s.GenerarCartas();
                                        salas.Add(s);
                                        SalasActivas?.Invoke(s);
                                    }
                                    else
                                    {
                                        s.AgregarJugador(jugador.Nombre, ip);
                                        SalasActivas?.Invoke(s);

                                    }
                                    while (s.EstaCompleto == false)
                                    {
                                        Thread.Sleep(1000);
                                    }
                                    var memo = new MemoDTO()
                                    {
                                        Estado = s.Estado,
                                        Jugador1 = s.Jugador1,
                                        Jugador2 = s.Jugador2,
                                        PuntosJ1 = s.PuntosJ1,
                                        PuntosJ2 = s.PuntosJ2,
                                        Tablero = s.Cartas.Cast<int>().ToArray(),
                                        Turno = s.Turno,
                                        IdSala = s.Id
                                    };
                                    byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));
                                    context.Response.ContentLength64 = dato.Length;
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(dato);
                                    context.Response.StatusCode = 200; 
                                }
                            }
                            break;

                        case "/memo/esperando/":
                            buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            json = Encoding.UTF8.GetString(buffer);
                            jugador = JsonSerializer.Deserialize<JugadorDTO>(json);

                            if (jugador != null)
                            {
                                var ip = context.Request.RemoteEndPoint.Address.ToString();

                                Sala? s = salas.FirstOrDefault(x => x.Jugador1 == jugador.Nombre && x.Ip1 == ip || x.Jugador2 == jugador.Nombre && x.Ip2 == ip);

                                if (s == null)
                                {
                                    context.Response.StatusCode = 404;
                                }
                                else
                                {
                                    int intentos = 0;
                                    while (s.c == false && string.IsNullOrEmpty(s.Resultado) && intentos < 15)
                                    {
                                        Thread.Sleep(1000);
                                        intentos++;
                                        if (!string.IsNullOrEmpty(s.Resultado)) break;
                                    }
                                    s.c = false;
                                    var memo = new MemoDTO()
                                    {
                                        Estado = s.Estado,
                                        Jugador1 = s.Jugador1,
                                        Jugador2 = s.Jugador2,
                                        PuntosJ1 = s.PuntosJ1,
                                        PuntosJ2 = s.PuntosJ2,
                                        Tablero = s.Tablero.Cast<int>().ToArray(),
                                        Turno = s.Turno,
                                        IdSala = s.Id,
                                        Resultado = s.Resultado,
                                    };

                                    byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));

                                    context.Response.ContentLength64 = dato.Length;
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(dato);
                                    context.Response.StatusCode = 200; 


                                }
                            }

                            break;

                        case "/memo/jugada/":

                            buffer = new byte[context.Request.ContentLength64];
                            context.Request.InputStream.Read(buffer);
                            json = Encoding.UTF8.GetString(buffer);
                            var jugada = JsonSerializer.Deserialize<JugadaDTO>(json);


                            if (jugada != null)
                            {

                                Sala? s = salas.FirstOrDefault(x => x.Id == jugada.IdSala);

                                if (s == null)
                                {
                                    context.Response.StatusCode = 404; 
                                }
                                else
                                {

                                    if (s.ValidarTurno(jugada.Nombre))
                                    {
                                        s.RealizarMovimiento(jugada.Carta);


                                        var memo = new MemoDTO()
                                        {
                                            Estado = s.Estado,
                                            Jugador1 = s.Jugador1,
                                            Jugador2 = s.Jugador2,
                                            PuntosJ1 = s.PuntosJ1,
                                            PuntosJ2 = s.PuntosJ2,
                                            Tablero = s.Tablero.Cast<int>().ToArray(),
                                            Turno = s.Turno,
                                            IdSala = s.Id
                                        };

                                        byte[] dato = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(memo));

                                        context.Response.ContentLength64 = dato.Length;
                                        context.Response.ContentType = "application/json";
                                        context.Response.OutputStream.Write(dato);
                                        context.Response.StatusCode = 200; 

                                    }
                                    else
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    }
                                }
                            }
                            break;
                    }
                }
                context.Response.Close();

            }
            catch (Exception ex)
            {
                {
                    System.Diagnostics.Debug.WriteLine("Error " + ex);
                }
            }
        }
    }
}
