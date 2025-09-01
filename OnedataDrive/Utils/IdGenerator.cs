using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.Utils
{
    public static class IdGenerator
    {
        private static readonly Random random = Random.Shared;
        public static string GenerateId8()
        {
            return random.NextInt64(10_000_000, 100_000_000).ToString();
        }
    }
}
