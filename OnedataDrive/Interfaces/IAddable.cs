using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.Interfaces
{
    internal interface IAddable<T>
    {
        public void AddEvent(T item);
    }
}
