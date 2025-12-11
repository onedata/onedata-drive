using OnedataDrive.Utils;

namespace OnedataDrive.Interfaces
{
    public abstract class IEvent<T>
    {
        public string AEventId { get; protected set; } = IdGenerator.GenerateId8();
        public abstract void Merge(T mergeWith);
        public abstract string RelationKey();
        public abstract List<string> MoreInfo();
    }

    public class MergeException : Exception
    {
        public MergeException(string message) : base(message) { }
        public MergeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
