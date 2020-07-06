using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Numpy;
using Python;
using Python.Runtime;

namespace CS_Classes
{
    public class Person
    {
        public Person(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    public class NumPy_EmbeddedTest
    {
        public void Run()
        {
            Person person = new Person("Bob", "Davies");
            using (PyScope scope = Py.CreateScope())
            {
                PyObject pyPerson = person.ToPython();
                scope.Set("person", pyPerson);
                string code = "Fullname = person.FirstName + ' ' + person.LastName";
                scope.Exec(code);
                string printit = "print (person.FirstName)";
                scope.Exec(printit);
            }
        }
    }





    public class NumPy_EmbeddedMat
    {
        public void Run(OpenCvSharp.Mat src, string cmd)
        {
            using (PyScope scope = Py.CreateScope())
            {
                PyObject pySrc = src.ToPython();
                scope.Set("src", pySrc);
                scope.Exec(cmd);
            }
        }
    }





    public class NumPy_EmbeddedCmd
    {
        public void Run(string cmd)
        {
            using (PyScope scope = Py.CreateScope())
            {
                scope.Exec(cmd);
            }
        }
    }





    public class NumPy_Cmds
    {
        public void Run()
        {
            using (PyScope scope = Py.CreateScope())
            {
                dynamic np = Py.Import("numpy");
                Console.WriteLine(np.cos(np.pi * 2));

                dynamic sin = np.sin;
                Console.WriteLine(sin(5));

                Double c = np.cos(5) + sin(5);
                Console.WriteLine(c);

                dynamic a = np.array(new List<float> { 1, 2, 3});
                Console.WriteLine(a.dtype);

                dynamic b = np.array(new List<float> { 6, 5, 4}, dtype: np.int32);
                Console.WriteLine(b.dtype);

                Console.WriteLine(a * b);
            }
        }
    }
}
