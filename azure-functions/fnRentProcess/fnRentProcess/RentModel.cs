using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fnRentProcess
{
    internal class RentModel
    {
        public class Rootobject
        {
            public string nome { get; set; }
            public string email { get; set; }
            public Veiculo veiculo { get; set; }
            public DateTime data { get; set; }
        }

        public class Veiculo
        {
            public string modelo { get; set; }
            public int ano { get; set; }
            public string tempoAluguel { get; set; }
        }

    }
}
