using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BackgroundWorkers
{
    public interface IHandler<in T> where T : class
    {
        Task Run(T message);
        void OnComplete(T message);
        Collection<object> NewWorkItems { get; }
    }

    public abstract class Handler<T> : IHandler<T> where T : class
    {
        protected Handler()
        {
             NewWorkItems = new Collection<object>();   
        }

        public Collection<object> NewWorkItems { get; private set; }

        public abstract Task Run(T message);

        public virtual void OnComplete(T message)
        {
        }
    }

    public abstract class ForkHandler<TIn, TOut> : Handler<TIn>
        where TIn : class
        where TOut : class
    {
        protected ForkHandler() : base()
        {
            
        }

        public void ForkNewWork(TOut message)
        {
            base.NewWorkItems.Add(new MergeableMessage {Body = message});
        }
    }
}