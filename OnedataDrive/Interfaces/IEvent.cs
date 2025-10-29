using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.Interfaces
{
    public interface IEvent<T>
    {
        public void Merge(T mergeWith);
        public string RelationKey();
    }

    public class MergeException : Exception
    {
        public MergeException(string message) : base(message) { }
        public MergeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
