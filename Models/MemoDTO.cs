using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoramaHTTP.Models
{
    public class MemoDTO
    {
        public int IdSala { get; set; }
        public int[] Tablero { get; set; } = new int[20];
        public string Turno { get; set; }
        public string Jugador1 { get; set; }
        public string Jugador2 { get; set; }
        public int PuntosJ1 { get; set; }
        public int PuntosJ2 { get; set; }
        public string Resultado {  get; set; }
        public int Estado { get; set; }
        public string Alert { get; set; }
    }
}
