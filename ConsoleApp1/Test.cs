using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public abstract class Shape
    {
        public abstract Shape GetShape();
        public abstract IAnimal GetAnimal();
    }

    public class Square : Shape
    {
        public override Animal GetAnimal()
        {
            throw new NotImplementedException();
        }

        public override Square GetShape()
        {
            throw new NotImplementedException();
        }
    }

    public interface IAnimal
    {
        IAnimal GetAnimal();
    }

    public class Animal : IAnimal
    {
        public IAnimal GetAnimal()
        {
            throw new NotImplementedException();
        }
    }

    public class Disposable1 : IDisposable
    {
        public bool Disposed { get; set; }
        public void Dispose() => Disposed = true;
    }
}
